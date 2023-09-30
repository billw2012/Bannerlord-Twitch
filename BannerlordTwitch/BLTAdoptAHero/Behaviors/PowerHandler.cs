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
    /// Power application modes:
    /// - always, based on some matching criteria
    /// - only for a specific period of time, based on some matching criteria
    /// criteria are only "is it this exact agent", or "is the agent this specific hero" 

    public class PowerHandler 
    {
        public class Handlers
        {
            #region Mission Event Handler Delegates
            public delegate void AgentBuildDelegate(Agent agent);
            public delegate void MissionOverDelegate();
            public delegate void GotAKillDelegate(Agent attackerAgent, Agent victimAgent, 
                AgentState agentState, KillingBlow blow);
            public delegate void GotKilledDelegate(Agent victimAgent, Agent attackerAgent, 
                AgentState agentState, KillingBlow blow);

            public delegate void DoDamageDelegate(Agent attackerAgent, Agent victimAgent, 
                BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams);
            public delegate void TakeDamageDelegate(Agent victimAgent, Agent attackerAgent, 
                BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams);

            public delegate void DecideWeaponCollisionReactionDelegate(Agent attackerAgent, Agent victimAgent, 
                BLTHeroPowersMissionBehavior.DecideWeaponCollisionReactionParams decideWeaponCollisionReactionParams);
            
            public delegate void DoMeleeHitDelegate(Agent attackerAgent, Agent victimAgent, 
                BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams);
            public delegate void TakeMeleeHitDelegate(Agent attackerAgent, Agent victimAgent,
                BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams);
            public delegate void DoMissileHitDelegate(Agent attackerAgent,  
                Agent victimAgent, BLTHeroPowersMissionBehavior.MissileHitParams missileHitParams);
            public delegate void TakeMissileHitDelegate(Agent attackerAgent,   
                Agent victimAgent, BLTHeroPowersMissionBehavior.MissileHitParams missileHitParams);
            
            public delegate void MissionTickDelegate(float dt);
            public delegate void MissileCollisionDelegate(Mission.MissileCollisionReaction collisionReaction, 
                Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex, 
                bool attachedToShield, MatrixFrame attachLocalFrame, Mission.Missile missile);
            public delegate void AgentControllerChangedDelegate(Agent agent);

            public delegate void AddMissileDelegate(Agent shooterAgent, 
                RefHandle<WeaponData> weaponData, WeaponStatsData[] weaponStatsData);
            
            public delegate void DecideCrushedThroughDelegate(Agent attackerAgent, 
                Agent victimAgent, BLTAgentApplyDamageModel.DecideCrushedThroughParams meleeHitParams);
            
            public delegate void DecideMissileWeaponFlagsDelegate(Agent attackerAgent,
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

            public void AgentBuild(Agent agent) => OnAgentBuild?.Invoke(agent);
            public void MissionOver() => OnMissionOver?.Invoke();
            public void GotAKill(Agent attackerAgent, Agent victimAgent, AgentState agentState, KillingBlow blow) 
                => OnGotAKill?.Invoke(attackerAgent, victimAgent, agentState, blow);
            public void GotKilled(Agent victimAgent, Agent attackerAgent, AgentState agentState, KillingBlow blow) 
                => OnGotKilled?.Invoke(victimAgent, attackerAgent, agentState, blow);
            
            public void DoDamage(Agent attackerAgent, Agent victimAgent, 
                BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams) 
                => OnDoDamage?.Invoke(attackerAgent, victimAgent, blowParams);
            
            public void TakeDamage(Agent victimAgent, Agent attackerAgent, 
                BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams) 
                => OnTakeDamage?.Invoke(victimAgent, attackerAgent, blowParams);
            
            // public void DoDamage(Hero hero, Agent agent, 
            //     Hero victimHero, Agent victimAgent, RefHandle<AttackCollisionData> attackCollisionData) 
            //     => OnDoDamage?.Invoke(hero, agent, victimHero, victimAgent, attackCollisionData);
            // public void TakeDamage(Hero hero, Agent agent, 
            //     Hero attackerHero, Agent attackerAgent, RefHandle<AttackCollisionData> attackCollisionData) 
            //     => OnTakeDamage?.Invoke(hero, agent, attackerHero, attackerAgent, attackCollisionData);
            
            public void SlowTick(float dt) => OnSlowTick?.Invoke(dt);
            public void MissionTick(float dt) => OnMissionTick?.Invoke(dt);
            public void MissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction,
                Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex,
                bool attachedToShield, MatrixFrame attachLocalFrame, Mission.Missile missile) 
                => OnMissileCollision?.Invoke(collisionReaction, attackerAgent, attachedAgent,
                    attachedBoneIndex, attachedToShield, attachLocalFrame, missile);
            public void AgentControllerChanged(Agent agent) => OnAgentControllerChanged?.Invoke(agent);

            public void AddMissile(Agent attackerAgent, RefHandle<WeaponData> weaponData, WeaponStatsData[] weaponStatsData) 
                => OnAddMissile?.Invoke(attackerAgent, weaponData, weaponStatsData);

            public void DoMeleeHit(Agent attackerAgent, Agent victimAgent, BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams) 
                => OnDoMeleeHit?.Invoke(attackerAgent, victimAgent, meleeHitParams);

            public void TakeMeleeHit(Agent victimAgent, Agent attackerAgent, BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams) 
                => OnTakeMeleeHit?.Invoke(attackerAgent, victimAgent, meleeHitParams);
            
            public void PostDoMeleeHit(Agent attackerAgent, Agent victimAgent, BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams) 
                => OnPostDoMeleeHit?.Invoke(attackerAgent, victimAgent, meleeHitParams);

            public void PostTakeMeleeHit(Agent victimAgent, Agent attackerAgent, BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams) 
                => OnPostTakeMeleeHit?.Invoke(attackerAgent, victimAgent, meleeHitParams);
            
            public void DoMissileHit(Agent attackerAgent, Agent victimAgent, BLTHeroPowersMissionBehavior.MissileHitParams missileHitParams) 
                => OnDoMissileHit?.Invoke(attackerAgent, victimAgent, missileHitParams);

            public void TakeMissileHit(Agent victimAgent, Agent attackerAgent, BLTHeroPowersMissionBehavior.MissileHitParams missileHitParams) 
                => OnTakeMissileHit?.Invoke(attackerAgent, victimAgent, missileHitParams);

            public void DecideWeaponCollisionReaction(Agent attackerAgent, Agent victimAgent, BLTHeroPowersMissionBehavior.DecideWeaponCollisionReactionParams decideWeaponCollisionReactionParams)
                => OnDecideWeaponCollisionReaction?.Invoke(attackerAgent, victimAgent, decideWeaponCollisionReactionParams);
            
            public void DecideCrushedThrough(Agent attackerAgent, Agent victimAgent,
                BLTAgentApplyDamageModel.DecideCrushedThroughParams args)
            {
                OnDecideCrushedThrough?.Invoke(attackerAgent, victimAgent,
                    args);
            }

            public void DecideMissileWeaponFlags(Agent attackerAgent, BLTAgentApplyDamageModel.DecideMissileWeaponFlagsParams args)
            {
                OnDecideMissileWeaponFlags?.Invoke(attackerAgent, args);
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

        public bool CallHandlersForAgent(Agent agent, Action<Handlers> call, [CallerMemberName] string callerName = "")
        {
            var hero = agent?.IsMount == true 
                ? agent.RiderAgent?.GetAdoptedHero() 
                : agent?.GetAdoptedHero();
            
            if (hero == null) return false;

            CallHandlersForHero(hero, call, callerName);
            return true;
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
        
        public void CallHandlersForAll(Action<Handlers> call, [CallerMemberName] string callerName = "")
        {
            SafeCall(() =>
            {
                foreach (var (hero, handlersMap) in heroPowerHandlers.ToList())
                {
                    foreach (var handlers in handlersMap.ToList())
                    {
                        call(handlers.Value);
                    }
                }
            }, callerName);
        }

        public bool CallHandlersForAgentPair(Agent attackerAgent, Agent victimAgent, 
            Action<Handlers> attackerCall, Action<Handlers> victimCall = null)
        {
            var attackerHero = attackerAgent?.IsMount == true 
                ? attackerAgent.RiderAgent?.GetAdoptedHero() 
                : attackerAgent?.GetAdoptedHero();
            var victimHero = victimAgent?.IsMount == true 
                ? victimAgent.RiderAgent?.GetAdoptedHero() 
                : victimAgent?.GetAdoptedHero();

            if (attackerHero == null && victimHero == null)
                return false;

            if (attackerHero != null)
            {
                CallHandlersForHero(attackerHero, attackerCall);
            }
            if (victimCall != null && victimHero != null)
            {
                CallHandlersForHero(victimHero, victimCall);
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