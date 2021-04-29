using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using YamlDotNet.Serialization;
using Path = System.IO.Path;

// ReSharper disable MemberCanBePrivate.Global
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
    [UsedImplicitly]
    internal class RewardSpec
    {
        [Desc("The title of the reward")]
        public string Title;
        [Desc("The prompt for the viewer when they are redeeming the reward")]
        public string Prompt;
        [Desc("The cost of the reward")]
        public int Cost;
        [Desc("Is the reward currently enabled, if false the reward won’t show up to viewers. Defaults to true if not specified.")]
        public bool? IsEnabled;
        [Desc("Custom background color for the reward. Format: Hex with # prefix. Example: #00E5CB.")]
        public string BackgroundColor;
        [Desc("Does the user need to enter information when redeeming the reward. Defaults false.")]
        public bool IsUserInputRequired;
        [Desc("The maximum number per stream, defaults to unlimited")]
        public int? MaxPerStream;
        [Desc("The maximum number per user per stream, defaults to unlimited")]
        public int? MaxPerUserPerStream;
        [Desc("The cooldown in seconds, defaults to unlimited")]
        public int? GlobalCooldownSeconds;

        public CreateCustomRewardsRequest GetTwitchSpec() =>
            new()
            {
                Title = Title,
                Cost = Cost,
                IsEnabled = IsEnabled ?? true,
                BackgroundColor = BackgroundColor,
                IsUserInputRequired = IsUserInputRequired,
                Prompt = Prompt,
                // as we are performing the redemption we don't want to skip the queue
                ShouldRedemptionsSkipRequestQueue = false,
                IsGlobalCooldownEnabled = GlobalCooldownSeconds.HasValue,
                GlobalCooldownSeconds = GlobalCooldownSeconds,
                IsMaxPerStreamEnabled = MaxPerStream.HasValue,
                MaxPerStream = MaxPerStream,
                IsMaxPerUserPerStreamEnabled = MaxPerUserPerStream.HasValue,
                MaxPerUserPerStream = MaxPerUserPerStream,
            };
    }
    
    [Desc("Bot command definition")]
    internal class Command
    {
        [Desc("The command itself, not including the !")]
        public string Name;
        [Desc("Hides the command from the !help action")]
        public bool HideHelp;
        [Desc("What to show in the !help command")]
        public string Help;
        [Desc("Only allows the broadcaster to use this command, and hides it from !help")]
        public bool BroadcasterOnly;
        [Desc("Only allows the mods or broadcaster to use this command, and hides it from !help")]
        public bool ModOnly;
        [Desc("The name of the BLT command handler")]
        public string Handler;
        [Desc("The custom config for the command handler")]
        public object HandlerConfig;
    }
    
    [UsedImplicitly]
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

    [UsedImplicitly]
    internal class AuthSettings
    {
        public string AccessToken;
        public string ClientID;
        public string BotAccessToken;
        public string BotMessagePrefix;
        public object Test;
        
        private static string AuthFilePath => Path.Combine(Common.PlatformFileHelper.DocumentsPath,
            "Mount and Blade II Bannerlord", "Configs", "Bannerlord-Twitch-Auth.yaml");

        public static AuthSettings Load()
        {
            if (!File.Exists(AuthFilePath))
            {
                string templateFileName = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "..", "..",
                    Path.GetFileName(AuthFilePath));
                File.Copy(templateFileName, AuthFilePath);
                InformationManager.ShowInquiry(
                    new InquiryData(
                        "Bannerlord Twitch",
                        $"Auth settings file created at {AuthFilePath}, you need to edit it to provide authorization. Press Okay to exit now and edit the file.",
                        true, false, "Okay", null,
                        () => { 
                            Utilities.DoDelayedexit(0);
                            Process.Start("explorer.exe", $"/select, \"{AuthFilePath}\"");
                            Process.Start(AuthFilePath);
                        }, () => {}), true);
            }
            return new DeserializerBuilder().Build().Deserialize<AuthSettings>(File.ReadAllText(AuthFilePath));
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
        public bool DeleteRewardsOnExit;
        public Reward[] Rewards;
        public Command[] Commands;
        public GlobalConfig[] GlobalConfigs;
        public SimTestingConfig SimTesting;

        #if DEBUG
        private static string ProjectRootDir([CallerFilePath]string file = "") => Path.GetDirectoryName(file);
        private static string SaveFilePath => Path.Combine(ProjectRootDir(), "Bannerlord-Twitch.yaml");
        #else
        private static string SaveFilePath => Path.Combine(Common.PlatformFileHelper.DocumentsPath,
            "Mount and Blade II Bannerlord", "Configs", "Bannerlord-Twitch.yaml");
        #endif
        
        public static Settings Load()
        {
            if (!File.Exists(SaveFilePath))
            {
                string templateFileName = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "..", "..",
                    Path.GetFileName(SaveFilePath));
                File.Copy(templateFileName, SaveFilePath, overwrite: true);
                InformationManager.ShowInquiry(
                    new InquiryData(
                        "Bannerlord Twitch",
                        $"Default settings file created at {SaveFilePath}",
                        true, false, "Okay", null,
                    () => {}, () => {}), true);
            }
            var deserializer = new DeserializerBuilder().Build();
            var settings = deserializer.Deserialize<Settings>(File.ReadAllText(SaveFilePath));
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
