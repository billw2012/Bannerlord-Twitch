using System;
using System.Runtime.CompilerServices;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Powers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    public class BLTHeroPowersMissionBehavior : AutoMissionBehavior<BLTHeroPowersMissionBehavior>
    {
        private void ApplyPassivePower(Agent agent, Action<Hero, IHeroPowerPassive> apply, [CallerMemberName] string callerName = "")
        {
            try
            {
                var hero = agent.GetAdoptedHero();
                if (hero == null)
                {
                    return;
                }
                apply(hero, hero.GetClass());
            }
            catch (Exception ex)
            {
                Log.Exception($"{nameof(BLTHeroPowersMissionBehavior)}.{callerName}", ex);
            }
        }
        
        public override void OnAgentBuild(Agent agent, Banner banner) 
            => ApplyPassivePower(agent, (hero, passive) => passive.OnAgentBuild(hero, agent));

        public void ApplyHitDamage(Agent attackerAgent, Agent victimAgent,
            ref AttackCollisionData attackCollisionData)
        {
            try
            {
                var attackerHero = attackerAgent?.GetAdoptedHero();
                var victimHero = victimAgent?.GetAdoptedHero();
                (attackerHero?.GetClass() as IHeroPowerPassive)?.OnDoDamage(attackerHero, attackerAgent, victimHero, victimAgent, ref attackCollisionData);
                (victimHero?.GetClass() as IHeroPowerPassive)?.OnTakeDamage(victimHero, victimAgent, attackerHero, attackerAgent, ref attackCollisionData);
            }
            catch (Exception ex)
            {
                Log.Exception($"{nameof(BLTHeroPowersMissionBehavior)}.{nameof(ApplyHitDamage)}", ex);
            }
            
            // try
            // {
            //     float[] hitDamageMultipliers = agentEffectsActive
            //         .Where(e => ReferenceEquals(e.Key, attackerAgent))
            //         .SelectMany(e => e.Value
            //             .Select(f => f.config.DamageMultiplier ?? 0)
            //             .Where(f => f != 0)
            //         )
            //         .ToArray();
            //     if (hitDamageMultipliers.Any())
            //     {
            //         float forceMag = hitDamageMultipliers.Sum();
            //         attackCollisionData.BaseMagnitude = (int) (attackCollisionData.BaseMagnitude * forceMag);
            //         attackCollisionData.InflictedDamage = (int) (attackCollisionData.InflictedDamage * forceMag);
            //     }
            // }
            // catch (Exception e)
            // {
            //     Log.Exception($"BLTEffectsBehaviour.ApplyHitDamage", e);
            // }
        }
        
        public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled)
        {
            base.OnMissileHit(attacker, victim, isCanceled);
        }

        public override void OnMissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction, 
            Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex)
        {
            base.OnMissileCollisionReaction(collisionReaction, attackerAgent, attachedAgent, attachedBoneIndex);
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, 
            int damage, in MissionWeapon affectorWeapon)
        {
            base.OnAgentHit(affectedAgent, affectorAgent, damage, in affectorWeapon);
        }

        public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, 
            WeaponComponentData attackerWeapon, bool isBlocked,
            float damage, float movementSpeedDamageModifier, float hitDistance, AgentAttackType attackType,
            float shotDifficulty, BoneBodyPartType victimHitBodyPart)
        {
            base.OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, damage, movementSpeedDamageModifier, hitDistance, attackType, shotDifficulty, victimHitBodyPart);
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
        }

        public override void OnAgentMount(Agent agent)
        {
            base.OnAgentMount(agent);
        }

        public override void OnAgentDismount(Agent agent)
        {
            base.OnAgentDismount(agent);
        }

        public override void OnRegisterBlow(Agent attacker, Agent victim, GameEntity realHitEntity, Blow b,
            ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon)
        {
            base.OnRegisterBlow(attacker, victim, realHitEntity, b, ref collisionData, in attackerWeapon);
        }

        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, 
            Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        {
            base.OnAgentShootMissile(shooterAgent, weaponIndex, position, velocity, orientation, hasRigidBody, forcedMissileIndex);
        }
    }
}