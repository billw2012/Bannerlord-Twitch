using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BannerlordTwitch.Dummy;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Testing;
using BannerlordTwitch.Util;
using Newtonsoft.Json;
using TaleWorlds.Core;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace BannerlordTwitch
{
    public class ReplyContext
    {
        public string UserName { get; private set; }
        public string ReplyId { get; private set; }
        public string Args { get; private set; }
        public int Bits { get; private set; }
        public double BitsInDollars { get; private set; }
        public int SubscribedMonthCount { get; private set; }
        public bool IsBroadcaster { get; private set; }
        public bool IsModerator { get; private set; }
        public bool IsSubscriber { get; private set; }
        public bool IsVip { get; private set; }
        //public bool IsWhisper { get; private set; }
        public Guid RedemptionId { get; private set; }
        public ActionBase Source { get; private set; }

        public static ReplyContext FromMessage(ActionBase source, ChatMessage msg, string args) =>
            new()
            {
                UserName = msg.DisplayName,
                ReplyId = msg.Id,
                Args = args,
                Bits = msg.Bits,
                BitsInDollars = msg.BitsInDollars,
                SubscribedMonthCount = msg.SubscribedMonthCount,
                IsBroadcaster = msg.IsBroadcaster,
                IsModerator = msg.IsModerator,
                IsSubscriber = msg.IsSubscriber,
                IsVip = msg.IsVip,
                Source = source,
            };
        
        // public static ReplyContext FromWhisper(ResponseBase source, WhisperMessage whisper) =>
        //     new()
        //     {
        //         UserName = whisper.DisplayName,
        //         ReplyId = whisper.MessageId,
        //         Args = whisper.Message,
        //         IsBroadcaster = whisper.UserType == UserType.Broadcaster,
        //         IsModerator = whisper.UserType == UserType.Moderator,
        //         IsWhisper = true,
        //         Source = source,
        //     };
        
        public static ReplyContext FromRedemption(ActionBase source, OnRewardRedeemedArgs args) =>
            new()
            {
                UserName = args.DisplayName,
                Args = args.Message,
                RedemptionId = args.RedemptionId,
                Source = source,
            };
        
        public static ReplyContext FromUser(ActionBase source, string userName, string args = null) =>
            new()
            {
                UserName = userName,
                Args = args,
                Source = source,
            };
    }
    
    // https://twitchtokengenerator.com/
    // https://twitchtokengenerator.com/quick/AAYotwZPvU
    internal partial class TwitchService : IDisposable
    {
        private TwitchPubSub pubSub;
        private readonly TwitchAPI api;
        private string channelId;
        private readonly AuthSettings authSettings;

        private readonly Settings settings;

        private readonly ConcurrentDictionary<Guid, OnRewardRedeemedArgs> redemptionCache = new();
        private Bot bot;

        public TwitchService()
        {
            settings = Settings.Load();
            if (settings == null)
            {
                throw new Exception($"Failed to load action/command settings, please use the BLT Configure Window to configure the mod");
            }

            authSettings = AuthSettings.Load();
            if (authSettings == null)
            {
                throw new Exception($"You need to authorize via the BLT Configure Window, then restart. If the window isn't open then you need to enable the BLTConfigure module.");
            }

            if (authSettings.DebugSpoofAffiliate)
            {
                Log.LogFeedSystem($"Affiliate spoofing enabled");
                affiliateSpoofing = new Dummy.AffiliateSpoofingHttpCallHandler();
                api = new TwitchAPI(http: affiliateSpoofing);
                affiliateSpoofing.OnRewardRedeemed += OnRewardRedeemed;
            }
            else
            {
                api = new TwitchAPI();
            }

            //api.Settings.Secret = SECRET;
            api.Settings.SkipDynamicScopeValidation = true;
            api.Settings.ClientId = authSettings.ClientID;
            api.Settings.AccessToken = authSettings.AccessToken;

            api.Helix.Users.GetUsersAsync(accessToken: authSettings.AccessToken).ContinueWith(t =>
            {
                MainThreadSync.Run(() =>
                {
                    if (t.IsFaulted)
                    {
                        Log.LogFeedFail($"Service init failed: {t.Exception?.GetBaseException().Message}");
                        return;
                    }
                    
                    var user = t.Result.Users.First();

                    Log.Info($"Channel ID is {user.Id}");
                    channelId = user.Id;
                    
                    // Connect the chatbot
                    bot = new Bot(user.Login, authSettings);

                    if (string.IsNullOrEmpty(user.BroadcasterType))
                    {
                        Log.LogFeedFail($"Service init failed: you must be a twitch partner or affiliate to use the channel points system. Chat bot and testing are still functioning.");
                        return;
                    }
                    
                    // Create new instance of PubSub Client
                    pubSub = new TwitchPubSub();

                    // Subscribe to Events
                    // Whisper isn't supported without verified bot
                    //_pubSub.OnWhisper += OnWhisper;
                    pubSub.OnPubSubServiceConnected += OnPubSubServiceConnected;
                    pubSub.OnRewardRedeemed += OnRewardRedeemed;
                    pubSub.OnLog += (sender, args) =>
                    {
                        if (args.Data.Contains("PONG")) return;
                        try
                        {
                            Log.Trace(args.Data);
                        }
                        catch
                        {
                            // ignored
                        }
                    };

                    // pubSub.OnPubSubServiceClosed += OnOnPubSubServiceClosed;
                    RegisterRewardsAsync();

                    // Connect
                    pubSub.Connect();
                });
            });
        }
        
        private async void RegisterRewardsAsync()
        {
            RemoveRewards();

            Log.Info("Creating rewards");
            
            GetCustomRewardsResponse existingRewards = null;
            try
            {
                existingRewards = await api.Helix.ChannelPoints.GetCustomReward(channelId, accessToken: authSettings.AccessToken, onlyManageableRewards: true);
            }
            catch (Exception e)
            {
                Log.LogFeedFail($"ERROR: Couldn't retrieve existing rewards: {e.Message}");
            }

            bool anyFailed = false;
            foreach (var rewardDef in settings.EnabledRewards.Where(r => existingRewards == null || existingRewards.Data.All(e => e.Title != r.RewardSpec?.Title)))
            {
                try
                {
                    var createdReward = (await api.Helix.ChannelPoints.CreateCustomRewards(channelId, rewardDef.RewardSpec.GetTwitchSpec(), authSettings.AccessToken)).Data.First();
                    Log.Info($"Created reward {createdReward.Title} ({createdReward.Id})");
                }
                catch (Exception e)
                {
                    Log.LogFeedCritical($"Couldn't create reward {rewardDef.RewardSpec.Title}: {e.Message}");
                    anyFailed = true;
                }
            }

            if (anyFailed)
            {
                InformationManager.ShowInquiry(
                    new InquiryData(
                        "Bannerlord Twitch",
                        $"Failed to create some of the channel rewards, please check the logs for details!",
                        true, false, "Okay", null,
                        () => {}, () => {}), true);
            }
        }

        private void RemoveRewards()
        {
            Log.Info("Removing existing rewards");
            try
            {
                var allRewards = api.Helix.ChannelPoints.GetCustomReward(channelId, accessToken: authSettings.AccessToken, onlyManageableRewards: true).Result;
                if (allRewards == null)
                {
                    throw new Exception($"Couldn't retrieve channel point rewards");
                }
                Task.WaitAll(allRewards.Data.Select(r => api.Helix.ChannelPoints.DeleteCustomReward(channelId, r.Id, accessToken: authSettings.AccessToken)
                        .ContinueWith(t =>
                        {
                            Log.Info(t.IsCompleted
                                ? $"Removed reward {r.Title}"
                                : $"Failed to remove {r.Title}: {t.Exception?.Message}");
                        })).ToArray(), TimeSpan.FromSeconds(5));
                Log.LogFeedSystem($"All rewards removed");
            }
            catch (Exception e)
            {
                Log.LogFeedSystem($"Failed to remove all rewards: {e.Message}");
            }
        }

        private void OnRewardRedeemed(object sender, OnRewardRedeemedArgs redeemedArgs)
        {
            MainThreadSync.Run(() =>
            {
                var reward = settings.Rewards.FirstOrDefault(r => r.RewardSpec.Title == redeemedArgs.RewardTitle);
                if (reward == null)
                {
                    Log.Info($"Reward {redeemedArgs.RewardTitle} not owned by this extension, ignoring it");
                    // We don't cancel redemptions we don't know about!
                    // RedemptionCancelled(e.RedemptionId, $"Reward {e.RewardTitle} not found");
                    return;
                }

                if (redeemedArgs.Status != "UNFULFILLED")
                {
                    Log.Info($"Reward {redeemedArgs.RewardTitle} status {redeemedArgs.Status} is not interesting, ignoring it");
                    return;
                }

                var context = ReplyContext.FromRedemption(reward, redeemedArgs);
                try
                {
                    redemptionCache.TryAdd(redeemedArgs.RedemptionId, redeemedArgs);
                    if (!ActionManager.HandleReward(reward.Handler, context, reward.HandlerConfig))
                    {
                        Log.Error($"Couldn't enqueue redemption {redeemedArgs.RedemptionId}: " 
                                  + $"RedemptionAction {reward.Handler} not found, check you have its Reward extension installed!");
                        // We DO cancel redemptions we know about, where the implementation is missing
                        RedemptionCancelled(context, $"Redemption action {reward.Handler} wasn't found");
                    }
                    else
                    {
                        //Log.Info($"Redemption of {redeemedArgs.RewardTitle} from {redeemedArgs.DisplayName} received!");
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Exception happened while trying to enqueue redemption {redeemedArgs.RedemptionId}: {e.Message}");
                    RedemptionCancelled(context, $"Exception occurred: {e.Message}");
                }
            });
        }

        public bool TestRedeem(string rewardName, string user, string message)
        {
            var reward = settings?.EnabledRewards.FirstOrDefault(r => string.Equals(r.RewardSpec.Title, rewardName, StringComparison.CurrentCultureIgnoreCase));
            if (reward == null)
            {
                Log.Error($"Reward {rewardName} not found!");
                return false;
            }

            if (affiliateSpoofing == null)
            {
                Log.LogFeedFail($"'DebugSpoofAffiliate: true' must be set in Bannerlord-Twitch-Auth.yaml to spoof redemptions (including in sim testing)!");
                return false;
            }

            return affiliateSpoofing.FakeRedeem(reward.RewardSpec.Title, user, message) == true;
            // var redeem = new OnRewardRedeemedArgs
            // {
            //     RedemptionId = Guid.NewGuid(),
            //     Message = message,
            //     DisplayName = user,
            //     Login = user,
            //     RewardTitle = rewardName,
            //     ChannelId = null,
            // };
            // redemptionCache.TryAdd(redeem.RedemptionId, redeem);
            //
            // ActionManager.HandleReward(reward.Handler, ReplyContext.FromRedemption(reward, redeem), reward.HandlerConfig);
        }

        // private void ShowMessage(string screenMsg, string botMsg, string userToAt)
        // {
        //     Log.Screen(screenMsg);
        //     SendChat($"@{userToAt}: {botMsg}");
        // }
        //
        // private void ShowMessageFail(string screenMsg, string botMsg, string userToAt)
        // {
        //     Log.ScreenFail(screenMsg);
        //     SendChat($"@{userToAt}: {botMsg}");
        // }

        // public void SendChat(params string[] message)
        // {
        //     Log.Trace($"[chat] {string.Join(" - ", message)}");
        //     bot.SendChat(message);
        // }

        public void SendReply(ReplyContext context, params string[] messages)
        {
            if (context.Source.RespondInOverlay)
            {
                Log.LogFeedResponse(context.UserName, messages);
                //Log.Trace($"[{nameof(TwitchService)}] Feed Response to {context.UserName}: {Join(", ", messages)}");
            }

            if (context.Source.RespondInTwitch)
            {
                // if (context.IsWhisper)
                // {
                //     bot.SendWhisper(context.UserName, messages);
                //     Log.Trace($"[whisper][{context.UserName}] {string.Join(" - ", messages)}");
                // }
                // else 
                if (context.UserName != null)
                {
                    bot.SendChatReply(context.UserName, messages);
                    Log.Trace($"[{nameof(TwitchService)}] Reply to {context.UserName}: {string.Join(", ", messages)}");
                }
                else
                {
                    bot.SendChat(messages);
                    Log.Trace($"[{nameof(TwitchService)}] Chat: {string.Join(", ", messages)}");
                }
            }

            if (context.Source.RespondInExtension)
            {
                //TODO: send the message to extension
                dynamic obj = new ExpandoObject();
                obj.context = context;
                obj.messages = messages;

                var socket = BLTModule.bltApi.GetSocketAwaitingResponseFor(context.UserName);
                if(socket != null)
                {
                    socket.Send(JsonConvert.SerializeObject(obj));
                }
            }
        }
        
        private void ShowCommandHelp()
        {
            var help = "Commands: ".Yield()
                .Concat(settings.EnabledCommands.Where(c
                        => !c.HideHelp && !c.BroadcasterOnly && !c.ModOnly)
                    .Select(c => string.IsNullOrEmpty(c.Help) ? c.Name : $"!{c.Name} - {c.Help}")
                )
                .ToList();
            if (settings.EnabledRewards.Any())
            {
                help.Add($"Rewards: ");
                help.AddRange(settings.EnabledRewards.Select(r => r.RewardSpec.Title));
            }
            bot.SendChat(help.ToArray());
        }
        
        public bool ExecuteCommand(string cmdName, ChatMessage chatMessage, string args)
        {
            var cmd = this.settings.GetCommand(cmdName);
            if (cmd == null)
            {
                Log.Trace($"[{nameof(TwitchService)}] Couldn't find command {cmdName}");
                return false;
            }

            var context = ReplyContext.FromMessage(cmd, chatMessage, args);
            if (cmd.ModOnly && !context.IsModerator && !context.IsBroadcaster ||
                cmd.BroadcasterOnly && !context.IsBroadcaster)
            {
                Log.Trace($"[{nameof(TwitchService)}] {chatMessage.Username} not allowed to use command {cmdName}");
                return false;
            }

            ActionManager.HandleCommand(cmd.Handler, context, cmd.HandlerConfig);
            return true;
        }

        public bool TestCommand(string cmdName, string userName, string args)
        {
            var cmd = this.settings.GetCommand(cmdName);
            if (cmd == null)
                return false;
            var context = ReplyContext.FromUser(cmd, userName, args);
            ActionManager.HandleCommand(cmd.Handler, context, cmd.HandlerConfig);
            return true;
        }
        
        public void RedemptionComplete(ReplyContext context, string info = null)
        {
            if (!redemptionCache.TryRemove(context.RedemptionId, out var redemption))
            {
                Log.Error($"RedemptionComplete failed: redemption {context.RedemptionId} not known!");
                return;
            }
            //Log.Trace($"[{nameof(TwitchService)}] Redemption of {redemption.RewardTitle} for {redemption.DisplayName} complete{(!IsNullOrEmpty(info) ? $": {info}" : "")}");
            ActionManager.SendReply(context, info);
            if (!string.IsNullOrEmpty(redemption.ChannelId))
            {
                if ((context.Source as Reward)?.RewardSpec?.DisableAutomaticFulfillment != true)
                {
                    _ = SetRedemptionStatusAsync(redemption, CustomRewardRedemptionStatus.FULFILLED);
                }
                else
                {
                    Log.Info($"Skipped marking {redemption.RewardTitle} for {redemption.DisplayName} as fulfilled as DisableAutomaticFulfillment is set");
                }
            }
            else
            {
                Log.Trace($"(skipped setting redemption status for test redemption)");
            }
        }

        public void RedemptionCancelled(ReplyContext context, string reason = null)
        {
            if (!redemptionCache.TryRemove(context.RedemptionId, out var redemption))
            {
                Log.Error($"RedemptionCancelled failed: redemption {context.RedemptionId} not known!");
                return;
            }
            //Log.Trace($"[{nameof(TwitchService)}] Redemption of {redemption.RewardTitle} for {redemption.DisplayName} cancelled{(!IsNullOrEmpty(reason) ? $": {reason}" : "")}");
            ActionManager.SendReply(context, reason);
            if (!string.IsNullOrEmpty(redemption.ChannelId))
            {
                _ = SetRedemptionStatusAsync(redemption, CustomRewardRedemptionStatus.CANCELED);
            }
            else
            {
                Log.Trace($"(skipped setting redemption status for test redemption)");
            }
        }

        private async Task SetRedemptionStatusAsync(OnRewardRedeemedArgs redemption, CustomRewardRedemptionStatus status)
        {
            try
            {
                await api.Helix.ChannelPoints.UpdateCustomRewardRedemptionStatus(
                    redemption.ChannelId,
                    redemption.RewardId.ToString(),
                    new List<string> {redemption.RedemptionId.ToString()},
                    new UpdateCustomRewardRedemptionStatusRequest {Status = status},
                    authSettings.AccessToken
                );
                //Log.Info($"Set redemption status of {redemption.RedemptionId} ({redemption.RewardTitle} for {redemption.DisplayName}) to {status}");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to set redemption status of {redemption.RedemptionId} ({redemption.RewardTitle} for {redemption.DisplayName}) to {status}: {e.Message}");
            }
        }

        private void OnPubSubServiceConnected(object sender, System.EventArgs e)
        {
            Log.LogFeedSystem("TwitchService connected");

#pragma warning disable 618
            // Obsolete warning disabled because no new version has yet been written!
            pubSub.ListenToRewards(channelId);
#pragma warning restore 618
            pubSub.SendTopics(authSettings.AccessToken);
        }
        
        // private void OnOnPubSubServiceClosed(object sender, EventArgs e)
        // {
        //     Log.ScreenFail("PubSub Service closed, attempting reconnect...");
        //     pubSub.Connect();
        // }
        
        public object FindGlobalConfig(string id) => settings?.GlobalConfigs?.FirstOrDefault(c => c.Id == id)?.Config;

        private static SimulationTest simTest;
        private readonly AffiliateSpoofingHttpCallHandler affiliateSpoofing;

        public bool StartSim()
        {
            StopSim();
            simTest = new SimulationTest(settings);
            return true;
        }
        
        public bool StopSim()
        {
            if (simTest != null)
            {
                Log.LogFeedSystem($"Sim stopped");
                simTest.Stop();
                simTest = null;
                return true;
            }

            return false;
        }

        private void ReleaseUnmanagedResources()
        {
            StopSim();
            RemoveRewards();
            bot?.Dispose();
            pubSub?.Disconnect();
            Log.LogFeedSystem($"TwitchService stopped");
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~TwitchService()
        {
            ReleaseUnmanagedResources();
        }
    }
}