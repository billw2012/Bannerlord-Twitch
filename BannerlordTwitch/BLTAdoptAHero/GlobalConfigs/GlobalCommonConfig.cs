using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Annotations;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Achievements;
using BLTAdoptAHero.Actions.Util;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    [CategoryOrder("General", 1), 
     CategoryOrder("Battle", 2), 
     CategoryOrder("Death", 3), 
     CategoryOrder("XP", 4),
     CategoryOrder("Kill Rewards", 5),
     CategoryOrder("Battle End Rewards", 6),
     CategoryOrder("Kill Streak Rewards", 7),
     CategoryOrder("Achievements", 8), 
     CategoryOrder("Shouts", 9),
     LocDisplayName("{=vDjnDtoL}Common Config")]
    internal class GlobalCommonConfig : IUpdateFromDefault, IDocumentable, INotifyPropertyChanged
    {
        #region Static
        private const string ID = "Adopt A Hero - General Config";

        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalCommonConfig));
        internal static GlobalCommonConfig Get() => ActionManager.GetGlobalConfig<GlobalCommonConfig>(ID);
        internal static GlobalCommonConfig Get(Settings fromSettings) => fromSettings.GetGlobalConfig<GlobalCommonConfig>(ID);
        #endregion

        #region User Editable
        #region General
        [LocDisplayName("{=xwcKN7sH}Sub Boost"),
         LocCategory("General", "{=C5T5nnix}General"),
         LocDescription("{=rX68wbfF}Multiplier applied to all rewards for subscribers (less or equal to 1 means no boost). NOTE: This is only partially implemented, it works for bot commands only currently."),
         PropertyOrder(1), Document, UsedImplicitly,
         Range(0.5, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor))]
        public float SubBoost { get; set; } = 1;

        [LocDisplayName("{=O0LU5WBa}Custom Reward Modifiers"),
         LocCategory("General", "{=C5T5nnix}General"), 
         LocDescription("{=tp3YdGmo}The specification for custom item rewards, applies to tournament prize and achievement rewards"), 
         PropertyOrder(3), ExpandableObject, UsedImplicitly]
        public RandomItemModifierDef CustomRewardModifiers { get; set; } = new();
        #endregion

        #region Battle
        [LocDisplayName("{=X8r0C5fx}Start With Full Health"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"), 
         LocDescription("{=HbNVrZuv}Whether the hero will always start with full health"), 
         PropertyOrder(1), Document, UsedImplicitly]
        public bool StartWithFullHealth { get; set; } = true;

        [LocDisplayName("{=fxZIKL65}Start Health Multiplier"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"),
         LocDescription("{=8yNIRS9S}Amount to multiply normal starting health by, to give heroes better staying power vs others"),
         PropertyOrder(2),
         Range(0.1, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float StartHealthMultiplier { get; set; } = 2;

        [LocDisplayName("{=HvcTekVk}Start Retinue Health Multiplier"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"),
         LocDescription("{=G6JJT2ot}Amount to multiply normal retinue starting health by, to give retinue better staying power vs others"),
         PropertyOrder(3),
         Range(0.1, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float StartRetinueHealthMultiplier { get; set; } = 2;

        [LocDisplayName("{=ZPmBe7XI}Morale Loss Factor"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"),
         LocDescription("{=tpgJtS5q}Reduces morale loss when summoned heroes die"),
         PropertyOrder(4),
         Range(0, 2), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float MoraleLossFactor { get; set; } = 0.5f;
        
        [LocDisplayName("{=bXdC2trk}Retinue Use Heroes Formation"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"),
         LocDescription("{=D8uDzXlV}Whether an adopted heroes retinue should spawn in the same formation as the hero (otherwise they will go into default formations)"),
         PropertyOrder(13), Document, UsedImplicitly]
        public bool RetinueUseHeroesFormation { get; set; }

        [LocDisplayName("{=OlJrCEyE}Summon Cooldown In Seconds"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"), 
         LocDescription("{=DeGB2BGZ}Minimum time between summons for a specific hero"), 
         PropertyOrder(5),
         Range(0, int.MaxValue),
         Document, UsedImplicitly]
        public int SummonCooldownInSeconds { get; set; } = 20;

        [Browsable(false), YamlIgnore]
        public bool CooldownEnabled => SummonCooldownInSeconds > 0;

        [LocDisplayName("{=f9HVD2cC}Summon Cooldown Use Multiplier"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"), 
         LocDescription("{=4gZlfHzM}How much to multiply the cooldown by each time summon is used. e.g. if Summon Cooldown is 20 seconds, and UseMultiplier is 1.1 (the default), then the first summon has a cooldown of 20 seconds, and the next 24 seconds, the 10th 52 seconds, and the 20th 135 seconds. See https://www.desmos.com/calculator/muej1o5eg5 for a visualization of this."), 
         PropertyOrder(6),
         Range(1, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float SummonCooldownUseMultiplier { get; set; } = 1.1f;

        [LocDisplayName("{=ViLoy0k3}Summon Cooldown Example"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"), 
         LocDescription("{=xZoSFrAb}Shows the consecutive cooldowns (in seconds) for 10 summons"),
         PropertyOrder(7), YamlIgnore, ReadOnly(true), UsedImplicitly] 
        public string SummonCooldownExample => string.Join(", ", 
            Enumerable.Range(1, 10)
                .Select(i => $"{i}: {GetCooldownTime(i):0}s"));
        #endregion

        #region Death
        [LocDisplayName("{=4sNJRQyw}Allow Death"),
         LocCategory("Death", "{=dbU7WEKG}Death"), 
         LocDescription("{=VbBUYOfc}Whether an adopted hero is allowed to die"), 
         PropertyOrder(1), Document, UsedImplicitly]
        public bool AllowDeath { get; set; }
        
        [Browsable(false), UsedImplicitly]
        public float DeathChance { get; set; } = 0.1f;
        
        [LocDisplayName("{=ZEfAPyOm}Final Death Chance Percent"),
         LocCategory("Death", "{=dbU7WEKG}Death"),
         LocDescription("{=xlt1pNuT}Final death chance percent (includes vanilla chance)"),
         PropertyOrder(2),
         Range(0, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         YamlIgnore, Document, UsedImplicitly] 
        public float FinalDeathChancePercent
        { 
            get => DeathChance * 10f;
            set => DeathChance = value * 0.1f;
        }
        
        [LocDisplayName("{=sbc5Fp4o}Apply Death Chance To All Heroes"),
         LocCategory("Death", "{=dbU7WEKG}Death"), 
         LocDescription("{=nbR7NLNz}Whether to apply the Death Chance changes to all heroes, not just adopted ones"), 
         PropertyOrder(5), Document, UsedImplicitly]
        public bool ApplyDeathChanceToAllHeroes { get; set; } = true;
        
        [LocDisplayName("{=}Retinue Death Chance Percent"),
         LocCategory("Death", "{=dbU7WEKG}Death"),
         LocDescription("{=}Retinue death chance percent (this determines the chance that a killing blow will " +
                        "actually kill the retinue, removing them from the adopted hero's retinue list)"),
         PropertyOrder(6),
         Range(0, 100), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         YamlIgnore, Document, UsedImplicitly] 
        public float RetinueDeathChancePercent
        { 
            get => RetinueDeathChance * 100f;
            set => RetinueDeathChance = value * 0.01f;
        }
                
        [Browsable(false), UsedImplicitly]
        public float RetinueDeathChance { get; set; } = 0.025f;
        #endregion

        #region XP
        [LocDisplayName("{=lwU4dELT}Use Raw XP"),
         LocCategory("XP", "{=06KnYhyh}XP"),
         LocDescription("{=dICRr4BH}Use raw XP values instead of adjusting by focus and attributes, also ignoring skill cap. This avoids characters getting stuck when focus and attributes are not well distributed. "),
         PropertyOrder(1), Document, UsedImplicitly]
        public bool UseRawXP { get; set; } = true;
        
        [LocDisplayName("{=S5FAna09}Raw XP Skill Cap"),
         LocCategory("XP", "{=06KnYhyh}XP"),
         LocDescription("{=WUzqXuHN}Skill cap when using Raw XP. Skills will not go above this value. 330 is the vanilla XP skill cap."),
         PropertyOrder(2), Range(0, 1023), Document, UsedImplicitly]
        public int RawXPSkillCap { get; set; } = 330;
        #endregion

        #region Kill Rewards
        [LocDisplayName("{=94Ouh5It}Gold Per Kill"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"), 
         LocDescription("{=iSAMxZ8a}Gold the hero gets for every kill"), 
         PropertyOrder(1), Document, UsedImplicitly]
        public int GoldPerKill { get; set; } = 5000;

        [LocDisplayName("{=DMGKBoJT}XP Per Kill"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=kwW5pZT9}XP the hero gets for every kill"), 
         PropertyOrder(2), Document, UsedImplicitly]
        public int XPPerKill { get; set; } = 5000;

        [LocDisplayName("{=a1zjEuUe}XP Per Killed"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=bW8t2g5N}XP the hero gets for being killed"), 
         PropertyOrder(3), Document, UsedImplicitly]
        public int XPPerKilled { get; set; } = 2000;

        [LocDisplayName("{=cRV9HDdf}Heal Per Kill"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=7VWAZgfK}HP the hero gets for every kill"), 
         PropertyOrder(4), Document, UsedImplicitly]
        public int HealPerKill { get; set; } = 20;

        [LocDisplayName("{=lIhhHjih}Retinue Gold Per Kill"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=h93j0qw3}Gold the hero gets for every kill their retinue gets"),
         PropertyOrder(5), Document, UsedImplicitly]
        public int RetinueGoldPerKill { get; set; } = 2500;

        [LocDisplayName("{=KwlWrzDS}Retinue Heal Per Kill"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=Q3UVoHmt}HP the hero's retinue gets for every kill"), 
         PropertyOrder(6), Document, UsedImplicitly]
        public int RetinueHealPerKill { get; set; } = 50;

        [LocDisplayName("{=wSzUkbNR}Relative Level Scaling"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=1LTDJZ7Y}How much to scale the kill rewards by, based on relative level of the two characters. If this is 0 (or not set) then the rewards are always as specified, if this is higher than 0 then the rewards increase if the killed unit is higher level than the hero, and decrease if it is lower. At a value of 0.5 (recommended) at level difference of 10 would give about 2.5 times the normal rewards for gold, xp and health."),
         PropertyOrder(7), 
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         Document, UsedImplicitly]
        public float RelativeLevelScaling { get; set; } = 0.5f;

        [LocDisplayName("{=BDk1G4nc}Level Scaling Cap"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=Vod0pJEN}Caps the maximum multiplier for the level difference, defaults to 5 if not specified"),
         PropertyOrder(8), 
         Range(0, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float LevelScalingCap { get; set; } = 5;
        #endregion

        #region Battle End Rewards
        [LocDisplayName("{=IQTT5vYE}Win Gold"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"), 
         LocDescription("{=pc3G0W39}Gold won if the heroes side wins"), 
         PropertyOrder(1), Document, UsedImplicitly]
        public int WinGold { get; set; } = 10000;

        [LocDisplayName("{=h8I3PWkV}Win XP"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"), 
         LocDescription("{=F7Tw4D07}XP the hero gets if the heroes side wins"), 
         PropertyOrder(2), Document, UsedImplicitly]
        public int WinXP { get; set; } = 10000;

        [LocDisplayName("{=lfCWK7aA}Lose Gold"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"), 
         LocDescription("{=E209XRml}Gold lost if the heroes side loses"), 
         PropertyOrder(3), Document, UsedImplicitly]
        public int LoseGold { get; set; } = 5000;

        [LocDisplayName("{=Vobr36Bl}Lose XP"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"), 
         LocDescription("{=itAfYdmO}XP the hero gets if the heroes side loses"), 
         PropertyOrder(4), Document, UsedImplicitly]
        public int LoseXP { get; set; } = 5000;
        
        [LocDisplayName("{=ihB1KMOY}Difficulty Scaling On Players Side"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"), 
         LocDescription("{=Bt1PS0aC}Apply difficulty scaling to players side"), 
         PropertyOrder(5), Document, UsedImplicitly]
        public bool DifficultyScalingOnPlayersSide { get; set; } = true;
        
        [LocDisplayName("{=nym7EtAd}Difficulty Scaling On Enemy Side"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"), 
         LocDescription("{=U0hZef9L}Apply difficulty scaling to enemy side"), 
         PropertyOrder(6), Document, UsedImplicitly]
        public bool DifficultyScalingOnEnemySide { get; set; } = true;
        
        [LocDisplayName("{=CaVuq5tE}Difficulty Scaling"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"), 
         LocDescription("{=IhhfIQ74}End reward difficulty scaling: determines the extent to which higher difficulty battles increase the above rewards (0 to 1)"), 
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(7), Document, UsedImplicitly]
        public float DifficultyScaling { get; set; } = 1;
        
        [LocDisplayName("{=891WqOrJ}Difficulty Scaling Min"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"), 
         LocDescription("{=FPXz7lBi}Min difficulty scaling multiplier"), 
         PropertyOrder(8),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float DifficultyScalingMin { get; set; } = 0.2f;
        [YamlIgnore, Browsable(false)]
        public float DifficultyScalingMinClamped => MathF.Clamp(DifficultyScalingMin, 0, 1);

        [LocDisplayName("{=Wsho5Yns}Difficulty Scaling Max"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"), 
         LocDescription("{=ZW7O1JTv}Max difficulty scaling multiplier"), 
         PropertyOrder(9),
         Range(1, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float DifficultyScalingMax { get; set; } = 3f;
        [YamlIgnore, Browsable(false)]
        public float DifficultyScalingMaxClamped => Math.Max(DifficultyScalingMax, 1f);
        #endregion

        #region Kill Streak Rewards
        [LocDisplayName("{=3DZYc6hN}Kill Streaks"),
         LocCategory("Kill Streak Rewards", "{=lnz7d1BI}Kill Streak Rewards"), 
         LocDescription("{=3DZYc6hN}Kill Streaks"),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(1), UsedImplicitly]
        public ObservableCollection<KillStreakDef> KillStreaks { get; set; } = new();

        [LocDisplayName("{=wQ7lXgLA}Show Kill Streak Popup"),
         LocCategory("Kill Streak Rewards", "{=lnz7d1BI}Kill Streak Rewards"), 
         LocDescription("{=wDW3143d}Whether to use the popup banner to announce kill streaks. Will only print in the overlay instead if disabled."), 
         PropertyOrder(2), UsedImplicitly]
        public bool ShowKillStreakPopup { get; set; } = true;

        [LocDisplayName("{=rhwujKvf}Kill Streak Popup Alert Sound"),
         LocCategory("Kill Streak Rewards", "{=lnz7d1BI}Kill Streak Rewards"), 
         LocDescription("{=1GVV1fjY}Sound to play when killstreak popup is disabled."),
         PropertyOrder(3), UsedImplicitly]
        public Log.Sound KillStreakPopupAlertSound { get; set; } = Log.Sound.Horns2;
        
        [LocDisplayName("{=dP9AoB9o}Reference Level Reward"),
         LocCategory("Kill Streak Rewards", "{=lnz7d1BI}Kill Streak Rewards"), 
         LocDescription("{=y7AZjeSK}The level at which the rewards normalize and start to reduce (if relative level scaling is enabled)."), 
         PropertyOrder(4), UsedImplicitly]
        public int ReferenceLevelReward { get; set; } = 15;
        #endregion

        #region Achievements
        [LocDisplayName("{=zTLei6dQ}Achievements"),
         LocCategory("Achievements", "{=EPr2clqT}Achievements"), 
         LocDescription("{=zTLei6dQ}Achievements"),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(1), UsedImplicitly]
        public ObservableCollection<AchievementDef> Achievements { get; set; } = new();
        #endregion

        #region Shouts
        [LocDisplayName("{=HkD6326j}Shouts"),
         LocCategory("Shouts", "{=UhUpH8C8}Shouts"), 
         LocDescription("{=ufqtH5QV}Custom shouts"),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(1), UsedImplicitly]
        public ObservableCollection<Shout> Shouts { get; set; } = new();

        [LocDisplayName("{=wehigXCC}Include Default Shouts"),
         LocCategory("Shouts", "{=UhUpH8C8}Shouts"), 
         LocDescription("{=m6Vv2LBt}Whether to include default shouts"), 
         PropertyOrder(2), UsedImplicitly]
        public bool IncludeDefaultShouts { get; set; } = true;
        #endregion
        #endregion

        #region Public Interface
        [YamlIgnore, Browsable(false)]
        public float DifficultyScalingClamped => MathF.Clamp(DifficultyScaling, 0, 5);

        [YamlIgnore, Browsable(false)]
        public IEnumerable<AchievementDef> ValidAchievements => Achievements.Where(a => a.Enabled);

        public AchievementDef GetAchievement(Guid id) => ValidAchievements?.FirstOrDefault(a => a.ID == id);
        
        public float GetCooldownTime(int summoned) 
           => (float) (Math.Pow(SummonCooldownUseMultiplier, Mathf.Max(0, summoned - 1)) * SummonCooldownInSeconds);
        #endregion

        #region IUpdateFromDefault
        public void OnUpdateFromDefault(Settings defaultSettings)
        {
            SettingsHelpers.MergeCollectionsSorted(
                KillStreaks, 
                Get(defaultSettings).KillStreaks,
                (a, b) => a.ID == b.ID,
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
                generator.H1("{=F6vM1OJo}Common Config".Translate());
                DocumentationHelpers.AutoDocument(generator, this);

                var killStreaks = KillStreaks.Where(k => k.Enabled).ToList();
                if (killStreaks.Any())
                {
                    generator.H2("{=3DZYc6hN}Kill Streaks".Translate());
                    generator.Table("kill-streaks", () =>
                    {
                        generator.TR(() => generator
                            .TH("{=uUzmy7Lh}Name".Translate())
                            .TH("{=mG7HzT0z}Kills Required".Translate())
                            .TH("{=sHWjkhId}Reward".Translate())
                        );
                        foreach (var k in killStreaks
                            .OrderBy(k => k.KillsRequired))
                        {
                            generator.TR(() => 
                                generator.TD(k.Name.ToString()).TD($"{k.KillsRequired}").TD(() =>
                                {
                                    if (k.GoldReward > 0) generator.P($"{k.GoldReward}{Naming.Gold}");
                                    if (k.XPReward > 0) generator.P($"{k.XPReward}{Naming.XP}");
                                }));
                        }
                    });
                }
                
                var achievements = ValidAchievements.Where(a => a.Enabled).ToList();
                if (achievements.Any())
                {
                    generator.H2("{=ZW9XlwY7}Achievements".Translate());
                    generator.Table("achievements", () =>
                    {
                        generator.TR(() => generator
                            .TH("{=uUzmy7Lh}Name".Translate())
                            .TH("{=TFbiD0CZ}Requirements".Translate())
                            .TH("{=sHWjkhId}Reward".Translate())
                        );
                        foreach (var a in achievements
                            .OrderBy(a => a.Name))
                        {
                            generator.TR(() =>
                                generator.TD(a.Name.ToString())
                                    .TD(() =>
                                    {
                                        foreach (var r in a.Requirements)
                                        {
                                            // ReSharper disable once SuspiciousTypeConversion.Global
                                            if (r is IDocumentable d)
                                            {
                                                d.GenerateDocumentation(generator);
                                            }
                                            else
                                            {
                                                generator.P(r.ToString());
                                            }
                                        }
                                    })
                                    .TD(() =>
                                    {
                                        if (a.GoldGain > 0) generator.P($"{a.GoldGain}{Naming.Gold}");
                                        if (a.XPGain > 0) generator.P($"{a.XPGain}{Naming.XP}");
                                        if (a.GiveItemReward) generator.P($"{Naming.Item}: {a.ItemReward}");
                                    })
                                );
                        }
                    });
                }
            });
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
    }
}