using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Achievements;
using BLTAdoptAHero.Actions.Util;
using JetBrains.Annotations;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    [CategoryOrder("General", 1)]
    [CategoryOrder("XP", 2)]
    [CategoryOrder("Kill Rewards", 3)]
    [CategoryOrder("Battle End Rewards", 4)]
    [CategoryOrder("Shouts", 5)]
    [CategoryOrder("Kill Streaks", 6)]
    [CategoryOrder("Achievements", 7)]
    internal class GlobalCommonConfig : IUpdateFromDefault, IDocumentable
    {
        #region Static
        private const string ID = "Adopt A Hero - General Config";

        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalCommonConfig));
        internal static GlobalCommonConfig Get() => ActionManager.GetGlobalConfig<GlobalCommonConfig>(ID);
        internal static GlobalCommonConfig Get(Settings fromSettings) => fromSettings.GetGlobalConfig<GlobalCommonConfig>(ID);
        #endregion

        #region User Editable
        #region General
        [Category("General"), Description("Whether an adopted hero is allowed to die"), PropertyOrder(3), Document, UsedImplicitly]
        public bool AllowDeath { get; set; }
        
        [Category("General"), 
         Description("Chance (from 0 to 1) of killing blow not being reduced to a knock out blow (when Allow Death " +
                     "is enabled above). Remember the death chance for non companion heroes is 10% in vanilla, and " +
                     "this chance is applied as well as that. So if you set death chance to 0.5 (50%), then final " +
                     "death chance is 50% of 10%, which is 5%. Setting this to > 1 will NOT increase final death " +
                     "chance beyond 10%."), PropertyOrder(4), Document, UsedImplicitly]
        public float DeathChance { get; set; } = 0.2f;
        
        [Category("General"), 
         Description("Whether to apply the Death Chance changes to all heroes, not just adopted ones"), 
         PropertyOrder(5), Document, UsedImplicitly]
        public bool ApplyDeathChanceToAllHeroes { get; set; } = true;

        [Category("General"), Description("Whether the hero will always start with full health"), 
         PropertyOrder(6), Document, UsedImplicitly]
        public bool StartWithFullHealth { get; set; } = true;

        [Category("General"),
         Description("Amount to multiply normal starting health by, to give heroes better staying power vs others"),
         PropertyOrder(7), Document, UsedImplicitly]
        public float StartHealthMultiplier { get; set; } = 2;

        [Category("General"),
         Description("Amount to multiply normal retinue starting health by, to give retinue better staying power vs others"),
         PropertyOrder(8), Document, UsedImplicitly]
        public float StartRetinueHealthMultiplier { get; set; } = 2;

        [Category("General"),
         Description("Reduces morale loss when summoned heroes die"),
         PropertyOrder(9), Document, UsedImplicitly]
        public float MoraleLossFactor { get; set; } = 0.5f;

        [Category("General"),
         Description("Multiplier applied to all rewards for subscribers (less or equal to 1 means no boost)"),
         PropertyOrder(10), Document, UsedImplicitly]
        public float SubBoost { get; set; } = 1;

        [Category("XP"),
         Description("Use raw XP values instead of adjusting by focus and attributes, also ignoring skill cap. " +
                     "This avoids characters getting stuck when focus and attributes are not well distributed. "),
         PropertyOrder(1), Document, UsedImplicitly]
        public bool UseRawXP { get; set; } = true;
        
        [Category("XP"),
         Description("Skill cap when using Raw XP. Skills will not go above this value. " +
                     "330 is the vanilla XP skill cap."),
         PropertyOrder(2), Document, UsedImplicitly]
        public int RawXPSkillCap { get; set; } = 330;

        [Category("General"),
         Description("Whether an adopted heroes retinue should spawn in the same formation as the hero (otherwise " +
                     "they will go into default formations)"), PropertyOrder(13), Document, UsedImplicitly]
        public bool RetinueUseHeroesFormation { get; set; }

        [Category("General"), Description("Minimum time between summons for a specific hero"), 
         PropertyOrder(14), Document, UsedImplicitly]
        public int SummonCooldownInSeconds { get; set; } = 20;
        [Browsable(false), YamlIgnore]
        public bool CooldownEnabled => SummonCooldownInSeconds > 0;

        [Category("General"), 
         Description("How much to multiply the cooldown by each time summon is used. e.g. if Summon Cooldown is 20 " +
                     "seconds, and UseMultiplier is 1.1 (the default), then the first summon has a cooldown of 20 " +
                     "seconds, and the next 24 seconds, the 10th 52 seconds, and the 20th 135 seconds. " +
                     "See https://www.desmos.com/calculator/muej1o5eg5 for a visualization of this."), 
         PropertyOrder(15), Document, UsedImplicitly]
        public float SummonCooldownUseMultiplier { get; set; } = 1.1f;

        [Category("General"), 
         Description("Will disable companion limit. You will be able to have infinite number of companion"), 
         PropertyOrder(16), Document, UsedImplicitly]
        public bool BreakCompanionLimit { get; set; }
        #endregion

        #region Kill Rewards
        [Category("Kill Rewards"), Description("Gold the hero gets for every kill"), PropertyOrder(1), Document, UsedImplicitly]
        public int GoldPerKill { get; set; } = 5000;

        [Category("Kill Rewards"), Description("XP the hero gets for every kill"), PropertyOrder(2), Document, UsedImplicitly]
        public int XPPerKill { get; set; } = 5000;

        [Category("Kill Rewards"), Description("XP the hero gets for being killed"), PropertyOrder(3), Document, UsedImplicitly]
        public int XPPerKilled { get; set; } = 2000;

        [Category("Kill Rewards"), Description("HP the hero gets for every kill"), PropertyOrder(4), Document, UsedImplicitly]
        public int HealPerKill { get; set; } = 20;

        [Category("Kill Rewards"), Description("Gold the hero gets for every kill their retinue gets"),
         PropertyOrder(5), Document, UsedImplicitly]
        public int RetinueGoldPerKill { get; set; } = 2500;

        [Category("Kill Rewards"), Description("HP the hero's retinue gets for every kill"), 
         PropertyOrder(6), Document, UsedImplicitly]
        public int RetinueHealPerKill { get; set; } = 50;

        [Category("Kill Rewards"),
         Description("How much to scale the kill rewards by, based on relative level of the two characters. " +
                                               "If this is 0 (or not set) then the rewards are always as specified, if this is higher than 0 " +
                                               "then the rewards increase if the killed unit is higher level than the hero, and decrease if it " +
                                               "is lower. At a value of 0.5 (recommended) at level difference of 10 would give about 2.5 times " +
                                               "the normal rewards for gold, xp and health."),
         PropertyOrder(7), Document, UsedImplicitly]
        public float RelativeLevelScaling { get; set; } = 0.5f;

        [Category("Kill Rewards"),
         Description("Caps the maximum multiplier for the level difference, defaults to 5 if not specified"),
         PropertyOrder(8), Document, UsedImplicitly]
        public float LevelScalingCap { get; set; } = 5;
        #endregion

        #region Battle End Rewards
        [Category("Battle End Rewards"), Description("Gold won if the heroes side wins"), 
         PropertyOrder(1), Document, UsedImplicitly]
        public int WinGold { get; set; } = 10000;

        [Category("Battle End Rewards"), Description("XP the hero gets if the heroes side wins"), 
         PropertyOrder(2), Document, UsedImplicitly]
        public int WinXP { get; set; } = 10000;

        [Category("Battle End Rewards"), Description("Gold lost if the heroes side loses"), 
         PropertyOrder(3), Document, UsedImplicitly]
        public int LoseGold { get; set; } = 5000;

        [Category("Battle End Rewards"), Description("XP the hero gets if the heroes side loses"), 
         PropertyOrder(4), Document, UsedImplicitly]
        public int LoseXP { get; set; } = 5000;
        
        [Category("Battle End Rewards"), Description("Apply difficulty scaling to players side"), 
         PropertyOrder(5), Document, UsedImplicitly]
        public bool DifficultyScalingOnPlayersSide { get; set; } = true;
        
        [Category("Battle End Rewards"), Description("Apply difficulty scaling to enemy side"), 
         PropertyOrder(6), Document, UsedImplicitly]
        public bool DifficultyScalingOnEnemySide { get; set; } = true;
        
        [Category("Battle End Rewards"), 
         Description("End reward difficulty scaling: determines the extent to which higher difficulty battles " +
                     "increase the above rewards"), PropertyOrder(7), Document, UsedImplicitly]
        public float DifficultyScaling { get; set; } = 1;
        
        [Category("Battle End Rewards"), Description("Min difficulty scaling multiplier"), 
         PropertyOrder(8), Document, UsedImplicitly]
        public float DifficultyScalingMin { get; set; } = 0.2f;
        [YamlIgnore, Browsable(false)]
        public float DifficultyScalingMinClamped => MathF.Clamp(DifficultyScalingMin, 0, 1);

        [Category("Battle End Rewards"), Description("Max difficulty scaling multiplier"), 
         PropertyOrder(9), Document, UsedImplicitly]
        public float DifficultyScalingMax { get; set; } = 3f;
        [YamlIgnore, Browsable(false)]
        public float DifficultyScalingMaxClamped => Math.Max(DifficultyScalingMax, 1f);
        #endregion

        #region Shouts
        [Category("Shouts"), Description("Custom shouts"), PropertyOrder(1), UsedImplicitly]
        public List<Shout> Shouts { get; set; } = new();

        [Category("Shouts"), Description("Whether to include default shouts"), PropertyOrder(2), UsedImplicitly]
        public bool IncludeDefaultShouts { get; set; } = true;
        #endregion

        #region Kill Streak Rewards
        [Category("Kill Streak Rewards"), Description("Kill Streaks"), PropertyOrder(1), UsedImplicitly]
        public List<KillStreakRewards> KillStreaks { get; set; } = new();

        [Category("Kill Streak Rewards"), 
         Description("Whether to use the popup banner to announce kill streaks. Will only print in the overlay " +
                     "instead if disabled."), PropertyOrder(2), UsedImplicitly]
        public bool ShowKillStreakPopup { get; set; } = true;

        [Category("Kill Streak Rewards"), Description("Sound to play when killstreak popup is disabled."),
         PropertyOrder(3), UsedImplicitly]
        public Log.Sound KillStreakPopupAlertSound { get; [UsedImplicitly] set; } = Log.Sound.Horns2;
        
        [Category("Kill Streak Rewards"), 
         Description("The level at which the rewards normalize and start to reduce " +
                     "(if relative level scaling is enabled)."), PropertyOrder(4), UsedImplicitly]
        public int ReferenceLevelReward { get; set; } = 15;
        #endregion

        #region Achievements
        [Category("Achievements"), Description("Achievements"), PropertyOrder(1), UsedImplicitly]
        public List<AchievementDef> Achievements { get; set; } = new();

        public AchievementDef GetAchievement(Guid id) => Achievements?.FirstOrDefault(a => a.ID == id) ?? null;
        #endregion
        #endregion

        #region Public Interface
        [YamlIgnore, Browsable(false)]
        public float DifficultyScalingClamped => MathF.Clamp(DifficultyScaling, 0, 5);

        public float GetCooldownTime(int summoned) 
           => (float) (Math.Pow(SummonCooldownUseMultiplier, Mathf.Max(1, summoned)) * SummonCooldownInSeconds);
        #endregion

        #region IUpdateFromDefault
        public void OnUpdateFromDefault(Settings defaultSettings)
        {
            SettingsHelpers.MergeCollectionsSorted(
                KillStreaks, 
                Get(defaultSettings).KillStreaks,
                (a, b) => a.Name == b.Name,
                (a, b) => a.KillsRequired.CompareTo(b.KillsRequired)
            );
            SettingsHelpers.MergeCollections(
                Achievements, 
                Get(defaultSettings).Achievements,
                (a, b) => a.ID == b.ID
            );
        }
        #endregion
        
        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.Div("common-config", () =>
            {
                generator.H1("Global Common Config");
                DocumentationHelpers.AutoDocument(generator, this);

                var killStreaks = KillStreaks.Where(k => k.Enabled).ToList();
                if (killStreaks.Any())
                {
                    generator.H2("Kill Streaks");
                    generator.Table("kill-streaks", () =>
                    {
                        generator.TR(() => generator.TH("Name").TH("Kills Required").TH("Reward"));
                        foreach (var k in killStreaks
                            .OrderBy(k => k.KillsRequired))
                        {
                            generator.TR(() => 
                                generator.TD(k.Name).TD($"{k.KillsRequired}").TD(() =>
                                {
                                    if (k.GoldReward > 0) generator.P($"{k.GoldReward}{Naming.Gold}");
                                    if (k.XPReward > 0) generator.P($"{k.XPReward} XP");
                                }));
                        }
                    });
                }
                
                var achievements = Achievements.Where(a => a.Enabled).ToList();
                if (achievements.Any())
                {
                    generator.H2("Achievements");
                    generator.Table("achievements", () =>
                    {
                        generator.TR(() => generator.TH("Name").TH("Requirement").TH("Reward"));
                        // TODO: fix
                        // foreach (var a in achievements
                        //     .OrderBy(a => a.Value)
                        //     .GroupBy(a => a.Type)
                        //     .SelectMany(a => a))
                        // {
                        //     generator.TR(() =>
                        //         generator.TD(a.Name)
                        //             .TD($"{a.Value} {a.Type.ToString().SplitCamelCase()}")
                        //             .TD(() =>
                        //             {
                        //                 if (a.GoldGain > 0) generator.P($"{a.GoldGain}{Naming.Gold}");
                        //                 if (a.XPGain > 0) generator.P($"{a.XPGain} XP");
                        //             })
                        //         );
                        // }
                    });
                }
            });
        }
        #endregion
    }
}