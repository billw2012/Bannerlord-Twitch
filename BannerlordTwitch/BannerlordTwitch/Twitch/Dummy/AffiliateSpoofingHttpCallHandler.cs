﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.HttpCallHandlers;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.PubSub.Events;

namespace BannerlordTwitch.Dummy
{
    // Usage:
    // var affiliateSpoofing = new Dummy.AffiliateSpoofingHttpCallHandler();
    // api = new TwitchAPI(http: affiliateSpoofing);
    // affiliateSpoofing.OnRewardRedeemed += OnRewardRedeemed;
    //
    // Then add/remove rewards via the normal API.
    // Then test redemption like:
    // affiliateSpoofing?.FakeRedeem("My Reward Title", "fake_user_name", "text");
    public class AffiliateSpoofingHttpCallHandler : TwitchRedirectingHttpCallHandler
    {
        private class CustomReward : TwitchLib.Api.Helix.Models.ChannelPoints.CustomReward
        {
            public CustomReward(CreateCustomRewardsRequest from)
            {
                Id = Guid.NewGuid().ToString();
                Title = from.Title;
                Prompt = from.Prompt;
                Cost = from.Cost;
                IsEnabled = from.IsEnabled;
                BackgroundColor = from.BackgroundColor;
                IsUserInputRequired = from.IsUserInputRequired;
                // IsMaxPerStreamEnabled = from.IsMaxPerStreamEnabled;
                // MaxPerStream = from.MaxPerStream;
                // IsMaxPerUserPerStreamEnabled = from.IsMaxPerUserPerStreamEnabled;
                // MaxPerUserPerStream = from.MaxPerUserPerStream;
                // IsGlobalCooldownEnabled = from.IsGlobalCooldownEnabled;
                // GlobalCooldownSeconds = from.GlobalCooldownSeconds;
                ShouldRedemptionsSkipQueue = from.ShouldRedemptionsSkipRequestQueue;
            }
        }

        private class Redemption : RewardRedemption
        {
            public Redemption(string id, string user, string args, string rewardId, string rewardTitle)
            {
                Id = id;
                UserLogin = user;
                UserName = user;
                UserInput = args;
                Status = CustomRewardRedemptionStatus.UNFULFILLED;
                RedeemedAt = DateTime.Now;
                Reward = JsonConvert.DeserializeObject<TwitchLib.Api.Helix.Models.ChannelPoints.Reward>(
                    JsonConvert.SerializeObject(new { id = rewardId, title = rewardTitle }) );
            }
        }
        
        private readonly List<CustomReward> customRewards = new ();
        private readonly List<Redemption> activeRedemptions = new ();
        
        private readonly JsonSerializerSettings deserializerSettings = new() { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore };

        public event EventHandler<OnChannelPointsRewardRedeemedArgs> OnRewardRedeemed;
        
        public AffiliateSpoofingHttpCallHandler(IHttpCallHandler http = null, ILogger<TwitchHttpClient> logger = null) : base(http, logger)
        {
            AddRedirect(ApiVersion.Helix, "/channel_points/custom_rewards", "POST", CreateCustomRewardsAsync);
            AddRedirect(ApiVersion.Helix, "/channel_points/custom_rewards", "DELETE", DeleteCustomRewardsAsync);
            AddRedirect(ApiVersion.Helix, "/channel_points/custom_rewards", "GET", GetCustomRewardsAsync);
            AddRedirect(ApiVersion.Helix, "/users", "GET", GetUsersAsync);
            AddRedirect(ApiVersion.Helix, "/channel_points/custom_rewards/redemptions", "PATCH", PatchRedemptionsAsync);
        }

        public bool FakeRedeem(string title, string user, string args)
        {
            var reward = customRewards.FirstOrDefault(r => r.Title == title);
            if (reward != null)
            {
                var id = Guid.NewGuid();
                activeRedemptions.Add(new Redemption(id.ToString(), user, args, reward.Id, reward.Title));
                // TODO: fix
                // OnRewardRedeemed?.Invoke(this, new OnChannelPointsRewardRedeemedArgs {
                //     
                //     ChannelId = user + "channel",
                //     Login = user,
                //     DisplayName = user,
                //     Message = args,
                //     RewardId = Guid.Parse(reward.Id),
                //     RewardTitle = reward.Title, 
                //     Status = "UNFULFILLED",
                //     RedemptionId = id,
                //     });
                return true;
            }

            return false;
        }
        
        private async Task<KeyValuePair<int, string>> GetUsersAsync(string payload, string clientid, string accesstoken, Func<Task<KeyValuePair<int, string>>> realcall, Dictionary<string, string[]> urlparams)
        {
            var results = await realcall();
            if (urlparams.Count == 0)
            {
                var response = JObject.Parse(results.Value);
                response["data"][0]["broadcaster_type"] = new JValue("affiliate");
                return new KeyValuePair<int, string>(200, JsonConvert.SerializeObject(response, deserializerSettings));
            }
            return results;
        }

        private Task<KeyValuePair<int, string>> CreateCustomRewardsAsync(string payload, string clientid, string accesstoken, Func<Task<KeyValuePair<int, string>>> realcall, Dictionary<string, string[]> urlParams)
        {
            var reward = JsonConvert.DeserializeObject<CreateCustomRewardsRequest>(payload);
            var newReward = new CustomReward(reward);
            customRewards.Add(newReward);
            return Task.FromResult(new KeyValuePair<int, string>(
                200,
                JsonConvert.SerializeObject(new
                {
                    data = new [] { newReward }
                }, deserializerSettings)));
        }

        private Task<KeyValuePair<int, string>> DeleteCustomRewardsAsync(string payload, string clientid, string accesstoken, Func<Task<KeyValuePair<int, string>>> realcall, Dictionary<string, string[]> urlparams)
        {
            if (customRewards.RemoveAll(c => c.Id == urlparams["id"].FirstOrDefault()) != 0)
            {
                return Task.FromResult(new KeyValuePair<int, string>(204, ""));
            }
            else
            {
                return Task.FromResult(new KeyValuePair<int, string>(404, ""));
            }
        }

        private Task<KeyValuePair<int, string>> GetCustomRewardsAsync(string payload, string clientid, string accesstoken, Func<Task<KeyValuePair<int, string>>> realcall, Dictionary<string, string[]> urlparams)
        {
            string id = urlparams.ContainsKey("id")
                ? urlparams["id"].FirstOrDefault()
                : null;
            var rewards = customRewards.Where(c => id == null || c.Id == id).ToArray();
            if (rewards.Length == 0)
            {
                return Task.FromResult(new KeyValuePair<int, string>(404, ""));
            }
            return Task.FromResult(new KeyValuePair<int, string>(
                200,
                JsonConvert.SerializeObject(new
                {
                    data = rewards
                }, deserializerSettings)));
        }

        private Task<KeyValuePair<int, string>> PatchRedemptionsAsync(string payload, string clientid, string accesstoken, Func<Task<KeyValuePair<int, string>>> realcall, Dictionary<string, string[]> urlparams)
        {
            string[] ids = urlparams["id"];
            // Just assume we are cancelling or redeeming...
            activeRedemptions.RemoveAll(r => ids.Contains(r.Id));
            // var request = JsonConvert.DeserializeObject<UpdateCustomRewardRedemptionStatusRequest>(payload, deserializerSettings);
            // foreach (string id in ids)
            // {
            //     var redemption = activeRedemptions.FirstOrDefault(r => r.Id == id);
            //     if (redemption != null)
            //     {
            //         redemption.Status = request.Status;
            //     }
            // }
            return Task.FromResult(new KeyValuePair<int, string>(200, @"{ ""data"": [] }"));
        }
    }
}