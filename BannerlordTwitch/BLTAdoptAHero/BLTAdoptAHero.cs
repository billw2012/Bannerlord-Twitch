using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Actions.Util;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox;
using SandBox.View;
using SandBox.View.Missions;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

#pragma warning disable 649

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [HarmonyPatch]
    public class BLTAdoptAHeroModule : MBSubModuleBase
    {
        private Harmony harmony;
        public const string Name = "BLTAdoptAHero";
        public const string Ver = "1.4.1";

        internal static GlobalCommonConfig CommonConfig { get; private set; }
        internal static GlobalTournamentConfig TournamentConfig { get; private set; }
        internal static GlobalHeroClassConfig HeroClassConfig { get; private set; }
        public BLTAdoptAHeroModule()
        {
            ActionManager.RegisterAll(typeof(BLTAdoptAHeroModule).Assembly);
            GlobalCommonConfig.Register();
            GlobalTournamentConfig.Register();
            GlobalHeroClassConfig.Register();
        }

        public override void OnMissionBehaviourInitialize(Mission mission)
        {
            if(mission.GetMissionBehaviour<MissionNameMarkerUIHandler>() == null &&
               (MissionHelpers.InSiegeMission() || MissionHelpers.InFieldBattleMission() || Mission.Current?.GetMissionBehaviour<TournamentFightMissionController>() != null))
            {
                mission.AddMissionBehaviour(SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission));
            }
        }
        
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(MissionNameMarkerTargetVM), MethodType.Constructor, typeof(Agent))]
        public static void MissionNameMarkerTargetVMConstructorPostfix(MissionNameMarkerTargetVM __instance, Agent agent)
        {
            if (MissionHelpers.InSiegeMission() || MissionHelpers.InFieldBattleMission() || MissionHelpers.InHideOutMission())
            {
                if (Agent.Main != null && agent.IsEnemyOf(Agent.Main) || agent.Team.IsEnemyOf(Mission.Current.PlayerTeam))
                {
                    __instance.MarkerType = 2;
                }
                else if (Agent.Main != null && agent.IsFriendOf(Agent.Main) || agent.Team.IsFriendOf(Mission.Current.PlayerTeam))
                {
                    __instance.MarkerType = 0;
                }
            }
        }

        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(MissionNameMarkerVM), MethodType.Constructor, typeof(Mission), typeof(Camera))]
        // ReSharper disable once RedundantAssignment
        public static void MissionNameMarkerVMConstructorPostfix(MissionNameMarkerVM __instance, Mission mission, ref Vec3 ____heightOffset)
        {
            ____heightOffset = new Vec3(0, 0, 4, -1);
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            if (harmony == null)
            {
                harmony = new Harmony("mod.bannerlord.bltadoptahero");
                harmony.PatchAll();
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if(game.GameType is Campaign) 
            {
                // Reload settings here so they are fresh
                CommonConfig = GlobalCommonConfig.Get();
                TournamentConfig = GlobalTournamentConfig.Get();
                HeroClassConfig = GlobalHeroClassConfig.Get();

                var campaignStarter = (CampaignGameStarter) gameStarterObject;
                campaignStarter.AddBehavior(new BLTAdoptAHeroCampaignBehavior());
                JoinTournament.AddBehaviors(campaignStarter);
            }
        }
        
        public override void BeginGameStart(Game game)
        {
        }
        
        // public override void OnCampaignStart(Game game, object starterObject)
        // {
        //     base.OnCampaignStart(game, starterObject);
        //     // JoinTournament.SetupGameMenus(starterObject as CampaignGameStarter);
        // }
        
        public override void OnGameEnd(Game game)
        {
            base.OnGameEnd(game);
            if(game.GameType is Campaign campaign) 
            {
                JoinTournament.OnGameEnd(campaign);
            }
        }

        internal const string Tag = "[BLT]";
    }

    [CategoryOrder("General", 1)]
    [CategoryOrder("Kill Rewards", 2)]
    [CategoryOrder("Battle End Rewards", 3)]
    [CategoryOrder("Shouts", 4)]
    [CategoryOrder("Kill Streaks", 5)]
    internal class GlobalCommonConfig
    {

        private const string ID = "Adopt A Hero - General Config";

        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalCommonConfig));
        internal static GlobalCommonConfig Get() => ActionManager.GetGlobalConfig<GlobalCommonConfig>(ID);

        [Category("General"), Description("Whether the hero is allowed to die"), PropertyOrder(3)]
        public bool AllowDeath { get; [UsedImplicitly] set; }

        [Category("General"), Description("Whether the hero will always start with full health"), PropertyOrder(4)]
        public bool StartWithFullHealth { get; set; } = true;

        [Category("General"),
         Description("Amount to multiply normal starting health by, to give heroes better staying power vs others"),
         PropertyOrder(5)]
        public float StartHealthMultiplier { get; set; } = 2;

        [Category("General"),
         Description("Amount to multiply normal retinue starting health by, to give retinue better staying power vs others"),
         PropertyOrder(6)]
        public float StartRetinueHealthMultiplier { get; set; } = 2;

        [Category("General"),
         Description("Reduces morale loss when summoned heroes die"),
         PropertyOrder(7)]
        public float MoraleLossFactor { get; set; } = 0.5f;

        [Category("General"),
         Description("Multiplier applied to all rewards for subscribers (less or equal to 1 means no boost)"),
         PropertyOrder(10)]
        public float SubBoost { get; set; } = 1;

        [Category("General"),
         Description("Use raw XP values instead of adjusting by focus and attributes, also ignoring skill cap. This avoids characters getting stuck when focus and attributes are not well distributed. You should consider hiding "),
         PropertyOrder(11)]
        public bool UseRawXP { get; set; } = true;

        [Category("General"), Description("Whether an adopted heroes retinue should spawn in the same formation as the hero (otherwise they will go into default formations)"), PropertyOrder(12)]
        public bool RetinueUseHeroesFormation { get; [UsedImplicitly] set; }

        [Category("General"), Description("Minimum time between summons for a specific hero"), PropertyOrder(13)]
        public int CooldownInSeconds { get; [UsedImplicitly] set; } = 60;

        [Category("Kill Rewards"), Description("Gold the hero gets for every kill"), PropertyOrder(1)]
        public int GoldPerKill { get; set; } = 5000;

        [Category("Kill Rewards"), Description("XP the hero gets for every kill"), PropertyOrder(2)]
        public int XPPerKill { get; set; } = 5000;

        [Category("Kill Rewards"), Description("XP the hero gets for being killed"), PropertyOrder(3)]
        public int XPPerKilled { get; set; } = 2000;

        [Category("Kill Rewards"), Description("HP the hero gets for every kill"), PropertyOrder(4)]
        public int HealPerKill { get; set; } = 20;

        [Category("Kill Rewards"), Description("Gold the hero gets for every kill their retinue gets"),
         PropertyOrder(5)]
        public int RetinueGoldPerKill { get; set; } = 2500;

        [Category("Kill Rewards"), Description("HP the hero's retinue gets for every kill"), PropertyOrder(6)]
        public int RetinueHealPerKill { get; set; } = 50;

        [Category("Kill Rewards"),
         Description("How much to scale the kill rewards by, based on relative level of the two characters. " +
                     "If this is 0 (or not set) then the rewards are always as specified, if this is higher than 0 " +
                     "then the rewards increase if the killed unit is higher level than the hero, and decrease if it " +
                     "is lower. At a value of 0.5 (recommended) at level difference of 10 would give about 2.5 times " +
                     "the normal rewards for gold, xp and health."),
         PropertyOrder(7)]
        public float RelativeLevelScaling { get; set; } = 0.5f;

        [Category("Kill Rewards"),
         Description("Caps the maximum multiplier for the level difference, defaults to 5 if not specified"),
         PropertyOrder(8)]
        public float LevelScalingCap { get; set; } = 5;

        [Category("Battle End Rewards"), Description("Gold won if the heroes side wins"), PropertyOrder(1)]
        public int WinGold { get; set; } = 10000;

        [Category("Battle End Rewards"), Description("XP the hero gets if the heroes side wins"), PropertyOrder(2)]
        public int WinXP { get; set; } = 10000;

        [Category("Battle End Rewards"), Description("Gold lost if the heroes side loses"), PropertyOrder(3)]
        public int LoseGold { get; set; } = 5000;

        [Category("Battle End Rewards"), Description("XP the hero gets if the heroes side loses"), PropertyOrder(4)]
        public int LoseXP { get; set; } = 5000;
        
        [Category("Battle End Rewards"), Description("Apply difficulty scaling to players side"), PropertyOrder(5)]
        public bool DifficultyScalingOnPlayersSide { get; set; } = true;
        
        [Category("Battle End Rewards"), Description("Apply difficulty scaling to enemy side"), PropertyOrder(6)]
        public bool DifficultyScalingOnEnemySide { get; set; } = true;
        
        [Category("Battle End Rewards"), Description("End reward difficulty scaling: determines the extent to which higher difficulty battles increase the above rewards"), PropertyOrder(7)]
        public float DifficultyScaling { get; set; } = 1;

        [YamlIgnore, Browsable(false)]
        public float DifficultyScalingClamped => MathF.Clamp(DifficultyScaling, 0, 5);
        
        [Category("Battle End Rewards"), Description("Min difficulty scaling multiplier"), PropertyOrder(8)]
        public float DifficultyScalingMin { get; set; } = 0.2f;
        [YamlIgnore, Browsable(false)]
        public float DifficultyScalingMinClamped => MathF.Clamp(DifficultyScalingMin, 0, 1);

        [Category("Battle End Rewards"), Description("Max difficulty scaling multiplier"), PropertyOrder(9)]
        public float DifficultyScalingMax { get; set; } = 3f;
        [YamlIgnore, Browsable(false)]
        public float DifficultyScalingMaxClamped => Math.Max(DifficultyScalingMax, 1f);
        
        [Category("Shouts"), Description("Custom shouts"), PropertyOrder(1)]
        public List<SummonHero.Shout> Shouts { get; set; } = new();

        [Category("Shouts"), Description("Whether to include default shouts"), PropertyOrder(2)]
        public bool IncludeDefaultShouts { get; set; } = true;

        [Category("Kill Streak Rewards"), Description("Kill Streaks"), PropertyOrder(1)]
        public List<KillStreakRewards> KillStreaks { get; set; } = new();

        [Category("Kill Streak Rewards"), Description("Whether to use the popup banner to announce kill streaks. Will only print in the overlay instead if disabled."), PropertyOrder(2)]
        public bool ShowKillStreakPopup { get; set; } = true;

        [Category("Kill Streak Rewards"), Description("Sound to play when killstreak popup is disabled."),
         PropertyOrder(3)]
        public Log.Sound KillStreakPopupAlertSound { get; [UsedImplicitly] set; } = Log.Sound.Horns2;
        
        [Category("Kill Streak Rewards"), Description("The level at which the rewards normalize and start to reduce (if relative level scaling is enabled)."), PropertyOrder(4)]
        public int ReferenceLevelReward { get; set; } = 15;
     }
    
    [CategoryOrder("General", 1)]
    [CategoryOrder("Match Rewards", 2)]
    internal class GlobalTournamentConfig
    {
        private const string ID = "Adopt A Hero - Tournament Config";
        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalTournamentConfig));
        internal static GlobalTournamentConfig Get() => ActionManager.GetGlobalConfig<GlobalTournamentConfig>(ID);

        [Category("General"),
         Description("Amount to multiply normal starting health by"),
         PropertyOrder(1)]
        public float StartHealthMultiplier { get; set; } = 2;
        
        [Category("Rewards"), Description("Gold won if the hero wins the tournaments"), PropertyOrder(1)]
        public int WinGold { get; set; } = 50000;

        [Category("Rewards"), Description("XP given if the hero wins the tournaments"), PropertyOrder(2)]
        public int WinXP { get; set; } = 50000;

        [Category("Rewards"), Description("XP given if the hero participates in a tournament"), PropertyOrder(3)]
        public int ParticipateXP { get; set; } = 10000;

        [Category("Match Rewards"), Description("Gold won if the hero wins their match"), PropertyOrder(1)]
        public int WinMatchGold { get; set; } = 10000;

        [Category("Match Rewards"), Description("XP given if the hero wins their match"), PropertyOrder(2)]
        public int WinMatchXP { get; set; } = 10000;

        [Category("Match Rewards"), Description("XP given if the hero participates in a match"), PropertyOrder(3)]
        public int ParticipateMatchXP { get; set; } = 2500;
    }

    internal class GlobalHeroClassConfig
    {
        private const string ID = "Adopt A Hero - Class Config";
        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalHeroClassConfig));
        internal static GlobalHeroClassConfig Get() => ActionManager.GetGlobalConfig<GlobalHeroClassConfig>(ID);
        
        [Description("Defined classes")] 
        public List<HeroClassDef> ClassDefs { get; set; } = new();

        [Browsable(false), YamlIgnore]
        public IEnumerable<string> ClassNames => ClassDefs.Select(c => c.Name?.ToLower());

        public HeroClassDef GetClass(Guid id) 
            => ClassDefs.FirstOrDefault(c => c.ID == id);

        public HeroClassDef FindClass(string search) 
            => ClassDefs.FirstOrDefault(c => c.Name.Equals(search, StringComparison.InvariantCultureIgnoreCase));

        // public HeroClassDef FindClosestClass(Hero hero) =>
        //     // Find class that has maximum sum of matched skills
        //     ClassDefs.OrderByDescending(c
        //             // Sum the heroes skill values for all the class skills
        //             => c.Skills.Sum(s => hero.GetSkillValue(SkillGroup.GetSkill(s.Skill))))
        //         .FirstOrDefault();
    }
    
    // We could do this, but they could also gain money so...
    // public static class Patches
    // {
    //     [HarmonyPrefix]
    //     [HarmonyPatch(typeof(Hero), nameof(Hero.Gold), MethodType.Setter)]
    //     public static bool set_GoldPrefix(Hero __instance, int value)
    //     {
    //         // Don't allow changing gold of our adopted heroes, as we use it ourselves
    //         return !__instance.GetName().Contains(AdoptAHero.Tag);
    //     }
    // }
}