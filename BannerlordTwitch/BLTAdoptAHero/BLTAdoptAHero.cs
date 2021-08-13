using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Powers;
using BLTAdoptAHero.UI;
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
using TaleWorlds.MountAndBlade.ComponentInterfaces;

#pragma warning disable 649

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [HarmonyPatch]
    public class BLTAdoptAHeroModule : MBSubModuleBase
    {
        private Harmony harmony;

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
            
            TournamentHub.Register();
            MissionInfoHub.Register();
        }

        public override void OnMissionBehaviourInitialize(Mission mission)
        {
            try
            {
                // Add the marker overlay for appropriate mission types
                if (mission.GetMissionBehaviour<MissionNameMarkerUIHandler>() == null
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
            catch (Exception e)
            {
                Log.Exception(nameof(OnMissionBehaviourInitialize), e);
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
            ____heightOffset = new (0, 0, 4);
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
            try
            {
                if (game.GameType is Campaign)
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

                    gameStarterObject.AddModel(new BLTAgentApplyDamageModel(gameStarterObject.Models
                        .OfType<AgentApplyDamageModel>().FirstOrDefault()));
                }
            }
            catch (Exception e)
            {
                Log.Exception(nameof(OnGameStart), e);
                MessageBox.Show($"Error in {nameof(OnGameStart)}, please report this on the discord: {e}", "Bannerlord Twitch Mod STARTUP ERROR");
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

    public class BLTAgentApplyDamageModel : AgentApplyDamageModel
    {
        private readonly AgentApplyDamageModel previousModel;

        public BLTAgentApplyDamageModel(AgentApplyDamageModel previousModel)
        {
            this.previousModel = previousModel;
        }
        
        public override float CalculateDamage(ref AttackInformation attackInformation, ref AttackCollisionData collisionData,
            in MissionWeapon weapon, float baseDamage)
        {
            return previousModel.CalculateDamage(ref attackInformation, ref collisionData, in weapon, baseDamage);
        }
    
        public override float CalculateEffectiveMissileSpeed(Agent attackerAgent, WeaponComponentData missileWeapon,
            ref Vec3 missileStartDirection, float missileStartSpeed)
        {
            return previousModel.CalculateEffectiveMissileSpeed(attackerAgent, missileWeapon, ref missileStartDirection, missileStartSpeed);
        }
    
        public override float CalculateDismountChanceBonus(Agent attackerAgent, WeaponComponentData weapon)
        {
            return previousModel.CalculateDismountChanceBonus(attackerAgent, weapon);
        }
    
        public override float CalculateKnockBackChanceBonus(Agent attackerAgent, WeaponComponentData weapon)
        {
            return previousModel.CalculateKnockBackChanceBonus(attackerAgent, weapon);
        }
    
        public override float CalculateKnockDownChanceBonus(Agent attackerAgent, WeaponComponentData weapon)
        {
            return previousModel.CalculateKnockDownChanceBonus(attackerAgent, weapon);
        }
    
        public class DecideMissileWeaponFlagsParams
        {
            public MissionWeapon missileWeapon;
            public WeaponFlags missileWeaponFlags;
        }
        
        public override void DecideMissileWeaponFlags(Agent attackerAgent, MissionWeapon missileWeapon, ref WeaponFlags missileWeaponFlags)
        {
            previousModel.DecideMissileWeaponFlags(attackerAgent, missileWeapon, ref missileWeaponFlags);
            
            var args = new DecideMissileWeaponFlagsParams
            {
                missileWeapon = missileWeapon,
                missileWeaponFlags = missileWeaponFlags,
            };

            if (BLTHeroPowersMissionBehavior.PowerHandler?.CallHandlersForAgent(attackerAgent,
                handlers => handlers.DecideMissileWeaponFlags(attackerAgent, args)
                ) == true)
            {
                missileWeaponFlags = args.missileWeaponFlags;
            }
        }
    
        public override void CalculateCollisionStunMultipliers(Agent attackerAgent, Agent defenderAgent, bool isAlternativeAttack,
            CombatCollisionResult collisionResult, WeaponComponentData attackerWeapon, WeaponComponentData defenderWeapon,
            out float attackerStunMultiplier, out float defenderStunMultiplier)
        {
            previousModel.CalculateCollisionStunMultipliers(attackerAgent, defenderAgent, isAlternativeAttack, collisionResult, attackerWeapon, defenderWeapon, out attackerStunMultiplier, out defenderStunMultiplier);
        }
    
        public override float CalculateStaggerThresholdMultiplier(Agent defenderAgent)
        {
            return previousModel.CalculateStaggerThresholdMultiplier(defenderAgent);
        }
    
        public override float CalculatePassiveAttackDamage(BasicCharacterObject attackerCharacter, ref AttackCollisionData collisionData,
            float baseDamage)
        {
            return previousModel.CalculatePassiveAttackDamage(attackerCharacter, ref collisionData, baseDamage);
        }
    
        public override MeleeCollisionReaction DecidePassiveAttackCollisionReaction(Agent attacker, Agent defender, bool isFatalHit)
        {
            return previousModel.DecidePassiveAttackCollisionReaction(attacker, defender, isFatalHit);
        }
    
        public override float CalculateShieldDamage(float baseDamage)
        {
            return previousModel.CalculateShieldDamage(baseDamage);
        }

#if e159
        public override float GetDamageMultiplierForBodyPart(BoneBodyPartType bodyPart, DamageTypes type)
        {
            return previousModel.GetDamageMultiplierForBodyPart(bodyPart, type);
        }
#else
        public override float GetDamageMultiplierForBodyPart(BoneBodyPartType bodyPart, DamageTypes type, bool isHuman)
        {
            return previousModel.GetDamageMultiplierForBodyPart(bodyPart, type, isHuman);
        }
#endif
        
        public class DecideCrushedThroughParams
        {
            public float totalAttackEnergy;
            public Agent.UsageDirection attackDirection;
            public StrikeType strikeType;
            public WeaponComponentData defendItem;
            public bool isPassiveUsageHit;
            public bool crushThrough; // set this to override the behaviour
        }
        
        public override bool DecideCrushedThrough(Agent attackerAgent, Agent defenderAgent, float totalAttackEnergy,
            Agent.UsageDirection attackDirection, StrikeType strikeType, WeaponComponentData defendItem, bool isPassiveUsageHit)
        {
            bool originalResult = previousModel.DecideCrushedThrough(attackerAgent, defenderAgent, totalAttackEnergy, attackDirection, strikeType, defendItem, isPassiveUsageHit);
            var args = new DecideCrushedThroughParams
            {
                totalAttackEnergy = totalAttackEnergy,
                attackDirection = attackDirection,
                strikeType = strikeType,
                defendItem = defendItem,
                isPassiveUsageHit = isPassiveUsageHit,
                crushThrough = originalResult,
            };

            BLTHeroPowersMissionBehavior.PowerHandler?.CallHandlersForAgentPair(attackerAgent, defenderAgent,
                handlers => handlers.DecideCrushedThrough(attackerAgent, defenderAgent, args));

            return args.crushThrough;
        }
        
        #if !e159 && !e1510 && !e160
        public override bool CanWeaponIgnoreFriendlyFireChecks(WeaponComponentData weapon)
        {
            return previousModel.CanWeaponIgnoreFriendlyFireChecks(weapon);
        }
        #endif
    }
}