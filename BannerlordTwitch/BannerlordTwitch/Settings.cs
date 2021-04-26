using System;
using System.Collections.Generic;
using System.IO;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using YamlDotNet.Serialization;

#pragma warning disable 649

namespace BannerlordTwitch
{
    [Desc("Channel points reward definition")]
    internal class Reward
    {
        [Desc("Channel points reward definition")]
        public RewardSpec RewardSpec;
        [Desc("Name of the BLT action")]
        public string Action;
        [Desc("Custom config for the BLT action")]
        public object ActionConfig;
    }
    
    // Docs here https://dev.twitch.tv/docs/api/reference#create-custom-rewards
    [Desc("The Twitch specific part of the channel points reward specification")]
    internal class RewardSpec
    {
        [Desc("The title of the reward")]
        public string Title;
        [Desc("The prompt for the viewer when they are redeeming the reward")]
        public string Prompt;
        [Desc("The cost of the reward")]
        public int Cost;
        [Desc("Is the reward currently enabled, if false the reward won’t show up to viewers. Defaults true")]
        public bool IsEnabled;
        [Desc("Custom background color for the reward. Format: Hex with # prefix. Example: #00E5CB")]
        public string BackgroundColor;
        [Desc("Does the user need to enter information when redeeming the reward. Defaults false")]
        public bool IsUserInputRequired;
        [Desc("The maximum number per stream if enabled")]
        public int? MaxPerStream;
        [Desc("The maximum number per user per stream if enabled")]
        public int? MaxPerUserPerStream;
        [Desc("The cooldown in seconds if enabled")]
        public int? GlobalCooldownSeconds;

        public CreateCustomRewardsRequest GetTwitchSpec() =>
            new()
            {
                BackgroundColor = BackgroundColor,
                Cost = Cost,
                GlobalCooldownSeconds = GlobalCooldownSeconds,
                IsEnabled = IsEnabled,
                IsGlobalCooldownEnabled = GlobalCooldownSeconds.HasValue,
                IsMaxPerStreamEnabled = MaxPerStream.HasValue,
                IsMaxPerUserPerStreamEnabled = MaxPerUserPerStream.HasValue,
                IsUserInputRequired = IsUserInputRequired,
                MaxPerStream = MaxPerStream,
                MaxPerUserPerStream = MaxPerUserPerStream,
                Prompt = Prompt,
                ShouldRedemptionsSkipRequestQueue = false,
                Title = Title
            };
    }
    
    [Desc("Bot command definition")]
    internal class Command
    {
        [Desc("The command itself, not including the !")]
        public string Name;
        [Desc("Hides the help for the command from the !help action")]
        public bool HideHelp;
        [Desc("Only allows the broadcaster to use this command, and hides it from !help")]
        public bool BroadcasterOnly;
        [Desc("Only allows the mods or broadcaster to use this command, and hides it from !help")]
        public bool ModOnly;
        [Desc("What to show in the !help command")]
        public string Help;
        [Desc("The name of the BLT command handler")]
        public string Handler;
        [Desc("The custom config for the command handler")]
        public object HandlerConfig;
    }
    
    internal class GlobalConfig
    {
        public string Id;
        public object Config;
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
        public object Test;
        
        private static string SaveFilePath => Path.Combine(Common.PlatformFileHelper.DocumentsPath,
            "Mount and Blade II Bannerlord", "Configs", "Bannerlord-Twitch-Auth.yaml");

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
            return new DeserializerBuilder().Build().Deserialize<AuthSettings>(File.ReadAllText(SaveFilePath));
        }
    }
    
    internal class Db
    {
#pragma warning disable 414
        public int Version = 1;
#pragma warning restore 414
        public List<string> RewardsCreated = new();
        
        private static string DbFilePath => Path.Combine(Common.PlatformFileHelper.DocumentsPath,
            "Mount and Blade II Bannerlord", "Configs", "Bannerlord-Twitch-Db.yaml");

        public static Db Load()
        {
            return !File.Exists(DbFilePath) 
                ? new Db()
                : new DeserializerBuilder().Build().Deserialize<Db>(File.ReadAllText(DbFilePath));
        }
        
        public static void Save(Db db) => File.WriteAllText(DbFilePath, new SerializerBuilder().Build().Serialize(db));
    }
    
    internal class Settings
    {
        // public string Instructions;

        public Reward[] Rewards;
        public Command[] Commands;
        public GlobalConfig[] GlobalConfigs;
        public SimTestingConfig SimTesting;

        private static string SaveFilePath => Path.Combine(Common.PlatformFileHelper.DocumentsPath,
            "Mount and Blade II Bannerlord", "Configs", "Bannerlord-Twitch.yaml");
        
        public static Settings Load()
        {
#if RELEASE
            if (!File.Exists(SaveFilePath))
#endif
            {
                var templateFileName = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "..", "..",
                    Path.GetFileName(SaveFilePath));
                File.Copy(templateFileName, SaveFilePath, overwrite: true);
                InformationManager.ShowInquiry(
                    new InquiryData(
                        "Bannerlord Twitch",
                        $"Reward/Command settings file created, please close the game, and edit it at {SaveFilePath}",
                        true, false, "Okay", null,
                    () => {}, () => {}), true);
            }
            var deserializer = new DeserializerBuilder().Build();
            var settings = deserializer.Deserialize<Settings>(File.ReadAllText(SaveFilePath));
            //var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SaveFilePath));
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
                if (reward.Action == null)
                {
                    throw new FormatException($"A reward is missing an ActionId");
                }
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
