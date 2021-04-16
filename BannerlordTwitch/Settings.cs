using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Library;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using Formatting = Newtonsoft.Json.Formatting;

namespace BannerlordTwitch
{
    internal struct Reward
    {
        // Docs here https://dev.twitch.tv/docs/api/reference#create-custom-rewards
        public CreateCustomRewardsRequest RewardSpec;
        public string ActionId;
        public object ActionConfig;
    }
    
    internal struct Settings
    {
        public string Instructions;
        public string AccessToken;
        public Reward[] Rewards;

        public static Settings Load()
        {
            var filename = Path.Combine(Common.PlatformFileHelper.DocumentsPath, "Mount and Blade II Bannerlord", "Configs", "Bannerlord-Twitch.json");
            if (!File.Exists(filename))
            {
                var settings = new Settings {
                    Instructions = "Go to https://twitchtokengenerator.com/quick/8SINwcahZ4 to generate an access token, then paste it below. It will last 60 days. Keep it secret! If you think it was compromised then go to https://twitchtokengenerator.com and revoke it.",
                    AccessToken = "<paste your access token here>",
                    Rewards = new[] {
                        new Reward {
                            RewardSpec = new CreateCustomRewardsRequest
                            {
                                Title = "The title of the reward",
                                Prompt = "The prompt for the viewer when they are redeeming the reward",
                                Cost = 42,
                                IsEnabled = true,
                                BackgroundColor = "#7F2020",
                            },
                            ActionId = "ExampleAction",
                            ActionConfig = new {
                                a_string = "hello",
                                a_number = 42.0f,
                            }
                        }
                    }
                };
                File.WriteAllText(filename, JsonConvert.SerializeObject(settings, Formatting.Indented));
                return default;
            }
            else
            {
                return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(filename));
            }
        }
    }
}
