using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Powers;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox;
using SandBox.View;
using SandBox.View.Missions;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

#pragma warning disable 649

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [HarmonyPatch]
    public class BLTAdoptAHeroModule : MBSubModuleBase
    {
        private Harmony harmony;
        public const string Name = "BLTAdoptAHero";
        public const string Ver = "1.4.6";

        internal static GlobalCommonConfig CommonConfig { get; private set; }
        internal static GlobalTournamentConfig TournamentConfig { get; private set; }
        internal static GlobalHeroClassConfig HeroClassConfig { get; private set; }
        internal static GlobalHeroPowerConfig HeroPowerConfig { get; private set; }

        public BLTAdoptAHeroModule()
        {
            ActionManager.RegisterAll(typeof(BLTAdoptAHeroModule).Assembly);

            GlobalCommonConfig.Register();
            GlobalTournamentConfig.Register();
            GlobalHeroClassConfig.Register();
            GlobalHeroPowerConfig.Register();

            HeroPowerDefBase.RegisterAll(typeof(BLTAdoptAHeroModule).Assembly);
        }

        public override void OnMissionBehaviourInitialize(Mission mission)
        {
            // Add the marker overlay for appropriate mission types
            if(mission.GetMissionBehaviour<MissionNameMarkerUIHandler>() == null 
               && (MissionHelpers.InSiegeMission() 
                   || MissionHelpers.InFieldBattleMission() 
                   || Mission.Current?.GetMissionBehaviour<TournamentFightMissionController>() != null))
            {
                mission.AddMissionBehaviour(SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission));
            }
            mission.AddMissionBehaviour(new BLTAdoptAHeroCommonMissionBehavior());
            mission.AddMissionBehaviour(new BLTAdoptAHeroCustomMissionBehavior());
            mission.AddMissionBehaviour(new BLTSummonBehavior());
            mission.AddMissionBehaviour(new BLTRemoveAgentsBehavior());
            mission.AddMissionBehaviour(new BLTHeroPowersMissionBehavior());
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

        [UsedImplicitly, HarmonyPatch(typeof(DefaultClanTierModel), nameof(DefaultClanTierModel.GetCompanionLimit))]
        public static void Postfix(ref int __result)
        {
            if (CommonConfig != null && CommonConfig.BreakCompanionLimit)
            {
                __result = Clan.PlayerClan.Companions.Count + 1;
            }
            return;
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
                HeroPowerConfig = GlobalHeroPowerConfig.Get();

                var campaignStarter = (CampaignGameStarter) gameStarterObject;
                campaignStarter.AddBehavior(new BLTAdoptAHeroCampaignBehavior());
                campaignStarter.AddBehavior(new BLTTournamentQueueBehavior());
                campaignStarter.AddBehavior(new BLTCustomItemsCampaignBehavior());
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
}