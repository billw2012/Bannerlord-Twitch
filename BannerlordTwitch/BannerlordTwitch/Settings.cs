using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Path = System.IO.Path;

// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable 649

namespace BannerlordTwitch
{
    public class RewardHandlerItemsSource : IItemsSource
    {
        public ItemCollection GetValues()
        {
            var items = new ItemCollection();
            foreach (string action in ActionManager.RewardHandlerNames)
                items.Add(action);
            return items;
        }
    }
    
    public class CommandHandlerItemsSource : IItemsSource
    {
        public ItemCollection GetValues()
        {
            var items = new ItemCollection();
            foreach (string cmd in ActionManager.CommandHandlerNames)
                items.Add(cmd);
            return items;
        }
    }

    [CategoryOrder("General", 0)]
    [CategoryOrder("Behavior", 2)]
    public abstract class ActionBase
    {
        [Category("General"), Description("Whether this is enabled or not"), PropertyOrder(-100)]
        public bool Enabled { get; set; }
        [Category("Behavior"), Description("Show response in the twitch chat")]
        public bool RespondInTwitch { get; set; }
        [Category("Behavior"), Description("Show response in the overlay window feed")]
        public bool RespondInOverlay { get; set; }
        
        [Category("Behavior"), Description("Name of the handler"), ReadOnly(true), PropertyOrder(1)]
        public abstract string Handler { get; set; }

        [Category("Behavior"), Description("Custom config for the handler"), ExpandableObject, ReadOnly(true), PropertyOrder(2)]
        public object HandlerConfig { get; set; }
    }
    
    [Description("Channel points reward definition")]
    public class Reward : ActionBase
    {
        [Category("General"), Description("Twitch channel points reward definition"), ExpandableObject, ReadOnly(true), PropertyOrder(1)]
        public RewardSpec RewardSpec { get; set; }

        public override string ToString() => $"{RewardSpec.Title} ({Handler})";
        
        [ItemsSource(typeof(RewardHandlerItemsSource))]
        public override string Handler { get; set; }
    }
    
    // Docs here https://dev.twitch.tv/docs/api/reference#create-custom-rewards
    [Description("The Twitch specific part of the channel points reward specification")]
    [UsedImplicitly]
    public class RewardSpec
    {
        [Description("The title of the reward"), PropertyOrder(1)]
        public string Title { get; set; }
        [Description("The prompt for the viewer when they are redeeming the reward, if IsUserInputRequired is true."), PropertyOrder(2)]
        public string Prompt { get; set; }
        [Description("The cost of the reward"), PropertyOrder(3)]
        public int Cost { get; set; }

        [Description("Is the reward currently enabled, if false the reward won’t show up to viewers."), PropertyOrder(4)]
        public bool IsEnabled { get; set; } = true;

        public string BackgroundColorText { get; set; }
        [Description("Custom background color for the reward"), PropertyOrder(5)]
        [YamlIgnore]
        public Color BackgroundColor
        {
            get => (Color) (ColorConverter.ConvertFromString(BackgroundColorText ?? $"#FF000000") ?? Colors.Black);
            set => BackgroundColorText = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
        }

        [Description("Does the user need to enter information when redeeming the reward. If this is true the Prompt will be shown."), PropertyOrder(6)]
        public bool IsUserInputRequired { get; set; }

        [Category("Limits"), Description("The maximum number per stream, defaults to unlimited"), DefaultValue(null), PropertyOrder(7)]
        public int? MaxPerStream { get; set; }

        [Category("Limits"), Description("The maximum number per user per stream, defaults to unlimited"), DefaultValue(null), PropertyOrder(8)]
        public int? MaxPerUserPerStream { get; set; }
        [Category("Limits"), Description("The cooldown in seconds, defaults to unlimited"), DefaultValue(null), PropertyOrder(9)]
        public int? GlobalCooldownSeconds { get; set; }

        private static string WebColor(System.Windows.Media.Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        public CreateCustomRewardsRequest GetTwitchSpec() =>
            new()
            {
                Title = Title,
                Cost = Cost,
                IsEnabled = IsEnabled,
                BackgroundColor = WebColor(BackgroundColor),
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

        public override string ToString() => Title;
    }
    
    [Description("Bot command definition")]
    public class Command : ActionBase
    {
        [Category("General"), Description("The command itself, not including the !"), PropertyOrder(1)]
        public string Name { get; set; }
        [Category("General"), Description("Hides the command from the !help action"), PropertyOrder(2)]
        public bool HideHelp { get; set; }
        [Category("General"), Description("What to show in the !help command"), PropertyOrder(3)]
        public string Help { get; set; }
        [Category("General"), Description("Only allows the broadcaster to use this command, and hides it from !help"), PropertyOrder(4)]
        public bool BroadcasterOnly { get; set; }
        [Category("General"), Description("Only allows the mods or broadcaster to use this command, and hides it from !help"), PropertyOrder(5)]
        public bool ModOnly { get; set; }
        
        [ItemsSource(typeof(CommandHandlerItemsSource))]
        public override string Handler { get; set; }
        
        public override string ToString() => $"{Name} ({Handler})";
    }
    
    [UsedImplicitly]
    public class GlobalConfig
    {
        public string Id { get; set; }
        public object Config { get; set; }
    }

    [UsedImplicitly]
    public class SimTestingItem
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public string Args { get; set; }
    }
    
    [UsedImplicitly]
    public class SimTestingConfig
    {
        public int UserCount { get; set; }
        public int UserStayTime { get; set; }
        public int IntervalMinMS { get; set; }
        public int IntervalMaxMS { get; set; }
        public List<SimTestingItem> Init { get; set; }
        public List<SimTestingItem> Use { get; set; }
    }

    [UsedImplicitly]
    public class AuthSettings
    {
        public string AccessToken { get; set; }
        public string ClientID { get; set; }
        public string BotAccessToken { get; set; }
        public string BotMessagePrefix { get; set; }
        public object Test { get; set; }
        
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

        public static void Save(AuthSettings authSettings)
        {
            File.WriteAllText(AuthFilePath, new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .Build()
                .Serialize(authSettings));
        }
    }
    
    public class Db
    {
#pragma warning disable 414
        public int Version = 1;
#pragma warning restore 414
        public List<string> RewardsCreated { get; set; } = new();
        
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
    
    public class Settings
    {
        public bool DeleteRewardsOnExit { get; set; }
        public List<Reward> Rewards { get; set; } = new ();
        [YamlIgnore]
        public IEnumerable<Reward> EnabledRewards => Rewards.Where(r => r.Enabled);
        public List<Command> Commands { get; set; } = new ();
        [YamlIgnore]
        public IEnumerable<Command> EnabledCommands => Commands.Where(r => r.Enabled);
        public List<GlobalConfig> GlobalConfigs { get; set; } = new ();
        public SimTestingConfig SimTesting { get; set; }

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
            var settings = new DeserializerBuilder().Build().Deserialize<Settings>(File.ReadAllText(SaveFilePath));
            if (settings == null)
                return null;
            
            // Fix up the settings to avoid NullReferences etc
            // settings.Rewards ??= new List<Reward>();
            // settings.Commands ??= new List<Command>();
            // settings.GlobalConfigs ??= new List<GlobalConfig>();

            foreach (var reward in settings.Rewards)
            {
                if (reward.RewardSpec == null)
                {
                    throw new FormatException($"A reward is missing a RewardSpec");
                }
                if (reward.Handler == null)
                {
                    throw new FormatException($"A reward is missing an Handler");
                }
            }
            
            return settings;
        }

        public static void Save(Settings settings)
        {
            File.WriteAllText(SaveFilePath, new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .Build()
                .Serialize(settings));
        }
    }
}
