using System;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
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
        public CommandMessage command;
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

    internal class AuthSettings
    {
        public string AccessToken;
        public string ClientID;
        public string BotAccessToken;
        public string BotMessagePrefix;
        
        private static string SaveFilePath => Path.Combine(Common.PlatformFileHelper.DocumentsPath,
            "Mount and Blade II Bannerlord", "Configs", "Bannerlord-Twitch-Auth.jsonc");

        public static AuthSettings Load()
        {
            if (!File.Exists(SaveFilePath))
            {
                var templateFileName = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "..", "..",
                    Path.GetFileName(SaveFilePath));
                File.Copy(templateFileName, SaveFilePath);
                InformationManager.ShowInquiry(
                    new InquiryData(
                        "Bannerlord Twitch",
                        $"Auth settings file created, please close the game, and edit it at {SaveFilePath}",
                        true, false, "Okay", null,
                        () => {}, () => {}), true);
            }
            return JsonConvert.DeserializeObject<AuthSettings>(File.ReadAllText(SaveFilePath));
        }
    }
    
    internal class Settings
    {
        // public string Instructions;

        public Reward[] Rewards;
        public Command[] Commands;
        public GlobalConfig[] GlobalConfigs;
        public SimTestingConfig SimTesting;

        private static string SaveFilePath => Path.Combine(Common.PlatformFileHelper.DocumentsPath,
            "Mount and Blade II Bannerlord", "Configs", "Bannerlord-Twitch.jsonc");
        
        public static Settings Load()
        {
            if (!File.Exists(SaveFilePath))
            {
                var templateFileName = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "..", "..",
                    Path.GetFileName(SaveFilePath));
                File.Copy(templateFileName, SaveFilePath);
                InformationManager.ShowInquiry(
                    new InquiryData(
                        "Bannerlord Twitch",
                        $"Reward/Command settings file created, please close the game, and edit it at {SaveFilePath}",
                        true, false, "Okay", null,
                    () => {}, () => {}), true);
            }
            
            var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SaveFilePath));
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

        // public static void Save(Settings settings)
        // {
        //     var filename = SaveFilePath;
        //     File.WriteAllText(filename, JsonConvert.SerializeObject(settings, Formatting.Indented));
        // }
    }
}
