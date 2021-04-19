using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Testing;
using BannerlordTwitch.Util;
using Newtonsoft.Json.Linq;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Client.Extensions;
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
                    Log.ScreenCritical($"MOD DISABLED: Failed to load settings from settings file, please check the formatting");
                    return;
                }
            }
            catch (Exception e)
            {
                Log.ScreenCritical(
                    $"MOD DISABLED: Failed to load settings from settings file, please check the formatting ({e.Message})");
                return;
            }

            api = new TwitchAPI();
            //api.Settings.Secret = SECRET;
            api.Settings.ClientId = settings.ClientID;
            api.Settings.AccessToken = settings.AccessToken;

            api.Helix.Users.GetUsersAsync(accessToken: settings.AccessToken).ContinueWith(t =>
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
                    bot = new Bot(user.Login, settings);

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

                    RegisterRewardsAsync();

                    // Connect
                    pubSub.Connect();
                });
            });
        }

        private async void RegisterRewardsAsync()
        {
            GetCustomRewardsResponse existingRewards = null;
            try
            {
                existingRewards = await api.Helix.ChannelPoints.GetCustomReward(channelId, accessToken: settings.AccessToken);
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't retrieve existing rewards: {e.Message}");
            }
            
            // Can't really do this, channel might have rewards created by other people using the twitchtokengenerator
            // foreach (var reward in rewards?.Data ?? Enumerable.Empty<CustomReward>())
            // {
            //     try
            //     {
            //         await api.Helix.ChannelPoints.DeleteCustomReward(channelId, reward.Id, accessToken: settings.AccessToken);
            //         Log.Info($"Removed reward {reward.Title} ({reward.Id})");
            //     }
            //     catch (Exception e)
            //     {
            //         Log.Info($"Couldn't remove reward {reward.Title} ({reward.Id}): {e.Message}");
            //     }
            // }
            
            foreach (var rewardDef in settings.Rewards.Where(r => existingRewards == null || !existingRewards.Data.Any(e => e.Title == r.RewardSpec?.Title)))
            {
                try
                {
                    var createdReward = (await api.Helix.ChannelPoints.CreateCustomRewards(channelId, rewardDef.RewardSpec, settings.AccessToken)).Data.First();
                    rewardMap.TryAdd(createdReward.Id, rewardDef);
                    Log.Info($"Created reward {createdReward.Title} ({createdReward.Id})");
                }
                catch (Exception e)
                {
                    Log.Error($"Couldn't create reward {rewardDef.RewardSpec.Title}: {e.Message}");
                }
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

                try
                {
                    redemptionCache.TryAdd(redeemedArgs.RedemptionId, redeemedArgs);
                    if (!RewardManager.Enqueue(reward.ActionId, redeemedArgs.RedemptionId, redeemedArgs.Message, redeemedArgs.DisplayName, reward.ActionConfig))
                    {
                        Log.Error($"Couldn't enqueue redemption {redeemedArgs.RedemptionId}: RedemptionAction {reward.ActionId} not found, check you have its Reward extension installed!");
                        // We DO cancel redemptions we know about, where the implementation is missing
                        RedemptionCancelled(redeemedArgs.RedemptionId, $"Redemption action {reward.ActionId} wasn't found");
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

        // public void TestRandomRewardOrCommand(string user, string args)
        // {
        //     var randomRewardOrCmd = settings.Rewards.Cast<object>().Concat(settings.Commands).SelectRandom();
        //
        //     switch (randomRewardOrCmd)
        //     {
        //         case Reward reward:
        //             TestRedeem(reward.RewardSpec.Title, user, args);
        //             break;
        //         case Command command:
        //             var cmd = settings.Commands.FirstOrDefault(c => c.Cmd == command.Cmd);
        //             if (cmd != null)
        //             {
        //                 RewardManager.Command(cmd.Handler, args, user, null, cmd.Config);
        //             }
        //             break;
        //     }
        // }

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

            RewardManager.Enqueue(reward.ActionId, guid, message, user, reward.ActionConfig);
        }

        public void ShowMessage(string screenMsg, string botMsg, string userToAt)
        {
            Log.Screen(screenMsg);
            SendChat($"@{userToAt}: {botMsg}");
        }
        
        public void ShowMessageFail(string screenMsg, string botMsg, string userToAt)
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
        
        public void ShowCommandHelp(string replyId)
        {
            bot.SendReply(replyId, "Commands: ".Yield()
                .Concat(settings.Commands
                    .Select(c => $"!{c.Cmd} - {c.Help}")
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
                    settings.AccessToken
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
            Log.Info("PubSub Service Connected!");

#pragma warning disable 618
            // Obsolete warning disabled because no new version has yet been written!
            pubSub.ListenToRewards(channelId);
#pragma warning restore 618
        }

        public JObject FindGlobalConfig(string id) => settings?.GlobalConfigs?.FirstOrDefault(c => c.Id == id)?.Config;

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