using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Powers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    public class PowerHandler 
    {
        
        public class Handlers
        {
            #region Mission Event Handler Delegates
            public delegate void AgentBuildDelegate(Hero hero, Agent agent);
            public delegate void MissionOverDelegate(Hero hero);
            public delegate void GotAKillDelegate(Hero attackerHero, Agent attackerAgent, Hero victimHero, Agent victimAgent, 
                AgentState agentState, KillingBlow blow);
            public delegate void GotKilledDelegate(Hero victimHero, Agent victimAgent, Hero attackerHero, Agent attackerAgent, 
                AgentState agentState, KillingBlow blow);

            public delegate void DoDamageDelegate(Hero attackerHero, Agent attackerAgent, Hero victimHero, Agent victimAgent, 
                BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams);
            public delegate void TakeDamageDelegate(Hero victimHero, Agent victimAgent, Hero attackerHero, Agent attackerAgent, 
                BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams);

            public delegate void DecideWeaponCollisionReactionDelegate(Hero attackerHero, Agent attackerAgent, Hero victimHero,
                Agent victimAgent, BLTHeroPowersMissionBehavior.DecideWeaponCollisionReactionParams decideWeaponCollisionReactionParams);
            
            public delegate void DoMeleeHitDelegate(Hero attackerHero, Agent attackerAgent, Hero victimHero,
                Agent victimAgent, BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams);
            public delegate void TakeMeleeHitDelegate(Hero attackerHero, Agent attackerAgent, Hero victimHero,
                Agent victimAgent, BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams);
            public delegate void DoMissileHitDelegate(Hero attackerHero, Agent attackerAgent, Hero victimHero,
                Agent victimAgent, BLTHeroPowersMissionBehavior.MissileHitParams missileHitParams);
            public delegate void TakeMissileHitDelegate(Hero attackerHero, Agent attackerAgent, Hero victimHero,
                Agent victimAgent, BLTHeroPowersMissionBehavior.MissileHitParams missileHitParams);
            
            public delegate void MissionTickDelegate(Hero hero, float dt);
            public delegate void MissileCollisionDelegate(Mission.MissileCollisionReaction collisionReaction, 
                Hero attackerHero, Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex, 
                bool attachedToShield, MatrixFrame attachLocalFrame, Mission.Missile missile);
            public delegate void AgentControllerChangedDelegate(Hero hero, Agent agent);

            public delegate void AddMissileDelegate(Hero shooterHero, Agent shooterAgent, 
                RefHandle<WeaponData> weaponData, WeaponStatsData[] weaponStatsData);
            
            public delegate void DecideCrushedThroughDelegate(Hero attackerHero, Agent attackerAgent, Hero victimHero,
                Agent victimAgent, BLTAgentApplyDamageModel.DecideCrushedThroughParams meleeHitParams);
            
            public delegate void DecideMissileWeaponFlagsDelegate(Hero attackerHero, Agent attackerAgent,
                BLTAgentApplyDamageModel.DecideMissileWeaponFlagsParams args);

            #endregion
            
            public event AgentBuildDelegate OnAgentBuild;
            public event MissionOverDelegate OnMissionOver;
            public event GotAKillDelegate OnGotAKill;
            public event GotKilledDelegate OnGotKilled;
            public event DoDamageDelegate OnDoDamage;
            public event TakeDamageDelegate OnTakeDamage;
            public event DecideWeaponCollisionReactionDelegate OnDecideWeaponCollisionReaction;
            public event DoMeleeHitDelegate OnDoMeleeHit;
            public event TakeMeleeHitDelegate OnTakeMeleeHit;
            public event DoMeleeHitDelegate OnPostDoMeleeHit;
            public event TakeMeleeHitDelegate OnPostTakeMeleeHit;
            public event DoMissileHitDelegate OnDoMissileHit;
            public event TakeMissileHitDelegate OnTakeMissileHit;
            public event MissionTickDelegate OnSlowTick;
            public event MissionTickDelegate OnMissionTick;
            public event MissileCollisionDelegate OnMissileCollision;
            public event AgentControllerChangedDelegate OnAgentControllerChanged;
            public event AddMissileDelegate OnAddMissile;
            public event DecideCrushedThroughDelegate OnDecideCrushedThrough;
            public event DecideMissileWeaponFlagsDelegate OnDecideMissileWeaponFlags;

            public void AgentBuild(Hero hero, Agent agent) => OnAgentBuild?.Invoke(hero, agent);
            public void MissionOver(Hero hero) => OnMissionOver?.Invoke(hero);
            public void GotAKill(Hero attackerHero, Agent attackerAgent, Hero victimHero, Agent victimAgent, AgentState agentState, KillingBlow blow) 
                => OnGotAKill?.Invoke(attackerHero, attackerAgent, victimHero, victimAgent, agentState, blow);
            public void GotKilled(Hero victimHero, Agent victimAgent, Hero attackerHero, Agent attackerAgent, AgentState agentState, KillingBlow blow) 
                => OnGotKilled?.Invoke(victimHero, victimAgent, attackerHero, attackerAgent, agentState, blow);
            
            public void DoDamage(Hero attackerHero, Agent attackerAgent, Hero victimHero, Agent victimAgent, 
                BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams) 
                => OnDoDamage?.Invoke(attackerHero, attackerAgent, victimHero, victimAgent, blowParams);
            
            public void TakeDamage(Hero victimHero, Agent victimAgent, Hero attackerHero, Agent attackerAgent, 
                BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams) 
                => OnTakeDamage?.Invoke(victimHero, victimAgent, attackerHero, attackerAgent, blowParams);
            
            // public void DoDamage(Hero hero, Agent agent, 
            //     Hero victimHero, Agent victimAgent, RefHandle<AttackCollisionData> attackCollisionData) 
            //     => OnDoDamage?.Invoke(hero, agent, victimHero, victimAgent, attackCollisionData);
            // public void TakeDamage(Hero hero, Agent agent, 
            //     Hero attackerHero, Agent attackerAgent, RefHandle<AttackCollisionData> attackCollisionData) 
            //     => OnTakeDamage?.Invoke(hero, agent, attackerHero, attackerAgent, attackCollisionData);
            
            public void SlowTick(Hero hero, float dt) => OnSlowTick?.Invoke(hero, dt);
            public void MissionTick(Hero hero, float dt) => OnMissionTick?.Invoke(hero, dt);
            public void MissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction,
                Hero attackerHero, Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex,
                bool attachedToShield, MatrixFrame attachLocalFrame, Mission.Missile missile) 
                => OnMissileCollision?.Invoke(collisionReaction, attackerHero, attackerAgent, attachedAgent,
                    attachedBoneIndex, attachedToShield, attachLocalFrame, missile);
            public void AgentControllerChanged(Hero hero, Agent agent) => OnAgentControllerChanged?.Invoke(hero, agent);

            public void AddMissile(Hero attackerHero, Agent attackerAgent, RefHandle<WeaponData> weaponData, WeaponStatsData[] weaponStatsData) 
                => OnAddMissile?.Invoke(attackerHero, attackerAgent, weaponData, weaponStatsData);

            public void DoMeleeHit(Hero attackerHero, Agent attackerAgent, Hero victimHero, Agent victimAgent, BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams) 
                => OnDoMeleeHit?.Invoke(attackerHero, attackerAgent, victimHero, victimAgent, meleeHitParams);

            public void TakeMeleeHit(Hero victimHero, Agent victimAgent, Hero attackerHero, Agent attackerAgent, BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams) 
                => OnTakeMeleeHit?.Invoke(attackerHero, attackerAgent, victimHero, victimAgent, meleeHitParams);
            
            public void PostDoMeleeHit(Hero attackerHero, Agent attackerAgent, Hero victimHero, Agent victimAgent, BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams) 
                => OnPostDoMeleeHit?.Invoke(attackerHero, attackerAgent, victimHero, victimAgent, meleeHitParams);

            public void PostTakeMeleeHit(Hero victimHero, Agent victimAgent, Hero attackerHero, Agent attackerAgent, BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams) 
                => OnPostTakeMeleeHit?.Invoke(attackerHero, attackerAgent, victimHero, victimAgent, meleeHitParams);
            
            public void DoMissileHit(Hero attackerHero, Agent attackerAgent, Hero victimHero, Agent victimAgent, BLTHeroPowersMissionBehavior.MissileHitParams missileHitParams) 
                => OnDoMissileHit?.Invoke(attackerHero, attackerAgent, victimHero, victimAgent, missileHitParams);

            public void TakeMissileHit(Hero victimHero, Agent victimAgent, Hero attackerHero, Agent attackerAgent, BLTHeroPowersMissionBehavior.MissileHitParams missileHitParams) 
                => OnTakeMissileHit?.Invoke(attackerHero, attackerAgent, victimHero, victimAgent, missileHitParams);

            public void DecideWeaponCollisionReaction(Hero attackerHero, Agent attackerAgent, Hero victimHero, Agent victimAgent, BLTHeroPowersMissionBehavior.DecideWeaponCollisionReactionParams decideWeaponCollisionReactionParams)
                => OnDecideWeaponCollisionReaction?.Invoke(attackerHero, attackerAgent, victimHero, victimAgent, decideWeaponCollisionReactionParams);
            
            public void DecideCrushedThrough(Hero attackerHero, Agent attackerAgent, Hero victimHero, Agent victimAgent,
                BLTAgentApplyDamageModel.DecideCrushedThroughParams args)
            {
                OnDecideCrushedThrough?.Invoke(attackerHero, attackerAgent, victimHero, victimAgent,
                    args);
            }

            public void DecideMissileWeaponFlags(Hero attackerHero, Agent attackerAgent, BLTAgentApplyDamageModel.DecideMissileWeaponFlagsParams args)
            {
                OnDecideMissileWeaponFlags?.Invoke(attackerHero, attackerAgent, args);
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

        public bool CallHandlersForAgent(Agent agent, Action<Hero, Handlers> call, [CallerMemberName] string callerName = "")
        {
            var hero = agent?.GetAdoptedHero();
            if (hero == null) return false;
            CallHandlersForHero(hero, handlers => call(hero, handlers), callerName);
            return true;
        }

        public void CallHandlersForHero(Hero hero, Action<Handlers> call, [CallerMemberName] string callerName = "")
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
        
        public void CallHandlersForAll(Action<Hero, Handlers> call, [CallerMemberName] string callerName = "")
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

        public delegate void HeroPairCallbackDelegate(Handlers handlers, Hero attackerHero, Hero victimHero); 
        public bool CallHandlersForAgentPair(Agent attackerAgent, Agent victimAgent, 
            HeroPairCallbackDelegate attackerCall, HeroPairCallbackDelegate victimCall)
        {
            var attackerHero = attackerAgent?.GetAdoptedHero();
            var victimHero = victimAgent?.GetAdoptedHero();

            if (attackerHero == null && victimHero == null)
                return false;

            if (attackerHero != null)
            {
                CallHandlersForHero(attackerHero, handlers => attackerCall(handlers, attackerHero, victimHero));
            }
            if (victimHero != null)
            {
                CallHandlersForHero(victimHero, handlers => victimCall(handlers, attackerHero, victimHero));
            }
            return true;
        }
        
        private void SafeCall(Action a, [CallerMemberName]string fnName = "")
        {
            try
            {
                a();
            }
            catch (Exception e)
            {
                Log.Exception($"{GetType().Name}.{fnName}", e);
            }
        }
    }
}