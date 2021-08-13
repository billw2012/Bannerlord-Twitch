using System.Collections.Generic;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    // Just because its impossible to use ref parameters in a lambda.
    // Instead, to allow events to modify the attack data, it is wrapped in this class,
    // then copied back once all the handlers are complete
    public class RefHandle<T>
    {
        public RefHandle() {}
        public RefHandle(T data) { this.Data = data; }
        
        public T Data;
    }

    [HarmonyPatch, UsedImplicitly]
    public class BLTHeroPowersMissionBehavior : AutoMissionBehavior<BLTHeroPowersMissionBehavior>
    {
        #region Mission Event Handling
        
        private readonly PowerHandler powerHandler = new();
        #endregion

        public static PowerHandler PowerHandler => Current?.powerHandler;

        #region MissionBehaviour Overrides

        private readonly HashSet<Hero> activeHeroes = new();
        public override void OnAgentCreated(Agent agent)
        {
            SafeCall(() =>
            {
                var hero = agent.GetAdoptedHero();
                var heroClass = hero?.GetClass();
                // If the hero has a class (thus can have passive powers), and isn't already
                // known, then call the init method for the passive powers
                if (hero != null && heroClass != null && activeHeroes.Add(hero))
                {
                    heroClass.PassivePower?.OnHeroJoinedBattle(hero);
                }
            });
        }

        public override void OnAgentBuild(Agent agent, Banner banner) 
            => powerHandler.CallHandlersForAgent(agent, handlers => handlers.AgentBuild(agent));

        // public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled)
        //     => CallHandlersForAgent(agent, (hero, handlers) => handlers.OnMissileHit(attacker, victim, isCanceled));

        private void MissileCollision(Mission.MissileCollisionReaction collisionReaction,
            Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex,
            bool attachedToShield,
            MatrixFrame attachLocalFrame, Mission.Missile missile)
            => powerHandler.CallHandlersForAgent(attackerAgent, handlers
                => handlers.MissileCollisionReaction(collisionReaction, attackerAgent, attachedAgent, 
                    attachedBoneIndex, attachedToShield, attachLocalFrame, missile));

        protected override void OnAgentControllerChanged(Agent agent)
            => powerHandler.CallHandlersForAgent(agent, 
                handlers => handlers.AgentControllerChanged(agent));

        public class DecideWeaponCollisionReactionParams
        {
            public Blow registeredBlow;
            public AttackCollisionData collisionData;
            public bool isFatalHit;
            public bool isShruggedOff;
            public MeleeCollisionReaction colReaction;
        }
        
        private void DecideWeaponCollisionReactionCallback(Blow registeredBlow, ref AttackCollisionData collisionData, 
            Agent attackerAgent, Agent victimAgent, bool isFatalHit, bool isShruggedOff, 
            ref MeleeCollisionReaction colReaction)
        {
            var param = new DecideWeaponCollisionReactionParams
            {
                registeredBlow = registeredBlow,
                collisionData = collisionData,
                isFatalHit = isFatalHit,
                isShruggedOff = isShruggedOff,
                colReaction = colReaction,
            };

            if (!powerHandler.CallHandlersForAgentPair(attackerAgent, victimAgent, 
                handlers => handlers.DecideWeaponCollisionReaction(attackerAgent, victimAgent, param))) 
                return;
            
            colReaction = param.colReaction;
        }
        
        public class MeleeHitParams
        {
            public AttackCollisionData collisionData;
            public float inOutMomentumRemaining;
            public MeleeCollisionReaction colReaction;
            public CrushThroughState crushThroughState;
            public bool crushedThroughWithoutAgentCollision;
        }

        private void MeleeHitCallback(ref AttackCollisionData collisionData, Agent attackerAgent, Agent victimAgent,
            ref float inOutMomentumRemaining, ref MeleeCollisionReaction colReaction,
            ref CrushThroughState crushThroughState, ref bool crushedThroughWithoutAgentCollision)
        {
            var param = new MeleeHitParams
            {
                collisionData = collisionData,
                inOutMomentumRemaining = inOutMomentumRemaining,
                colReaction = colReaction,
                crushThroughState = crushThroughState,
                crushedThroughWithoutAgentCollision = crushedThroughWithoutAgentCollision,
            };

            if (!powerHandler.CallHandlersForAgentPair(attackerAgent, victimAgent, 
                handlers => handlers.DoMeleeHit(attackerAgent, victimAgent, param),
                handlers => handlers.TakeMeleeHit(victimAgent, attackerAgent, param))) 
                return;
            
            collisionData = param.collisionData;
            inOutMomentumRemaining = param.inOutMomentumRemaining;
            colReaction = param.colReaction;
            crushThroughState = param.crushThroughState;
            crushedThroughWithoutAgentCollision = param.crushedThroughWithoutAgentCollision;
        }
        
        private void PostMeleeHitCallback(ref AttackCollisionData collisionData, Agent attackerAgent, Agent victimAgent,
            ref float inOutMomentumRemaining, ref MeleeCollisionReaction colReaction,
            ref CrushThroughState crushThroughState, ref bool crushedThroughWithoutAgentCollision)
        {
            var param = new MeleeHitParams
            {
                collisionData = collisionData,
                inOutMomentumRemaining = inOutMomentumRemaining,
                colReaction = colReaction,
                crushThroughState = crushThroughState,
                crushedThroughWithoutAgentCollision = crushedThroughWithoutAgentCollision,
            };

            if (!powerHandler.CallHandlersForAgentPair(attackerAgent, victimAgent, 
                handlers => handlers.PostDoMeleeHit(attackerAgent, victimAgent, param),
                handlers => handlers.PostTakeMeleeHit(victimAgent, attackerAgent, param))) 
                return;
            
            collisionData = param.collisionData;
            inOutMomentumRemaining = param.inOutMomentumRemaining;
            colReaction = param.colReaction;
            crushThroughState = param.crushThroughState;
            crushedThroughWithoutAgentCollision = param.crushedThroughWithoutAgentCollision;
        }

        public class MissileHitParams
        {
            public AttackCollisionData collisionData;
            public bool? overridePassThrough;
        }
        
        private void MissileHitCallback(ref AttackCollisionData collisionData, Agent attackerAgent, Agent victimAgent)
        {
            var param = new MissileHitParams
            {
                collisionData = collisionData,
            };
            if (!powerHandler.CallHandlersForAgentPair(attackerAgent, victimAgent, 
                handlers => handlers.DoMissileHit(attackerAgent, victimAgent, param),
                handlers => handlers.TakeMissileHit(victimAgent, attackerAgent, param))) 
                return;
            
            collisionData = param.collisionData;
        }
        
        // public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex,
        // Vec3 position, Vec3 velocity, Mat3 orientation,
        //     bool hasRigidBody, int forcedMissileIndex)
        // {
        //     base.OnAgentShootMissile(shooterAgent, weaponIndex, position, velocity, orientation, hasRigidBody, forcedMissileIndex);
        // }

        // private void ApplyHitDamage(Agent attackerAgent, Agent victimAgent,
        // ref AttackCollisionData attackCollisionData)
        // {
        //     if (attackerAgent?.IsMount == true)
        //         attackerAgent = attackerAgent.RiderAgent;
        //
        //     var acdRef = new RefHandle<AttackCollisionData>(attackCollisionData);
        //
        //     if (CallHandlersForAgentPair(attackerAgent, victimAgent, 
        //         handlers=> handlers.DoDamage(attackerAgent, victimHero,
        // victimAgent, acdRef),
        //         handlers=> handlers.TakeDamage(victimAgent, attackerHero,
        // attackerAgent, acdRef))) 
        //         return;
        //
        //     attackCollisionData = acdRef.Data;
        // }

        public class RegisterBlowParams
        {
            public bool AttackerIsMount;
            public bool VictimIsMount;
            public Blow blow;
            public AttackCollisionData collisionData;
            public MissionWeapon attackerWeapon;
            public CombatLogData combatLogData;
        }
        
        private void RegisterBlow(Agent attackerAgent, Agent victimAgent, ref Blow blow, 
            ref AttackCollisionData collisionData, ref MissionWeapon attackerWeapon, ref CombatLogData combatLogData)
        {
            var param = new RegisterBlowParams
            {
                AttackerIsMount = attackerAgent.IsMount,
                VictimIsMount = victimAgent.IsMount,
                blow = blow,
                collisionData = collisionData,
                attackerWeapon = attackerWeapon,
                combatLogData = combatLogData,
            };

            if (!powerHandler.CallHandlersForAgentPair(attackerAgent, victimAgent, 
                handlers => handlers.DoDamage(attackerAgent, victimAgent, param),
                handlers => handlers.TakeDamage(victimAgent, attackerAgent, param))) 
                return;

            blow = param.blow;
            collisionData = param.collisionData;
            attackerWeapon = param.attackerWeapon;
            combatLogData = param.combatLogData;
        }
        
        private void AddMissileAux(Agent shooterAgent, ref WeaponData weaponData, WeaponStatsData[] weaponStatsData)
        {
            var wdRef = new RefHandle<WeaponData>(weaponData);
            powerHandler.CallHandlersForAgent(shooterAgent, 
                handlers => handlers.AddMissile(shooterAgent, wdRef, weaponStatsData));
            weaponData = wdRef.Data;
        }

        public override void OnAgentRemoved(Agent victimAgent, Agent attackerAgent, AgentState agentState, 
            KillingBlow blow)
        {
            powerHandler.CallHandlersForAgentPair(attackerAgent, victimAgent,
                handlers => handlers.GotAKill(attackerAgent, victimAgent, agentState, blow),
                handlers => handlers.GotKilled(victimAgent, attackerAgent, agentState, blow));
        }

        protected override void OnEndMission()
        {
            powerHandler.CallHandlersForAll(handlers => handlers.MissionOver());
        }

        private const float SlowTickDuration = 2;
        private float slowTick;
            
        public override void OnMissionTick(float dt)
        {
            slowTick += dt;
            if (slowTick > SlowTickDuration)
            {
                slowTick -= SlowTickDuration;
                powerHandler.CallHandlersForAll(handlers => handlers.SlowTick(SlowTickDuration));
            }
            powerHandler.CallHandlersForAll(handlers => handlers.MissionTick(dt));
        }

        // public override void OnAgentMount(Agent agent)
        // {
        //     base.OnAgentMount(agent);
        // }
        //
        // public override void OnAgentDismount(Agent agent)
        // {
        //     base.OnAgentDismount(agent);
        // }
        //
        // public override void OnRegisterBlow(Agent attacker, Agent victim, GameEntity realHitEntity, Blow b,
        //     ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon)
        // {
        //     base.OnRegisterBlow(attacker, victim, realHitEntity, b, ref collisionData, in attackerWeapon);
        // }
        //
        // public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, 
        //     Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        // {
        //     base.OnAgentShootMissile(shooterAgent, weaponIndex, position, velocity, orientation, hasRigidBody, forcedMissileIndex);
        // }


        #endregion

        #region Patches

        // [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(Mission), "GetAttackCollisionResults",
        //      new[]
        //      {
        //          typeof(Agent), typeof(Agent), typeof(GameEntity), typeof(float), typeof(AttackCollisionData),
        //          typeof(MissionWeapon), typeof(bool), typeof(bool), typeof(bool), typeof(WeaponComponentData),
        //          typeof(CombatLogData)
        //      },
        //      new[]
        //      {
        //          ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref,
        //          ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out,
        //          ArgumentType.Out
        //      })]
        // public static void GetAttackCollisionResultsPostfix(Mission __instance, Agent attackerAgent,
        // Agent victimAgent, ref AttackCollisionData attackCollisionData)
        // {
        //     Current?.ApplyHitDamage(attackerAgent, victimAgent, ref attackCollisionData);
        // }
        
        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(Mission), "RegisterBlow")]
        private static void RegisterBlowPrefix(Agent attacker, Agent victim, GameEntity realHitEntity, ref Blow b,
            ref AttackCollisionData collisionData, ref MissionWeapon attackerWeapon, ref CombatLogData combatLogData)
        {
            Current?.RegisterBlow(attacker, victim, ref b, ref collisionData, 
                ref attackerWeapon, ref combatLogData);
        }

        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(Mission), "AddMissileAux")]
        private static void AddMissileAuxPrefix(bool isPrediction, Agent shooterAgent, ref WeaponData weaponData,
            WeaponStatsData[] weaponStatsData
            //, WeaponStatsData[] weaponStatsData, float damageBonus, ref Vec3 position, ref Vec3 direction,
            //ref Mat3 orientation, float baseSpeed, float speed, bool addRigidBody, GameEntity gameEntityToIgnore,
            //bool isPrimaryWeaponShot, out GameEntity missileEntity
            )
        {
            if (!isPrediction)
            {
                Current?.AddMissileAux(shooterAgent, ref weaponData, weaponStatsData);
            }
        }
        
        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(Mission), "HandleMissileCollisionReaction")]
        private static void HandleMissileCollisionReactionPrefix(Mission __instance, int missileIndex,
            Mission.MissileCollisionReaction collisionReaction, MatrixFrame attachLocalFrame, Agent attackerAgent,
            Agent attachedAgent, bool attachedToShield, sbyte attachedBoneIndex, MissionObject attachedMissionObject,
            Vec3 bounceBackVelocity, Vec3 bounceBackAngularVelocity, int forcedSpawnIndex)
        {
            var _missiles = (Dictionary<int, Mission.Missile>)AccessTools.Field(typeof(Mission), "_missiles")
                .GetValue(__instance);
            Current?.MissileCollision(collisionReaction, attackerAgent, attachedAgent, attachedBoneIndex,
                attachedToShield, attachLocalFrame, _missiles[missileIndex]);
        }
        
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(Mission), "DecideWeaponCollisionReaction")]
        private static void DecideWeaponCollisionReactionPostfix(Blow registeredBlow, 
            ref AttackCollisionData collisionData, Agent attacker, Agent defender, bool isFatalHit, bool isShruggedOff, 
            ref MeleeCollisionReaction colReaction)
        {
            Current?.DecideWeaponCollisionReactionCallback(registeredBlow, ref collisionData, attacker, 
                defender, isFatalHit, isShruggedOff, ref colReaction);
        }


        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(Mission), "MeleeHitCallback")]
        private static void MeleeHitCallbackPrefix(ref AttackCollisionData collisionData, Agent attacker, Agent victim,
            //GameEntity realHitEntity, 
            ref float inOutMomentumRemaining, ref MeleeCollisionReaction colReaction,
            ref CrushThroughState crushThroughState,
            //Vec3 blowDir, Vec3 swingDir,
            //ref HitParticleResultData hitParticleResultData, 
            ref bool crushedThroughWithoutAgentCollision
        )
        {
            Current?.MeleeHitCallback(
                ref collisionData, attacker, victim,
                //GameEntity realHitEntity, 
                ref inOutMomentumRemaining, ref colReaction,
                ref crushThroughState,
                //Vec3 blowDir, Vec3 swingDir,
                //ref HitParticleResultData hitParticleResultData, 
                ref crushedThroughWithoutAgentCollision
            );
        }
        
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(Mission), "MeleeHitCallback")]
        private static void MeleeHitCallbackPostfix(ref AttackCollisionData collisionData, Agent attacker, Agent victim,
            //GameEntity realHitEntity, 
            ref float inOutMomentumRemaining, ref MeleeCollisionReaction colReaction,
            ref CrushThroughState crushThroughState,
            //Vec3 blowDir, Vec3 swingDir,
            //ref HitParticleResultData hitParticleResultData, 
            ref bool crushedThroughWithoutAgentCollision
        )
        {
            Current?.PostMeleeHitCallback(
                ref collisionData, attacker, victim,
                //GameEntity realHitEntity, 
                ref inOutMomentumRemaining, ref colReaction,
                ref crushThroughState,
                //Vec3 blowDir, Vec3 swingDir,
                //ref HitParticleResultData hitParticleResultData, 
                ref crushedThroughWithoutAgentCollision
            );
        }

        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(Mission), "MissileHitCallback")]
        private static void MissileHitCallbackPrefix(
            //out int hitParticleIndex, 
            ref AttackCollisionData collisionData,
            //Vec3 missileStartingPosition, Vec3 missilePosition, Vec3 missileAngularVelocity, Vec3 movementVelocity,
            //MatrixFrame attachGlobalFrame, MatrixFrame affectedShieldGlobalFrame, int numDamagedAgents, 
            Agent attacker, Agent victim,
            //, GameEntity hitEntity
            bool __result
            )
        {
            Current?.MissileHitCallback(ref collisionData, attacker, victim);
        }

        #endregion
    }
}