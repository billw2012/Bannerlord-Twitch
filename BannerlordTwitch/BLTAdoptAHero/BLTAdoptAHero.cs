using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.UI;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox.Tournaments.MissionLogics;
using SandBox.View;
using SandBox.View.Missions;
using SandBox.ViewModelCollection.Missions.NameMarker;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.NameMarker;
using TaleWorlds.MountAndBlade.Source.Missions;

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

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            try
            {
                // Add the marker overlay for appropriate mission types
                if (mission.GetMissionBehavior<MissionNameMarkerUIHandler>() == null
                    && (mission.GetMissionBehavior<BattleSpawnLogic>() != null
                        || mission.GetMissionBehavior<TournamentFightMissionController>() != null))
                {
                    mission.AddMissionBehavior(SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission));
                }

                mission.AddMissionBehavior(new BLTAdoptAHeroCommonMissionBehavior());
                mission.AddMissionBehavior(new BLTAdoptAHeroCustomMissionBehavior());
                mission.AddMissionBehavior(new BLTSummonBehavior());
                mission.AddMissionBehavior(new BLTRemoveAgentsBehavior());
                mission.AddMissionBehavior(new BLTHeroPowersMissionBehavior());
            }
            catch (Exception e)
            {
                Log.Exception(nameof(OnMissionBehaviorInitialize), e);
            }
        }
        
        [UsedImplicitly, HarmonyPostfix, 
         HarmonyPatch(typeof(MissionNameMarkerTargetVM), MethodType.Constructor, typeof(Agent), typeof(bool))]
        public static void MissionNameMarkerTargetVMConstructorPostfix(MissionNameMarkerTargetVM __instance, Agent agent)
        {
            if (MissionHelpers.InSiegeMission() || MissionHelpers.InFieldBattleMission() || MissionHelpers.InHideOutMission())
            {
                if (Agent.Main != null && agent.IsEnemyOf(Agent.Main) || Mission.Current.PlayerTeam?.IsValid == true && agent.Team.IsEnemyOf(Mission.Current.PlayerTeam))
                {
                    __instance.NameType = MissionNameMarkerTargetVM.NameTypeEnemy;
                    __instance.IsFriendly = false;
                    __instance.IsEnemy = true;
                    __instance.IsTracked = true;

                }
                else if (Agent.Main != null && agent.IsFriendOf(Agent.Main) || Mission.Current.PlayerTeam?.IsValid == true && agent.Team.IsFriendOf(Mission.Current.PlayerTeam))
                {
                    __instance.NameType = MissionNameMarkerTargetVM.NameTypeFriendly;
                    __instance.IsFriendly = true;
                    __instance.IsEnemy = false;
                    __instance.IsTracked = true;
                }
            }
        }

        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(NameMarkerScreenWidget), "OnLateUpdate")]
        public static void NameMarkerScreenWidget_OnLateUpdatePostfix(List<NameMarkerListPanel> ____markers)
        {
            foreach (var marker in ____markers)
            {
                marker.IsFocused = marker.IsInScreenBoundaries;
            }
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
        
        public override float CalculateDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData,
            in MissionWeapon weapon, float baseDamage)
        {
            return previousModel.CalculateDamage(in attackInformation, in collisionData, in weapon, baseDamage);
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

        public override float CalculatePassiveAttackDamage(BasicCharacterObject attackerCharacter, in AttackCollisionData collisionData,
            float baseDamage)
        {
            return previousModel.CalculatePassiveAttackDamage(attackerCharacter, in collisionData, baseDamage);
        }

        public override MeleeCollisionReaction DecidePassiveAttackCollisionReaction(Agent attacker, Agent defender, bool isFatalHit)
        {
            return previousModel.DecidePassiveAttackCollisionReaction(attacker, defender, isFatalHit);
        }

        public override float CalculateShieldDamage(in AttackInformation attackInformation, float baseDamage)
        {
            return previousModel.CalculateShieldDamage(in attackInformation, baseDamage);
        }

        public override float GetDamageMultiplierForBodyPart(BoneBodyPartType bodyPart, DamageTypes type, bool isHuman)
        {
            return previousModel.GetDamageMultiplierForBodyPart(bodyPart, type, isHuman);
        }

        public override bool CanWeaponIgnoreFriendlyFireChecks(WeaponComponentData weapon)
        {
            return previousModel.CanWeaponIgnoreFriendlyFireChecks(weapon);
        }

        public override bool CanWeaponDismount(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow,
            in AttackCollisionData collisionData)
        {
            return previousModel.CanWeaponDismount(attackerAgent, attackerWeapon, in blow, in collisionData);
        }

        public override bool CanWeaponKnockback(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow,
            in AttackCollisionData collisionData)
        {
            return previousModel.CanWeaponKnockback(attackerAgent, attackerWeapon, in blow, in collisionData);
        }

        public override bool CanWeaponKnockDown(Agent attackerAgent, Agent victimAgent, WeaponComponentData attackerWeapon, in Blow blow,
            in AttackCollisionData collisionData)
        {
            return previousModel.CanWeaponKnockDown(attackerAgent, victimAgent, attackerWeapon, in blow, in collisionData);
        }

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

        public override bool DecideAgentShrugOffBlow(Agent victimAgent, AttackCollisionData collisionData, in Blow blow)
        {
            return previousModel.DecideAgentShrugOffBlow(victimAgent, collisionData, in blow);
        }

        public override bool DecideAgentDismountedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData,
            WeaponComponentData attackerWeapon, in Blow blow)
        {
            return previousModel.DecideAgentDismountedByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
        }

        public override bool DecideAgentKnockedBackByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData,
            WeaponComponentData attackerWeapon, in Blow blow)
        {
            return previousModel.DecideAgentKnockedBackByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
        }

        public override bool DecideAgentKnockedDownByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData,
            WeaponComponentData attackerWeapon, in Blow blow)
        {
            return previousModel.DecideAgentKnockedDownByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
        }

        public override bool DecideMountRearedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData,
            WeaponComponentData attackerWeapon, in Blow blow)
        {
            return previousModel.DecideMountRearedByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
        }

        public override float GetDismountPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow,
            in AttackCollisionData collisionData)
        {
            return previousModel.GetDismountPenetration(attackerAgent, attackerWeapon, in blow, in collisionData);
        }

        public override float GetKnockBackPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow,
            in AttackCollisionData collisionData)
        {
            return previousModel.GetKnockBackPenetration(attackerAgent, attackerWeapon, in blow, in collisionData);
        }

        public override float GetKnockDownPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow,
            in AttackCollisionData collisionData)
        {
            return previousModel.GetKnockDownPenetration(attackerAgent, attackerWeapon, in blow, in collisionData);
        }

        public override float GetHorseChargePenetration()
        {
            return previousModel.GetHorseChargePenetration();
        }
    }
}