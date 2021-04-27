using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Testing;
using BannerlordTwitch.Util;
using TaleWorlds.Core;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace BannerlordTwitch
{
    // https://twitchtokengenerator.com/
    // https://twitchtokengenerator.com/quick/AAYotwZPvU
    internal partial class TwitchService
    {
        private TwitchPubSub pubSub;
        private readonly TwitchAPI api;
        private string channelId;
        private readonly AuthSettings authSettings;
        private readonly Settings settings;

        private ConcurrentDictionary<Guid, OnRewardRedeemedArgs> redemptionCache = new ConcurrentDictionary<Guid, OnRewardRedeemedArgs>();
        private ConcurrentDictionary<string, Reward> rewardMap = new ConcurrentDictionary<string, Reward>();
        private Bot bot;

        public TwitchService()
        {
            // To keep the Unity application active in the background, you can enable "Run In Background" in the player settings:
            // Unity Editor --> Edit --> Project Settings --> Player --> Resolution and Presentation --> Resolution --> Run In Background
            // This option seems to be enabled by default in more recent versions of Unity. An aditional, less recommended option is to set it in code:
            // Application.runInBackground = true;
            try
            {
                settings = Settings.Load();
                if (settings == null)
                {
                    InformationManager.ShowInquiry(
                        new InquiryData(
                            "Bannerlord Twitch MOD DISABLED",
                            $"Failed to load action/command settings, please check the formatting in Bannerlord-Twitch.jsonc",
                            true, false, "Okay", null,
                            () => {}, () => {}), true);
                    Log.ScreenCritical($"MOD DISABLED: Failed to load settings from settings file, please check the formatting");
                    return;
                }
            }
            catch (Exception e)
            {
                InformationManager.ShowInquiry(
                    new InquiryData(
                        "Bannerlord Twitch MOD DISABLED",
                        $"Failed to load action/command settings, please check the formatting in Bannerlord-Twitch.jsonc\n{e.Message}",
                        true, false, "Okay", null,
                        () => {}, () => {}), true);
                Log.ScreenCritical(
                    $"MOD DISABLED: Failed to load settings from settings file, please check the formatting ({e.Message})");
                return;
            }
            try
            {
                authSettings = AuthSettings.Load();
                if (authSettings == null)
                {
                    InformationManager.ShowInquiry(
                        new InquiryData(
                            "Bannerlord Twitch MOD DISABLED",
                            $"Failed to load auth settings, please check the formatting in Bannerlord-Twitch-Auth.jsonc",
                            true, false, "Okay", null,
                            () => {}, () => {}), true);
                    Log.ScreenCritical($"MOD DISABLED: Failed to load auth settings from the auth file, please check the formatting");
                    return;
                }
            }
            catch(Exception e)
            {
                // Don't show the exception message, it might leak something
                InformationManager.ShowInquiry(
                    new InquiryData(
                        "Bannerlord Twitch MOD DISABLED",
                        $"Failed to load auth settings, please check the formatting in Bannerlord-Twitch-Auth.jsonc",
                        true, false, "Okay", null,
                        () => {}, () => {}), true);
                Log.ScreenCritical(
                    $"MOD DISABLED: Failed to load auth settings from the auth file, please check the formatting");
                Log.Error(e.ToString());
                return;
            }

            api = new TwitchAPI();
            //api.Settings.Secret = SECRET;
            api.Settings.ClientId = authSettings.ClientID;
            api.Settings.AccessToken = authSettings.AccessToken;

            api.Helix.Users.GetUsersAsync(accessToken: authSettings.AccessToken).ContinueWith(t =>
            {
                MainThreadSync.Run(() =>
                {
                    if (t.IsFaulted)
                    {
                        Log.ScreenFail($"Service init failed: {t.Exception?.Message}");
                        return;
                    }
                    
                    var user = t.Result.Users.First();

                    Log.Info($"Channel ID is {user.Id}");
                    channelId = user.Id;
                    
                    // Connect the chatbot
                    bot = new Bot(user.Login, authSettings, settings);

                    if (string.IsNullOrEmpty(user.BroadcasterType))
                    {
                        Log.ScreenFail($"Service init failed: you must be a twitch partner or affiliate to use the channel points system. Chat bot and testing are still functioning.");
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

        ~TwitchService()
        {
            if (settings.DeleteRewardsOnExit)
            {
                RemoveRewards();
            }
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
                Log.Error($"Couldn't retrieve existing rewards: {e.Message}");
            }

            foreach (var rewardDef in settings.Rewards.Where(r => existingRewards == null || existingRewards.Data.All(e => e.Title != r.RewardSpec?.Title)))
            {
                try
                {
                    var createdReward = (await api.Helix.ChannelPoints.CreateCustomRewards(channelId, rewardDef.RewardSpec.GetTwitchSpec(), authSettings.AccessToken)).Data.First();
                    rewardMap.TryAdd(createdReward.Id, rewardDef);
                    Log.Info($"Created reward {createdReward.Title} ({createdReward.Id})");
                    db.RewardsCreated.Add(createdReward.Id);
                }
                catch (Exception e)
                {
                    Log.Error($"Couldn't create reward {rewardDef.RewardSpec.Title}: {e.Message}");
                }
            }
            
            Db.Save(db);
        }

        private void RemoveRewards()
        {
            var db = Db.Load();
            foreach (var reward in db.RewardsCreated)
            {
                try
                {
                    api.Helix.ChannelPoints.DeleteCustomReward(channelId, reward, accessToken: authSettings.AccessToken).Wait();
                    Log.Info($"Removed reward {reward}");
                }
                catch (Exception e)
                {
                    Log.Info($"Couldn't remove reward {reward}: {e.Message}");
                }
            }
            db.RewardsCreated.Clear();
            Db.Save(db);
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

                try
                {
                    redemptionCache.TryAdd(redeemedArgs.RedemptionId, redeemedArgs);
                    if (!RewardManager.Enqueue(reward.Action, redeemedArgs.RedemptionId, redeemedArgs.Message, redeemedArgs.DisplayName, reward.ActionConfig))
                    {
                        Log.Error($"Couldn't enqueue redemption {redeemedArgs.RedemptionId}: RedemptionAction {reward.Action} not found, check you have its Reward extension installed!");
                        // We DO cancel redemptions we know about, where the implementation is missing
                        RedemptionCancelled(redeemedArgs.RedemptionId, $"Redemption action {reward.Action} wasn't found");
                    }
                    else
                    {
                        Log.Screen($"Redemption of {redeemedArgs.RewardTitle} from {redeemedArgs.DisplayName} received!");
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Exception happened while trying to enqueue redemption {redeemedArgs.RedemptionId}: {e.Message}");
                    RedemptionCancelled(redeemedArgs.RedemptionId, $"Exception occurred: {e.Message}");
                }
            });
        }

        public void TestRedeem(string rewardName, string user, string message)
        {
            var reward = settings?.Rewards.FirstOrDefault(r => r.RewardSpec.Title == rewardName);
            if (reward == null)
            {
                Log.Error($"Reward {rewardName} not found!");
                return;
            }

            var guid = Guid.NewGuid();
            redemptionCache.TryAdd(guid, new OnRewardRedeemedArgs
            {
                RedemptionId = guid,
                Message = message,
                DisplayName = user,
                Login = user,
                RewardTitle = rewardName,
                ChannelId = null,
            });

            RewardManager.Enqueue(reward.Action, guid, message, user, reward.ActionConfig);
        }

        private void ShowMessage(string screenMsg, string botMsg, string userToAt)
        {
            Log.Screen(screenMsg);
            SendChat($"@{userToAt}: {botMsg}");
        }

        private void ShowMessageFail(string screenMsg, string botMsg, string userToAt)
        {
            Log.ScreenFail(screenMsg);
            SendChat($"@{userToAt}: {botMsg}");
        }

        public void SendChat(params string[] message)
        {
            Log.Trace($"[chat] {string.Join(" - ", message)}");
            bot.SendChat(message);
        }

        public void SendReply(string replyId, params string[] message)
        {
            Log.Trace($"[chat] {replyId}->{string.Join(" - ", message)}");
            bot.SendReply(replyId, message);
        }

        private void ShowCommandHelp(string replyId)
        {
            bot.SendReply(replyId, "Commands: ".Yield()
                .Concat(settings.Commands.Where(c 
                        => !c.HideHelp && !c.BroadcasterOnly && !c.ModOnly)
                    .Select(c => $"!{c.Name} - {c.Help}")
                ).ToArray());
        }
        
        public void RedemptionComplete(Guid redemptionId, string info = null)
        {
            if (!redemptionCache.TryRemove(redemptionId, out var redemption))
            {
                Log.Error($"RedemptionComplete failed: redemption {redemptionId} not known!");
                return;
            }
            ShowMessage($"Redemption of {redemption.RewardTitle} for {redemption.DisplayName} complete" + 
                        (!string.IsNullOrEmpty(info) ? $": {info}" : ""), info, redemption.Login);
            if (!string.IsNullOrEmpty(redemption.ChannelId))
            {
                SetRedemptionStatusAsync(redemption, CustomRewardRedemptionStatus.FULFILLED);
            }
            else
            {
                Log.Trace($"(skipped setting redemption status for test redemption)");
            }
        }

        public void RedemptionCancelled(Guid redemptionId, string reason = null)
        {
            if (!redemptionCache.TryRemove(redemptionId, out var redemption))
            {
                Log.Error($"RedemptionCancelled failed: redemption {redemptionId} not known!");
                return;
            }
            ShowMessageFail($"Redemption of {redemption.RewardTitle} for {redemption.DisplayName} cancelled" + 
                            (!string.IsNullOrEmpty(reason) ? $": {reason}" : ""), reason, redemption.Login);
            if (!string.IsNullOrEmpty(redemption.ChannelId))
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
            Log.Screen("PubSub Service connected, now listening for rewards");

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
        
        public void StartSim()
        {
            StopSim();
            simTest = new SimulationTest(settings);
        }
        
        public void StopSim()
        {
            simTest?.Stop();
            simTest = null;
        }
    }
}