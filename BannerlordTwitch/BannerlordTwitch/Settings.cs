using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
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
    public abstract class ActionBase : INotifyPropertyChanged
    {
        // Unique ID for this action 
        [ReadOnly(true), UsedImplicitly]
        public Guid ID { get; set; } = Guid.NewGuid();

        [Category("General"), Description("Whether this is enabled or not"), PropertyOrder(-100), UsedImplicitly]
        public bool Enabled { get; set; }
        [Category("General"), Description("Show response in the twitch chat"), PropertyOrder(-99), UsedImplicitly]
        public bool RespondInTwitch { get; set; }
        [Category("General"), Description("Show response in the overlay window feed"), PropertyOrder(-98), UsedImplicitly]
        public bool RespondInOverlay { get; set; }
        
        [Category("General"), Description("Name of the handler"), ReadOnly(true), PropertyOrder(1), UsedImplicitly]
        public abstract string Handler { get; set; }

        [Category("General"), Description("Custom config for the handler"), ExpandableObject, ReadOnly(true), PropertyOrder(2), UsedImplicitly]
        public object HandlerConfig { get; set; }
        
        [Category("General"), Description("What to show in the generated documentation"), PropertyOrder(3), UsedImplicitly]
        public string Documentation { get; set; }

        [YamlIgnore, Browsable(false), UsedImplicitly]
        public virtual bool IsValid => !Enabled || !string.IsNullOrEmpty(Handler);

        public event PropertyChangedEventHandler PropertyChanged;
    }
    
    [Description("Channel points reward definition")]
    public class Reward : ActionBase
    {
        [Category("General"), Description("Twitch channel points reward definition"), ExpandableObject, ReadOnly(true), PropertyOrder(1)]
        public RewardSpec RewardSpec { get; set; }

        public override string ToString() => $"{RewardSpec?.Title ?? "unnamed reward"} ({Handler})";
        
        [ItemsSource(typeof(RewardHandlerItemsSource))]
        public override string Handler { get; set; }
    }
    
    // Docs here https://dev.twitch.tv/docs/api/reference#create-custom-rewards
    [Description("The Twitch specific part of the channel points reward specification")]
    [UsedImplicitly]
    public class RewardSpec
    {
        [Description("The title of the reward"), PropertyOrder(1), UsedImplicitly]
        public string Title { get; set; }
        [Description("Whether the reward will automatically be set to fulfilled once completed in game. If you set this to true then the redemptions that successfully complete in game will stay in your redemption queue. This is useful if you are worried about people losing points if the game crashes, or you reload an older save."), PropertyOrder(1), UsedImplicitly]
        public bool DisableAutomaticFulfillment { get; set; }
        [Description("Description / prompt"), PropertyOrder(2), UsedImplicitly]
        public string Prompt { get; set; }
        [Description("The cost of the reward"), PropertyOrder(3), UsedImplicitly]
        public int Cost { get; set; } = 100;

        [Description("Is the reward currently enabled, if false the reward won’t show up to viewers."), PropertyOrder(4), UsedImplicitly]
        public bool IsEnabled { get; set; } = true;

        [Browsable(false)]
        public string BackgroundColorText { get; set; }
        [Description("Custom background color for the reward"), PropertyOrder(5), UsedImplicitly]
        [YamlIgnore]
        public Color BackgroundColor
        {
            get => (Color) (ColorConverter.ConvertFromString(BackgroundColorText ?? $"#FF000000") ?? Colors.Black);
            set => BackgroundColorText = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
        }

        [Description("Does the user need to enter information when redeeming the reward. If this is true the Prompt will be shown."), PropertyOrder(6), UsedImplicitly]
        public bool IsUserInputRequired { get; set; }

        [Category("Limits"), Description("The maximum number per stream, defaults to unlimited"), DefaultValue(null), PropertyOrder(7), UsedImplicitly]
        public int? MaxPerStream { get; set; }

        [Category("Limits"), Description("The maximum number per user per stream, defaults to unlimited"), DefaultValue(null), PropertyOrder(8), UsedImplicitly]
        public int? MaxPerUserPerStream { get; set; }
        [Category("Limits"), Description("The cooldown in seconds, defaults to unlimited"), DefaultValue(null), PropertyOrder(9), UsedImplicitly]
        public int? GlobalCooldownSeconds { get; set; }

        private static string WebColor(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
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
        [Category("General"), Description("The command itself, not including the !"), PropertyOrder(1), UsedImplicitly]
        public string Name { get; set; }
        [Category("General"), Description("Hides the command from the !help action"), PropertyOrder(2), UsedImplicitly]
        public bool HideHelp { get; set; }
        [Category("General"), Description("What to show in the !help command"), PropertyOrder(3), UsedImplicitly]
        public string Help { get; set; }
        
        [ItemsSource(typeof(CommandHandlerItemsSource))]
        public override string Handler { get; set; }
        
        public override string ToString() => $"{Name} ({Handler})";
    }
    
    [UsedImplicitly]
    public class GlobalConfig
    {
        [Browsable(false)]
        public string Id { get; set; }
        [ExpandableObject, ReadOnly(true)]
        public object Config { get; set; }
        
        public override string ToString() => Id;
    }

    public enum SimActionType
    {
        Reward,
        Command
    }

    [UsedImplicitly]
    public class SimTestingItem : INotifyPropertyChanged
    {
        [PropertyOrder(0), UsedImplicitly]
        public bool Enabled { get; set; } = true;
        [PropertyOrder(1), UsedImplicitly]
        public SimActionType Type { get; set; }
        [PropertyOrder(2), UsedImplicitly]
        public string Id { get; set; }
        [PropertyOrder(3), UsedImplicitly]
        public string Args { get; set; }
        [PropertyOrder(4), UsedImplicitly]
        public float Weight { get; set; } = 1f;
        
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Args))
            {
                return $"{Id} ({Type}), {nameof(Args)}: {Args}, {nameof(Weight)}: {Weight}";
            }
            else
            {
                return $"{Id} ({Type}), {nameof(Weight)}: {Weight}";
            }
        }
        //public override string ToString() => $"{Type} {Id} {Args} {Weight}";

        public event PropertyChangedEventHandler PropertyChanged;
    }
    
    [UsedImplicitly]
    public class SimTestingConfig
    {
        [PropertyOrder(1), UsedImplicitly]
        public int UserCount { get; set; }
        [PropertyOrder(2), UsedImplicitly]
        public int UserStayTime { get; set; }
        [PropertyOrder(3), UsedImplicitly]
        public int IntervalMinMS { get; set; }
        [PropertyOrder(4), UsedImplicitly]
        public int IntervalMaxMS { get; set; }
        [PropertyOrder(5), UsedImplicitly]
        public List<SimTestingItem> Init { get; set; }

        [YamlIgnore, Browsable(false)]
        public IEnumerable<SimTestingItem> InitEnabled => Init?.Where(i => i.Enabled) ?? Enumerable.Empty<SimTestingItem>();
        
        [PropertyOrder(6), UsedImplicitly]
        public List<SimTestingItem> Use { get; set; }
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<SimTestingItem> UseEnabled => Use?.Where(i => i.Enabled) ?? Enumerable.Empty<SimTestingItem>();

        public override string ToString() => "Sim Testing Config";
    }

    [UsedImplicitly]
    public class AuthSettings
    {
        public string AccessToken { get; set; }
        public string ClientID { get; set; }
        public string BotAccessToken { get; set; }
        public string BotMessagePrefix { get; set; }
        public bool DebugSpoofAffiliate { get; set; }
        
        public string NeocitiesUsername { get; set; }
        
        public string NeocitiesPassword { get; set; }

        public string DocsTitle { get; set; } = "Bannerlord Twitch Viewer Guide";
        
        public string DocsIntroduction { get; set; } 
            = "Bannerlord Twitch (BLT) is a Twitch Integration mod for Mount & Blade II: Bannerlord.<br>" +
              "As a viewer you can use channel point rewards (if available), and chat commands to interact with the " +
              "game while the streamer is playing.<br>The primary feature of BLT is allowing you to 'adopt' a hero " +
              "in the game, i.e. a character in the game will be assigned to you, given your Twitch name, and be " +
              "available for you to interact with further.<br>" +
              "Some examples of things you can do with your hero include 'summoning' your hero into battles that the " +
              "streamer is taking part in, joining viewer tournaments, upgrading equipment, selecting your heroes " +
              "'class'.<br>" +
              "If you aren't sure where to start, begin by using the channel point redemption 'Adopt a Hero'.";
        
        private static PlatformFilePath AuthFilePath => FileSystem.GetConfigPath("Bannerlord-Twitch-Auth.yaml");

        public static AuthSettings Load()
        {
            return !FileSystem.FileExists(AuthFilePath) 
                ? null 
                : new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .Build()
                    .Deserialize<AuthSettings>(FileSystem.GetFileContentString(AuthFilePath));
        }

        public static void Save(AuthSettings authSettings)
        {
            FileSystem.SaveFileString(AuthFilePath, YamlHelpers.Serialize(authSettings));
        }
    }

    public class Settings : IDocumentable
    {
        public List<Reward> Rewards { get; set; } = new ();
        [YamlIgnore]
        public IEnumerable<Reward> EnabledRewards => Rewards.Where(r => r.Enabled);
        public List<Command> Commands { get; set; } = new ();
        [YamlIgnore]
        public IEnumerable<Command> EnabledCommands => Commands.Where(r => r.Enabled);
        public List<GlobalConfig> GlobalConfigs { get; set; } = new ();
        public SimTestingConfig SimTesting { get; set; }
        [YamlIgnore, Browsable(false)]
        public IEnumerable<ActionBase> AllActions => Rewards.Cast<ActionBase>().Concat(Commands);

        public bool DisableAutomaticFulfillment { get; set; }
        
        public Command GetCommand(string id) => EnabledCommands.FirstOrDefault(c =>
            string.Equals(c.Name, id, StringComparison.CurrentCultureIgnoreCase));

        [YamlIgnore]
        public IEnumerable<IConfig> ConfigInterfaces => AllActions
            .Select(c => c.HandlerConfig)
            .Concat(GlobalConfigs.Select(g => g.Config))
            .OfType<IConfig>();

        public T GetGlobalConfig<T>(string id) => (T)GlobalConfigs.First(c => c.Id == id).Config;

        #if DEBUG
        private static string ProjectRootDir([CallerFilePath]string file = "") => Path.GetDirectoryName(file);
        private static string SaveFilePath => Path.Combine(ProjectRootDir(), "Bannerlord-Twitch-v2.yaml");
        public static Settings Load()
        {
            var settings = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build()
                .Deserialize<Settings>(File.ReadAllText(SaveFilePath));
            if (settings == null)
                throw new Exception($"Couldn't load the mod settings from {SaveFilePath}");

            settings.Commands ??= new();
            settings.Rewards ??= new();
            settings.GlobalConfigs ??= new();
            settings.SimTesting ??= new();

            SettingsPostLoad(settings);

            return settings;
        }

        public static void Save(Settings settings)
        {
            SettingsPreSave(settings);
            File.WriteAllText(SaveFilePath, YamlHelpers.Serialize(settings));
        }

        #else
        private static PlatformFilePath SaveFilePath => FileSystem.GetConfigPath("Bannerlord-Twitch-v2.yaml");
        private static string TemplateFileName => Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "..", "..", "Bannerlord-Twitch-v2.yaml");

        public static Settings Load()
        {
            var templateSettings = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build()
                .Deserialize<Settings>(File.ReadAllText(TemplateFileName));
            if (templateSettings == null)
            {
                throw new Exception($"Couldn't load the mod template settings from {TemplateFileName}");
            }

            var settings = FileSystem.FileExists(SaveFilePath)
                ? new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .Build()
                    .Deserialize<Settings>(FileSystem.GetFileContentString(SaveFilePath))
                : templateSettings
                ;

            if (settings == null)
                throw new Exception($"Couldn't load the mod settings from {SaveFilePath}");

            // merge missing actions / rewards from template
            settings.Commands.AddRange(templateSettings.Commands.Where(s => settings.Commands.All(s2 => s2.ID != s.ID && s2.ToString() != s.ToString())));
            settings.Commands.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));
            settings.Rewards.AddRange(templateSettings.Rewards.Where(s => settings.Rewards.All(s2 => s2.ID != s.ID && s2.ToString() != s.ToString())));
            settings.Rewards.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));
            settings.GlobalConfigs.AddRange(templateSettings.GlobalConfigs.Where(s => settings.GlobalConfigs.All(s2 => s2.Id != s.Id)));
            settings.GlobalConfigs.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));

            SettingsPostLoad(settings);
            
            return settings;
        }

        public static void Save(Settings settings)
        {
            SettingsPreSave(settings);
            FileSystem.SaveFileString(SaveFilePath, YamlHelpers.Serialize(settings));
        }
        #endif

        private static void SettingsPostLoad(Settings settings)
        {
            ActionManager.ConvertSettings(settings.Commands);
            ActionManager.ConvertSettings(settings.Rewards);
            ActionManager.EnsureGlobalSettings(settings.GlobalConfigs);

            foreach (var config in settings.ConfigInterfaces)
            {
                config.OnLoaded(settings);
            }

            foreach (var action in settings.AllActions)
            {
                if (!action.IsValid)
                {
                    throw new FormatException($"Action {action} is not valid");
                }
            }
        }

        private static void SettingsPreSave(Settings settings)
        {
            foreach (var config in settings.ConfigInterfaces)
            {
                config.OnSaving();
            }
        }

        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.Div("commands", () =>
            {
                generator.H1("Commands");
                generator.Table(() => {
                    generator.TR(() => generator.TH("Command").TH("Description").TH("Settings"));
                    foreach (var d in Commands.Where(c => c.Enabled))
                    {
                        generator.TR(() =>
                        {
                            generator.TD(d.Name);
                            generator.TD(string.IsNullOrEmpty(d.Documentation) ? d.Help : d.Documentation);
                            generator.TD(() =>
                            {
                                if (d.HandlerConfig is IDocumentable doc)
                                {
                                    doc.GenerateDocumentation(generator);
                                }
                                else if (d.HandlerConfig != null)
                                {
                                    DocumentationHelpers.AutoDocument(generator, d.HandlerConfig);
                                }
                            });
                        });
                    }
                });
            });
            generator.Br();
            generator.Div("rewards", () =>
            {
                generator.H1("Channel Point Rewards");
                generator.Table(() => {
                    generator.TR(() => generator.TH("Command").TH("Description").TH("Settings"));
                    foreach (var r in Rewards.Where(r => r.Enabled))
                    {
                        generator.TR(() =>
                        {
                            generator.TD(r.RewardSpec.Title);
                            generator.TD(string.IsNullOrEmpty(r.Documentation) ? r.RewardSpec.Prompt : r.Documentation);
                            generator.TD(() =>
                            {
                                if (r.HandlerConfig is IDocumentable doc)
                                {
                                    doc.GenerateDocumentation(generator);
                                }
                                else if (r.HandlerConfig != null)
                                {
                                    DocumentationHelpers.AutoDocument(generator, r.HandlerConfig);
                                }
                            });
                        });
                    }
                });
            });
            generator.Br();
            generator.Div("global-configs", () =>
            {
                foreach (var g in GlobalConfigs.Select(c => c.Config).OfType<IDocumentable>())
                {
                    g.GenerateDocumentation(generator);
                }
            });
        }
    }
}
