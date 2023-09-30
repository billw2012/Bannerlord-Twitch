using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BannerlordTwitch.Dummy;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Testing;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.Library;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace BannerlordTwitch
{
    public class ReplyContext
    {
        [UsedImplicitly] public string UserName { get; private set; }
        [UsedImplicitly] public string ReplyId { get; private set; }
        [UsedImplicitly] public string Args { get; private set; }
        [UsedImplicitly] public int Bits { get; private set; }
        [UsedImplicitly] public double BitsInDollars { get; private set; }
        [UsedImplicitly] public int SubscribedMonthCount { get; private set; }
        [UsedImplicitly] public bool IsBroadcaster { get; private set; }
        [UsedImplicitly] public bool IsModerator { get; private set; }
        [UsedImplicitly] public bool IsSubscriber { get; private set; }
        [UsedImplicitly] public bool IsVip { get; private set; }
        //public bool IsWhisper { get; private set; }
        [UsedImplicitly] public string RedemptionId { get; private set; }
        [UsedImplicitly] public ActionBase Source { get; private set; }

        public string ArgsErrorMessage(string args)
        {
            if (Source is Command cmd)
            {
                return "{=JSW1ryNl}Usage: !{Name} {Args}".Translate(("Name", cmd.Name), ("Args", args));
            }
            else
            {
                return "{=mdhbHYNM}Usage: {Args}".Translate(("Args", args));
            }
        }

        private static string CleanDisplayName(string str) => str.Replace(" ", "").Replace(@"\s", "");
        public static ReplyContext FromMessage(ActionBase source, ChatMessage msg, string args) =>
            new()
            {
                UserName = CleanDisplayName(msg.DisplayName),
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
        
        public static ReplyContext FromRedemption(ActionBase source, Redemption redemption) =>
            new()
            {
                UserName = CleanDisplayName(redemption.User.DisplayName),
                Args = redemption.UserInput,
                RedemptionId = redemption.Id,
                Source = source,
            };
        
        public static ReplyContext FromUser(ActionBase source, string userName, string args = null) =>
            new()
            {
                UserName = CleanDisplayName(userName),
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
        
        private readonly ConcurrentDictionary<string, Redemption> redemptionCache = new();
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
                affiliateSpoofing.OnRewardRedeemed += OnRewardRedeemedInternal;
            }
            else
            {
                api = new TwitchAPI(http: new CustomTwitchHttpClient());
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
                        Log.Fatal($"Service init failed: {t.Exception?.GetBaseException().Message}");
                        return;
                    }
                    
                    var user = t.Result.Users.First();

                    Log.Info($"Channel ID is {user.Id}");
                    channelId = user.Id;
                    
                    // Connect the chatbot
                    bot = new Bot(user.Login, authSettings);

                    if (string.IsNullOrEmpty(user.BroadcasterType))
                    {
                        Log.Error($"You must be a Twitch Partner or Affiliate to use the channel points system. You can still use the chat commands (you may need to add some in the configure window to get full functionality).");
                        return;
                    }
                    
                    // Create new instance of PubSub Client
                    pubSub = new TwitchPubSub();

                    // Subscribe to Events
                    // Whisper isn't supported without verified bot
                    //_pubSub.OnWhisper += OnWhisper;
                    pubSub.OnPubSubServiceConnected += OnPubSubServiceConnected;
                    pubSub.OnChannelPointsRewardRedeemed += OnRewardRedeemed;
                    pubSub.ListenToChannelPoints(channelId);
                    // pubSub.OnRewardRedeemed += OnRewardRedeemed;
                    pubSub.OnLog += (_, args) =>
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
                    pubSub.SendTopics(authSettings.AccessToken);
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
                existingRewards = await api.Helix.ChannelPoints.GetCustomRewardAsync(channelId, accessToken: authSettings.AccessToken, onlyManageableRewards: true);
            }
            catch (Exception e)
            {
                Log.Error($"ERROR: Couldn't retrieve existing rewards: {e.Message}");
            }

            var failures = new List<string>();
            foreach (var rewardDef in settings.EnabledRewards.Where(r => existingRewards == null || existingRewards.Data.All(e => e.Title != r.RewardSpec?.Title.ToString())))
            {
                try
                {
                    if (rewardDef.RewardSpec.Cost <= 0)
                    {
                        throw new Exception("Cost must be greater than 0, it must NOT be 0");
                    }
                    if (rewardDef.RewardSpec.GlobalCooldownSeconds is <= 0)
                    {
                        throw new Exception("Global Cooldown must be either blank or greater than 0, it must NOT be 0");
                    }
                    if (rewardDef.RewardSpec.MaxPerUserPerStream is <= 0)
                    {
                        throw new Exception("Max Per User Per Stream must be either blank or greater than 0, it must NOT be 0");
                    }
                    if (rewardDef.RewardSpec.MaxPerStream is <= 0)
                    {
                        throw new Exception("Max Per Stream must be either blank or greater than 0, it must NOT be 0");
                    }

                    var createdReward = (await api.Helix.ChannelPoints.CreateCustomRewardsAsync(channelId, rewardDef.RewardSpec.GetTwitchSpec(), authSettings.AccessToken)).Data.First();
                    Log.Info($"Created reward {createdReward.Title} ({createdReward.Id})");
                }
                catch (Exception e)
                {
                    Log.Error($"Couldn't create reward {rewardDef.RewardSpec.Title}: {e.Message}");
                    failures.Add($"{rewardDef.RewardSpec.Title}: {e.Message}");
                }
            }

            if (failures.Any())
            {
                InformationManager.ShowInquiry(
                    new InquiryData(
                        "Bannerlord Twitch",
                        $"Failed to create some of the channel rewards:\n" + string.Join("\n", failures),
                        true, false, "Okay", null,
                        () => {}, () => {}), true);
            }
        }

        private void RemoveRewards()
        {
            Log.Info("Removing existing rewards");
            try
            {
                var allRewards = api.Helix.ChannelPoints.GetCustomRewardAsync(
                    channelId, accessToken: authSettings.AccessToken, onlyManageableRewards: true).Result;
                if (allRewards == null)
                {
                    throw new Exception($"Couldn't retrieve channel point rewards");
                }
                Task.WaitAll(allRewards.Data.Select(r
                    => api.Helix.ChannelPoints.DeleteCustomRewardAsync(
                            channelId, r.Id, accessToken: authSettings.AccessToken)
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

        private void OnRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs redeemedArgs)
        {
            if (redeemedArgs.ChannelId == channelId)
            {
                OnRewardRedeemedInternal(sender, redeemedArgs.RewardRedeemed.Redemption);
            }
        }

        private void OnRewardRedeemedInternal(object sender, Redemption redemption)
        {
            MainThreadSync.Run(() =>
            {
                var reward = settings.Rewards.FirstOrDefault(r => r.RewardSpec.Title.ToString() == redemption.Reward.Title);
                if (reward == null)
                {
                    Log.Info($"Reward {redemption.Reward.Title} not owned by this extension, ignoring it");
                    // We don't cancel redemptions we don't know about!
                    // RedemptionCancelled(e.RedemptionId, $"Reward {e.RewardRedeemed.Redemption.Reward.Title} not found");
                    return;
                }

                if (redemption.Status != "UNFULFILLED")
                {
                    Log.Info($"Reward {redemption.Reward.Title} status {redemption.Status} is not interesting, " +
                             $"ignoring it");
                    return;
                }

                Log.Info($"Redemption of {redemption.Reward.Title} from {redemption.User.DisplayName} received!");

                var context = ReplyContext.FromRedemption(reward, redemption);
#if !DEBUG
                try
                {
#endif
                    redemptionCache.TryAdd(redemption.Id, redemption);
                    ActionManager.HandleReward(reward.Handler, context, reward.HandlerConfig);
#if !DEBUG
                }
                catch (Exception e)
                {
                    Log.Error($"Exception happened while trying to enqueue redemption {redemption.Id}: {e.Message}");
                    RedemptionCancelled(context, $"Exception occurred: {e.Message}");
                }
#endif
            });
        }

        public bool TestRedeem(string rewardName, string user, string message)
        {
            var reward = settings?.EnabledRewards.FirstOrDefault(r => string.Equals(r.RewardSpec.Title.ToString(), rewardName, StringComparison.CurrentCultureIgnoreCase));
            if (reward == null)
            {
                Log.Error($"Reward {rewardName} not found!");
                return false;
            }

            if (affiliateSpoofing == null)
            {
                Log.Error($"You must enable Affiliate Spoofing on the Auth tab in the configure window to test redemption or perform sim testing!");
                return false;
            }

            return affiliateSpoofing.FakeRedeem(reward.RewardSpec.Title.ToString(), user, message);
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

        public bool IsSimTesting => simTest != null;
        
        public void SendReply(ReplyContext context, params string[] messages)
        {
            if (context.Source.RespondInOverlay || IsSimTesting)
            {
                Log.LogFeedResponse(context.UserName, messages);
                //Log.Trace($"[{nameof(TwitchService)}] Feed Response to {context.UserName}: {Join(", ", messages)}");
            }

            if (context.Source.RespondInTwitch && !IsSimTesting)
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
        }

        public void SendNonReply(ReplyContext context, params string[] messages)
        {
            if (context.Source.RespondInOverlay || IsSimTesting)
            {
                Log.LogFeedMessage(messages);
            }
            if (context.Source.RespondInTwitch && !IsSimTesting)
            {
                bot.SendChat(messages);
            }
        }
        
        public void SendChat(params string[] messages)
        {
            if (!IsSimTesting)
            {
                bot.SendChat(messages);
            }
            else
            {
                Log.LogFeedMessage("[CHAT]".Yield().Concat(messages).ToArray());
            }

            Log.Trace($"[{nameof(TwitchService)}] Chat: {string.Join(", ", messages)}");
        }

        private void ShowCommandHelp()
        {
            MainThreadSync.Run(() =>
            {
                var help = "{=luOJS8dL}Commands: ".Translate().Yield()
                    .Concat(settings.EnabledCommands.Where(c => !c.HideHelp)
                        .Select(c => LocString.IsNullOrEmpty(c.Help) ? $"!{c.Name}" : $"!{c.Name} - {c.Help}")
                    )
                    .ToList();
                if (settings.EnabledRewards.Any())
                {
                    help.Add("{=0o3dPQSk}Also see Channel Point Rewards".Translate());
                }

                bot.SendChat(help.ToArray());
            });
        }
        
        public void ExecuteCommand(string cmdName, ChatMessage chatMessage, string args)
        {
            MainThreadSync.Run(() =>
            {
                var cmd = this.settings.GetCommand(cmdName);
                if (cmd == null)
                {
                    Log.Trace($"[{nameof(TwitchService)}] Couldn't find command {cmdName}");
                    return;
                }

                var context = ReplyContext.FromMessage(cmd, chatMessage, args);

#if !DEBUG
                try
                {
#endif
                    ActionManager.HandleCommand(cmd.Handler, context, cmd.HandlerConfig);
#if !DEBUG
                }
                catch (Exception e)
                {
                    Log.Exception($"Command {cmdName} failed with exception {e.Message}, game might be unstable now!", e);
                }
#endif
            });
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
            if(!string.IsNullOrEmpty(info))
            {
                ActionManager.SendReply(context, info);
            }

            if (affiliateSpoofing == null)
            {
                if (!settings.DisableAutomaticFulfillment && (context.Source as Reward)?.RewardSpec?.DisableAutomaticFulfillment != true)
                {
                    _ = SetRedemptionStatusAsync(redemption, CustomRewardRedemptionStatus.FULFILLED);
                }
                else
                {
                    Log.Info($"Skipped marking {redemption.Reward.Title} for {redemption.User.DisplayName} as fulfilled as DisableAutomaticFulfillment is set");
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
            if(!string.IsNullOrEmpty(reason))
            {
                ActionManager.SendReply(context, reason);
            }

            if (affiliateSpoofing == null)
            {
                _ = SetRedemptionStatusAsync(redemption, CustomRewardRedemptionStatus.CANCELED);
            }
            else
            {
                Log.Trace($"(skipped setting redemption status for test redemption)");
            }
        }

        private async Task SetRedemptionStatusAsync(Redemption redemption, CustomRewardRedemptionStatus status)
        {
            try
            {
                await api.Helix.ChannelPoints.UpdateRedemptionStatusAsync(
                    redemption.ChannelId,
                    redemption.Reward.Id,
                    new List<string> {redemption.Id},
                    new UpdateCustomRewardRedemptionStatusRequest {Status = status},
                    authSettings.AccessToken
                );
                //Log.Info($"Set redemption status of {redemption.RedemptionId} ({redemption.RewardTitle} for {redemption.DisplayName}) to {status}");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to set redemption status of {redemption.Id} ({redemption.Reward.Title} for {redemption.User.DisplayName}) to {status}: {e.Message}");
            }
        }

        private void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            Log.LogFeedSystem("{=BiYZ1CbN}TwitchService connected".Translate());

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
            simTest = new (settings);
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
            Log.LogFeedSystem("{=mEcBdqNC}TwitchService stopped".Translate());
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