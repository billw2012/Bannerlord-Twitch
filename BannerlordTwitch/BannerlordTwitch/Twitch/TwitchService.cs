using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BannerlordTwitch.Dummy;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Testing;
using BannerlordTwitch.Util;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using static System.String;
using UserType = TwitchLib.Client.Enums.UserType;

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
        
        public static ReplyContext FromUser(ActionBase source, string userName) =>
            new()
            {
                UserName = userName,
                Source = source,
            };
    }
    
    // https://twitchtokengenerator.com/
    // https://twitchtokengenerator.com/quick/AAYotwZPvU
    internal partial class TwitchService
    {
        private TwitchPubSub pubSub;
        private readonly TwitchAPI api;
        private string channelId;
        private readonly AuthSettings authSettings;

        private Settings Settings { get; set; }

        private readonly ConcurrentDictionary<Guid, OnRewardRedeemedArgs> redemptionCache = new();
        private Bot bot;

        public TwitchService()
        {
            if (!LoadSettings())
            {
                return;
            }

            try
            {
                authSettings = AuthSettings.Load();
            }
            catch(Exception e)
            {
                Log.Error(e.ToString());
            }
            if (authSettings == null)
            {
                InformationManager.ShowInquiry(
                    new InquiryData(
                        "Bannerlord Twitch MOD DISABLED",
                        $"Failed to load auth settings, enable the BLTConfigure module and authorize the mod via the window",
                        true, false, "Okay", null,
                        () => {}, () => {}), true);
                Log.LogFeedCritical($"Failed to load auth settings, load the BLTConfigure module and authorize the mod via the window");
                return;
            }

            if (authSettings.DebugSpoofAffiliate)
            {
                affiliateSpoofing = new Dummy.AffiliateSpoofingHttpCallHandler();
                api = new TwitchAPI(http: affiliateSpoofing);
                affiliateSpoofing.OnRewardRedeemed += OnRewardRedeemed;
            }
            else
            {
                api = new TwitchAPI();
            }

            //api.Settings.Secret = SECRET;
            api.Settings.ClientId = authSettings.ClientID;
            api.Settings.AccessToken = authSettings.AccessToken;

            api.Helix.Users.GetUsersAsync(accessToken: authSettings.AccessToken).ContinueWith(t =>
            {
                MainThreadSync.Run(() =>
                {
                    if (t.IsFaulted)
                    {
                        Log.LogFeedFail($"Service init failed: {t.Exception?.Message}");
                        return;
                    }
                    
                    var user = t.Result.Users.First();

                    Log.Info($"Channel ID is {user.Id}");
                    channelId = user.Id;
                    
                    // Connect the chatbot
                    bot = new Bot(user.Login, authSettings, this);

                    if (IsNullOrEmpty(user.BroadcasterType))
                    {
                        Log.LogFeedFail($"Service init failed: you must be a twitch partner or affiliate to use the channel points system. Chat bot and testing are still functioning.");
                        return;
                    }
                    
                    // Create new instance of PubSub Client
                    pubSub = new TwitchPubSub();

                    // Subscribe to Events
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

        public bool LoadSettings()
        {
            try
            {
                Settings = Settings.Load();
                return true;
            }
            catch
            {
                // ignored
            }

            InformationManager.ShowInquiry(
                new InquiryData(
                    "Bannerlord Twitch MOD DISABLED",
                    $"Failed to load action/command settings, please enable the BLTConfigure module and use it to configure the mod",
                    true, false, "Okay", null,
                    () => {}, () => {}), true);
            Log.LogFeedCritical($"MOD DISABLED: Failed to load settings from settings file, please enable the BLTConfigure module and use it to configure the mod");

            return false;
        }
        
        public void Exit()
        {
            RemoveRewards();
            Log.Info($"Exiting");
        }
        
        private async void RegisterRewardsAsync()
        {
            var db = Db.Load();
            
            GetCustomRewardsResponse existingRewards = null;
            try
            {
                existingRewards = await api.Helix.ChannelPoints.GetCustomReward(channelId, accessToken: authSettings.AccessToken);
            }
            catch (Exception e)
            {
                Log.LogFeedFail($"ERROR: Couldn't retrieve existing rewards: {e.Message}");
            }

            bool anyFailed = false;
            foreach (var rewardDef in Settings.EnabledRewards.Where(r => existingRewards == null || existingRewards.Data.All(e => e.Title != r.RewardSpec?.Title)))
            {
                try
                {
                    var createdReward = (await api.Helix.ChannelPoints.CreateCustomRewards(channelId, rewardDef.RewardSpec.GetTwitchSpec(), authSettings.AccessToken)).Data.First();
                    Log.Info($"Created reward {createdReward.Title} ({createdReward.Id})");
                    db.RewardsCreated.Add(createdReward.Id);
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
                        () =>
                        {
                            // Can't get the file path directly since 1.5.10 ...
                            // string logDir = Path.Combine(Paths.ConfigPath, "..", "logs");
                            // try
                            // {
                            //     string logFile = Directory.GetFiles(logDir, "rgl_log_*.txt")
                            //         .FirstOrDefault(f => !f.Contains("errors"));
                            //     if (logFile != null)
                            //     {
                            //         // open with default editor
                            //         Process.Start(logFile);
                            //     }
                            //     else
                            //     {
                            //         Log.LogFeedFail($"ERROR: Couldn't find the log file at {logDir}");
                            //     }
                            // }
                            // catch
                            // {
                            //     // ignored
                            // }
                        }, () => {}), true);
            }
            
            Db.Save(db);
        }

        private void RemoveRewards()
        {
            var db = Db.Load();
            foreach (string rewardId in db.RewardsCreated.ToList())
            {
                try
                {
                    api.Helix.ChannelPoints.DeleteCustomReward(channelId, rewardId, accessToken: authSettings.AccessToken).Wait();
                    Log.Info($"Removed reward {rewardId}");
                    db.RewardsCreated.Remove(rewardId);
                }
                catch (Exception e)
                {
                    Log.Info($"Couldn't remove reward {rewardId}: {e.Message}");
                }
            }
            Db.Save(db);
        }

        private void OnRewardRedeemed(object sender, OnRewardRedeemedArgs redeemedArgs)
        {
            MainThreadSync.Run(() =>
            {
                var reward = Settings.Rewards.FirstOrDefault(r => r.RewardSpec.Title == redeemedArgs.RewardTitle);
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
                        Log.Error($"Couldn't enqueue redemption {redeemedArgs.RedemptionId}: RedemptionAction {reward.Handler} not found, check you have its Reward extension installed!");
                        // We DO cancel redemptions we know about, where the implementation is missing
                        RedemptionCancelled(context, $"Redemption action {reward.Handler} wasn't found");
                    }
                    else
                    {
                        Log.Info($"Redemption of {redeemedArgs.RewardTitle} from {redeemedArgs.DisplayName} received!");
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Exception happened while trying to enqueue redemption {redeemedArgs.RedemptionId}: {e.Message}");
                    RedemptionCancelled(context, $"Exception occurred: {e.Message}");
                }
            });
        }

        public void TestRedeem(string rewardName, string user, string message)
        {
            var reward = Settings?.EnabledRewards.FirstOrDefault(r => string.Equals(r.RewardSpec.Title, rewardName, StringComparison.CurrentCultureIgnoreCase));
            if (reward == null)
            {
                Log.Error($"Reward {rewardName} not found!");
                return;
            }
            affiliateSpoofing?.FakeRedeem(reward.RewardSpec.Title, user, message);
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
                Log.LogFeedResponse($"@{context.UserName}: " + Join("\n", messages));
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
                    Log.Trace($"[reply][{context.UserName}] {Join(" - ", messages)}");
                }
                else
                {
                    bot.SendChat(messages);
                    Log.Trace($"[chat] {Join(" - ", messages)}");
                }
            }
        }

        private void ShowCommandHelp()
        {
            string[] help = "Commands: ".Yield()
                .Concat(Settings.EnabledCommands.Where(c
                        => !c.HideHelp && !c.BroadcasterOnly && !c.ModOnly)
                    .Select(c => $"!{c.Name} - {c.Help}")
                ).ToArray();
            bot.SendChat(help);
        }
        
        public void RedemptionComplete(ReplyContext context, string info = null)
        {
            if (!redemptionCache.TryRemove(context.RedemptionId, out var redemption))
            {
                Log.Error($"RedemptionComplete failed: redemption {context.RedemptionId} not known!");
                return;
            }
            Log.Info($"Redemption of {redemption.RewardTitle} for {redemption.DisplayName} complete{(!IsNullOrEmpty(info) ? $": {info}" : "")}");
            ActionManager.SendReply(context, info);
            if (!IsNullOrEmpty(redemption.ChannelId))
            {
                SetRedemptionStatusAsync(redemption, CustomRewardRedemptionStatus.FULFILLED);
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
            Log.Info($"Redemption of {redemption.RewardTitle} for {redemption.DisplayName} cancelled{(!IsNullOrEmpty(reason) ? $": {reason}" : "")}");
            ActionManager.SendReply(context, reason);
            if (!IsNullOrEmpty(redemption.ChannelId))
            {
                SetRedemptionStatusAsync(redemption, CustomRewardRedemptionStatus.CANCELED);
            }
            else
            {
                Log.Trace($"(skipped setting redemption status for test redemption)");
            }
        }

        private async void SetRedemptionStatusAsync(OnRewardRedeemedArgs redemption, CustomRewardRedemptionStatus status)
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
                Log.Info($"Set redemption status of {redemption.RedemptionId} ({redemption.RewardTitle} for {redemption.DisplayName}) to {status}");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to set redemption status of {redemption.RedemptionId} ({redemption.RewardTitle} for {redemption.DisplayName}) to {status}: {e.Message}");
            }
        }

        private void OnPubSubServiceConnected(object sender, System.EventArgs e)
        {
            Log.LogFeedSystem("PubSub Service connected, now listening for rewards");

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
        
        public object FindGlobalConfig(string id) => Settings?.GlobalConfigs?.FirstOrDefault(c => c.Id == id)?.Config;

        private static SimulationTest simTest;
        private readonly AffiliateSpoofingHttpCallHandler affiliateSpoofing;

        public void StartSim()
        {
            StopSim();
            simTest = new SimulationTest(Settings);
        }
        
        public void StopSim()
        {
            simTest?.Stop();
            simTest = null;
        }
    }
}