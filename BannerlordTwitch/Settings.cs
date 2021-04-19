using System;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Library;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using Formatting = Newtonsoft.Json.Formatting;
#pragma warning disable 649

namespace BannerlordTwitch
{
    internal class Reward
    {
        // Docs here https://dev.twitch.tv/docs/api/reference#create-custom-rewards
        public CreateCustomRewardsRequest RewardSpec;
        public string ActionId;
        public JObject ActionConfig;
    }
    
    internal class Command
    {
        public string Cmd;
        public string Help;
        public string Handler;
        public JObject HandlerConfig;
    }
    
    internal class GlobalConfig
    {
        public string Id;
        public JObject Config;
    }

    [UsedImplicitly]
    internal class SimTestingItem
    {
        public string Type;
        public string Id;
        public string Args;
    }
    
    [UsedImplicitly]
    internal class SimTestingConfig
    {
        public int UserCount;
        public int UserStayTime;
        public int IntervalMinMS;
        public int IntervalMaxMS;
        public SimTestingItem[] Init;
        public SimTestingItem[] Use;
    }
    
    internal class Settings
    {
        // public string Instructions;
        public string AccessToken;
        public string ClientID;
        public string BotAccessToken;
        public string BotMessagePrefix;
        public Reward[] Rewards;
        public Command[] Commands;
        public GlobalConfig[] GlobalConfigs;
        public SimTestingConfig SimTesting;

        private static string SaveFilePath => Path.Combine(Common.PlatformFileHelper.DocumentsPath,
            "Mount and Blade II Bannerlord", "Configs", "Bannerlord-Twitch.jsonc");
        
        public static Settings Load()
        {
            var filename = SaveFilePath;
            if (!File.Exists(filename))
            {
                Save(new Settings {
                    AccessToken = "Go to https://twitchtokengenerator.com/quick/TgpaAFT9Sp to generate an ACCESS TOKEN (with the twitch account of your channel), then paste it here",
                    ClientID = "gp762nuuoqcoxypju8c569th9wz7q5",
                    BotAccessToken = "Either use the same access token as above (the bot will have your name), or sign into the twitch account you want to use as a bot, go to https://twitchtokengenerator.com/quick/0iN22Qaitu to generate an ACCESS TOKEN, and paste it here",
                    BotMessagePrefix = "(BLT)",
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
                            ActionConfig = JObject.FromObject( 
                                new {
                                    a_string = "hello",
                                    a_number = 42.0f,
                                })
                        }
                    },
                    Commands = new []
                    {
                        new Command
                        {
                            Cmd = "chat command name goes here, no spaces",
                            Help = "short description of the command, no longer than this",
                            Handler = "registered handler name, from whatever extensions you have installed",
                            HandlerConfig = JObject.FromObject( 
                                new {
                                    a_string = "hello",
                                    a_number = 42.0f,
                                })
                        }
                    },
                    GlobalConfigs = new []
                    {
                        new GlobalConfig
                        {
                            Id = "these can be accessed by actions and commands, to allow plugins to have shared config across reward specs",
                            Config = JObject.FromObject( 
                                new {
                                    a_string = "hello",
                                    a_number = 42.0f,
                                })
                        }
                    }
                });
                return default;
            }
            else
            {
                var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(filename));
                if (settings == null)
                    return null;
                
                // Fix up the settings to avoid NullReferences etc
                settings.Rewards ??= new Reward[] { };
                settings.Commands ??= new Command[] { };
                settings.GlobalConfigs ??= new GlobalConfig[] { };

                foreach (var reward in settings.Rewards)
                {
                    if (reward.RewardSpec == null)
                    {
                        throw new FormatException($"A reward is missing a RewardSpec");
                    }
                    if (reward.ActionId == null)
                    {
                        throw new FormatException($"A reward is missing an ActionId");
                    }
                    reward.RewardSpec.IsMaxPerStreamEnabled = reward.RewardSpec.MaxPerStream.HasValue;
                    reward.RewardSpec.IsMaxPerUserPerStreamEnabled = reward.RewardSpec.MaxPerUserPerStream.HasValue;
                    reward.RewardSpec.IsGlobalCooldownEnabled = reward.RewardSpec.GlobalCooldownSeconds.HasValue;
                }
                
                return settings;
            }
        }

        public static void Save(Settings settings)
        {
            var filename = SaveFilePath;
            File.WriteAllText(filename, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
    }
}
