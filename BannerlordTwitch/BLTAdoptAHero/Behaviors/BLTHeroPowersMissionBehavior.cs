using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Bannerlord.ButterLib.Common.Extensions;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using BLTAdoptAHero.Powers;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    // DOING:
    // Active Powers when activated should create a new ActivePowerInstance binding required info etc.
    // They should be strictly divided between Mission and Campaign map powers, with no overlap. 
    // Mission ones implement the mission events interface and are managed by BLTHeroPowersMissionBehavior.
    // Campaign ones implement the campaign interface and are managed by a campaign behaviour (needs to be made).
    // These will give correct lifetime management to the active instances, ensuring e.g. Agent handles don't escape 
    // from the Mission lifetime.
    
    // Just because its impossible to use ref parameters in a lambda.
    // Instead, to allow events to modify the attack data, it is wrapped in this class,
    // then copied back once all the handlers are complete
    public class AttackCollisionDataRef
    {
        public AttackCollisionData Data;
    }
    
    public class WeaponDataRef
    {
        public WeaponData Data;
    }


    [HarmonyPatch, UsedImplicitly]
    public class BLTHeroPowersMissionBehavior : AutoMissionBehavior<BLTHeroPowersMissionBehavior>
    {
        #region Mission Event Handler Delegates
        public delegate void AgentBuildDelegate(Hero hero, Agent agent);
        public delegate void MissionOverDelegate(Hero hero);
        public delegate void GotAKillDelegate(Hero hero, Agent agent, Hero killedHero, Agent killedAgent, 
            AgentState agentState, KillingBlow blow);
        public delegate void GotKilledDelegate(Hero hero, Agent agent, Hero killerHero, Agent killerAgent, 
            AgentState agentState, KillingBlow blow);

        public delegate void DoDamageDelegate(Hero hero, Agent agent, Hero victimHero, Agent victimAgent, 
            AttackCollisionDataRef attackCollisionData);
        public delegate void TakeDamageDelegate(Hero hero, Agent agent, Hero attackerHero, Agent attackerAgent, 
            AttackCollisionDataRef attackCollisionData);
        public delegate void MissionTickDelegate(Hero hero, float dt);
        public delegate void MissileCollisionDelegate(Mission.MissileCollisionReaction collisionReaction, 
            Hero attackerHero, Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex, 
            bool attachedToShield, MatrixFrame attachLocalFrame, Mission.Missile missile);
        public delegate void AgentControllerChangedDelegate(Hero hero, Agent agent);

        public delegate void AddMissileDelegate(Hero shooterHero, Agent shooterAgent, WeaponDataRef weaponData, WeaponStatsData[] weaponStatsData);
        #endregion

        #region Mission Event Handling
        public class Handlers
        {
            public event AgentBuildDelegate OnAgentBuild;
            public event MissionOverDelegate OnMissionOver;
            public event GotAKillDelegate OnGotAKill;
            public event GotKilledDelegate OnGotKilled;
            public event DoDamageDelegate OnDoDamage;
            public event TakeDamageDelegate OnTakeDamage;
            public event MissionTickDelegate OnSlowTick;
            public event MissionTickDelegate OnMissionTick;
            public event MissileCollisionDelegate OnMissileCollision;
            public event AgentControllerChangedDelegate OnAgentControllerChanged;
            public event AddMissileDelegate OnAddMissile;

            public void AgentBuild(Hero hero, Agent agent) => OnAgentBuild?.Invoke(hero, agent);
            public void MissionOver(Hero hero) => OnMissionOver?.Invoke(hero);
            public void GotAKill(Hero hero, Agent agent, Hero killedHero, Agent killedAgent, AgentState agentState, KillingBlow blow) 
                => OnGotAKill?.Invoke(hero, agent, killedHero, killedAgent, agentState, blow);
            public void GotKilled(Hero hero, Agent agent, Hero killerHero, Agent killerAgent, AgentState agentState, KillingBlow blow) 
                => OnGotKilled?.Invoke(hero, agent, killerHero, killerAgent, agentState, blow);
            public void DoDamage(Hero hero, Agent agent, 
                Hero victimHero, Agent victimAgent, AttackCollisionDataRef attackCollisionData) 
                => OnDoDamage?.Invoke(hero, agent, victimHero, victimAgent, attackCollisionData);
            public void TakeDamage(Hero hero, Agent agent, 
                Hero attackerHero, Agent attackerAgent, AttackCollisionDataRef attackCollisionData) 
                => OnTakeDamage?.Invoke(hero, agent, attackerHero, attackerAgent, attackCollisionData);
            public void SlowTick(Hero hero, float dt) => OnSlowTick?.Invoke(hero, dt);
            public void MissionTick(Hero hero, float dt) => OnMissionTick?.Invoke(hero, dt);
            public void MissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction,
                Hero attackerHero, Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex,
                bool attachedToShield, MatrixFrame attachLocalFrame, Mission.Missile missile) 
                => OnMissileCollision?.Invoke(collisionReaction, attackerHero, attackerAgent, attachedAgent,
                    attachedBoneIndex, attachedToShield, attachLocalFrame, missile);
            public void AgentControllerChanged(Hero hero, Agent agent) => OnAgentControllerChanged?.Invoke(hero, agent);

            public void AddMissile(Hero shooterHero, Agent shooterAgent, WeaponDataRef weaponData, WeaponStatsData[] weaponStatsData)
            {
                OnAddMissile?.Invoke(shooterHero, shooterAgent, weaponData, weaponStatsData);
            }
        }

        private readonly Dictionary<Hero, Dictionary<HeroPowerDefBase, Handlers>> heroPowerHandlers = new();

        public void ConfigureHandlers(Hero hero, HeroPowerDefBase power, Action<Handlers> configure)
        {
            if (!heroPowerHandlers.TryGetValue(hero, out var powerHandlers))
            {
                powerHandlers = new();
                heroPowerHandlers.Add(hero, powerHandlers);
            }
            
            if (!powerHandlers.TryGetValue(power, out var targetHandlers))
            {
                targetHandlers = new();
                powerHandlers.Add(power, targetHandlers);
            }
            
            configure(targetHandlers);
        }

        public void ClearHandlers(Hero hero, HeroPowerDefBase power)
        {
            if (heroPowerHandlers.TryGetValue(hero, out var powerHandlers))
            {
                powerHandlers.Remove(power);
            }
        }

        public bool HasHandlers(Hero hero, HeroPowerDefBase power) =>
            heroPowerHandlers.TryGetValue(hero, out var powerHandlers)
            && powerHandlers.ContainsKey(power);

        private void CallHandlersForAgent(Agent agent, Action<Hero, Handlers> call, [CallerMemberName] string callerName = "")
        {
            var hero = agent?.GetAdoptedHero();
            if (hero == null) return;
            CallHandlersForHero(hero, handlers => call(hero, handlers), callerName);
        }

        private void CallHandlersForHero(Hero hero, Action<Handlers> call, [CallerMemberName] string callerName = "")
        {
            SafeCall(() =>
            {
                if (heroPowerHandlers.TryGetValue(hero, out var powerHandlers))
                {
                    foreach (var handlers in powerHandlers.Values.ToList())
                    {
                        call(handlers);
                    }
                }
            }, callerName);
        }
        
        private void CallHandlersForAll(Action<Hero, Handlers> call, [CallerMemberName] string callerName = "")
        {
            SafeCall(() =>
            {
                foreach (var (hero, handlersMap) in heroPowerHandlers.ToList())
                {
                    foreach (var handlers in handlersMap.ToList())
                    {
                        call(hero, handlers.Value);
                    }
                }
            }, callerName);
        }
        #endregion

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
            => CallHandlersForAgent(agent, (hero, handlers) => handlers.AgentBuild(hero, agent));

        // public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled)
        //     => CallHandlersForAgent(agent, (hero, handlers) => handlers.OnMissileHit(attacker, victim, isCanceled));

        private void MissileCollision(Mission.MissileCollisionReaction collisionReaction,
            Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex,
            bool attachedToShield,
            MatrixFrame attachLocalFrame, Mission.Missile missile)
            => CallHandlersForAgent(attackerAgent, (hero, handlers) 
                => handlers.MissileCollisionReaction(collisionReaction, hero, attackerAgent, attachedAgent, 
                    attachedBoneIndex, attachedToShield, attachLocalFrame, missile));

        protected override void OnAgentControllerChanged(Agent agent)
            => CallHandlersForAgent(agent, (hero, handlers) => handlers.AgentControllerChanged(hero, agent));

        // public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation,
        //     bool hasRigidBody, int forcedMissileIndex)
        // {
        //     base.OnAgentShootMissile(shooterAgent, weaponIndex, position, velocity, orientation, hasRigidBody, forcedMissileIndex);
        // }

        private void ApplyHitDamage(Agent attackerAgent, Agent victimAgent, ref AttackCollisionData attackCollisionData)
        {
            var attackerHero = attackerAgent?.GetAdoptedHero();
            var victimHero = victimAgent?.GetAdoptedHero();

            if (attackerHero == null && victimHero == null)
                return;

            var acdRef = new AttackCollisionDataRef {Data = attackCollisionData};

            if (attackerHero != null)
            {
                CallHandlersForHero(attackerHero, handlers
                    => handlers.DoDamage(attackerHero, attackerAgent, victimHero, victimAgent, acdRef));
            }

            if (victimHero != null)
            {
                CallHandlersForHero(victimHero, handlers
                    => handlers.TakeDamage(victimHero, victimAgent, attackerHero, attackerAgent, acdRef));
            }

            attackCollisionData = acdRef.Data;
        }
        
        private void AddMissileAux(Agent shooterAgent, ref WeaponData weaponData, WeaponStatsData[] weaponStatsData)
        {
            var shooterHero = shooterAgent?.GetAdoptedHero();
            if (shooterHero == null)
                return;
            
            var wdRef = new WeaponDataRef {Data = weaponData};
            CallHandlersForHero(shooterHero, handlers => handlers.AddMissile(shooterHero, shooterAgent, wdRef, weaponStatsData));
            weaponData = wdRef.Data;
        }

        public override void OnAgentRemoved(Agent killedAgent, Agent killerAgent, AgentState agentState, KillingBlow blow)
        {
            var killerHero = killerAgent?.GetAdoptedHero();
            var killedHero = killedAgent?.GetAdoptedHero();

            if (killerHero != null)
            {
                CallHandlersForHero(killerHero, handlers
                    => handlers.GotAKill(killerHero, killerAgent, killedHero, killedAgent, agentState, blow));
            }

            if (killedHero != null)
            {
                CallHandlersForHero(killedHero, handlers
                    => handlers.GotKilled(killedHero, killedAgent, killerHero, killerAgent, agentState, blow));
            }
        }

        protected override void OnEndMission()
        {
            CallHandlersForAll((hero, handlers) => handlers.MissionOver(hero));
        }

        private const float SlowTickDuration = 2;
        private float slowTick;
            
        public override void OnMissionTick(float dt)
        {
            slowTick += dt;
            if (slowTick > SlowTickDuration)
            {
                slowTick -= SlowTickDuration;
                CallHandlersForAll((hero, handlers) => handlers.SlowTick(hero, SlowTickDuration));
            }
            CallHandlersForAll((hero, handlers) => handlers.MissionTick(hero, dt));
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

        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(Mission), "GetAttackCollisionResults",
             new[]
             {
                 typeof(Agent), typeof(Agent), typeof(GameEntity), typeof(float), typeof(AttackCollisionData),
                 typeof(MissionWeapon), typeof(bool), typeof(bool), typeof(bool), typeof(WeaponComponentData),
                 typeof(CombatLogData)
             },
             new[]
             {
                 ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref,
                 ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out,
                 ArgumentType.Out
             })]
        public static void GetAttackCollisionResultsPostfix(Mission __instance, Agent attackerAgent, Agent victimAgent, ref AttackCollisionData attackCollisionData)
        {
            Current?.ApplyHitDamage(attackerAgent, victimAgent, ref attackCollisionData);
        }

        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(Mission), "AddMissileAux")]
        private static void AddMissileAuxPrefix(bool isPrediction, Agent shooterAgent, ref WeaponData weaponData, WeaponStatsData[] weaponStatsData
            //, WeaponStatsData[] weaponStatsData, float damageBonus, ref Vec3 position, ref Vec3 direction, ref Mat3 orientation, float baseSpeed, float speed, bool addRigidBody, GameEntity gameEntityToIgnore, bool isPrimaryWeaponShot, out GameEntity missileEntity
            )
        {
            if (!isPrediction)
            {
                Current?.AddMissileAux(shooterAgent, ref weaponData, weaponStatsData);
            }
        }
        
        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(Mission), "HandleMissileCollisionReaction")]
        private static void HandleMissileCollisionReactionPrefix(Mission __instance, int missileIndex, Mission.MissileCollisionReaction collisionReaction, MatrixFrame attachLocalFrame, Agent attackerAgent, Agent attachedAgent, bool attachedToShield, sbyte attachedBoneIndex, MissionObject attachedMissionObject, Vec3 bounceBackVelocity, Vec3 bounceBackAngularVelocity, int forcedSpawnIndex)
        {
            var _missiles = (Dictionary<int, Mission.Missile>)AccessTools.Field(typeof(Mission), "_missiles").GetValue(__instance);
            Current?.MissileCollision(collisionReaction, attackerAgent, attachedAgent, attachedBoneIndex, attachedToShield, attachLocalFrame, _missiles[missileIndex]);
        }

        #endregion
    }

}