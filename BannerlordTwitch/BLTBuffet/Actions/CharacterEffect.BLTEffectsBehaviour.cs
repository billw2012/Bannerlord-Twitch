using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace BLTBuffet
{
    public partial class CharacterEffect
    {
        internal class CharacterEffectState
        {
            public readonly Agent agent;
            public readonly Config config;

            public float started = MBCommon.GetTime(MBCommon.TimeType.Mission);
            public class PfxState
            {
                public List<GameEntityComponent> weaponEffects;
                public BoneAttachments boneAttachments;
            }
            public readonly List<PfxState> state = new();
            public Light light;

            public CharacterEffectState(Agent agent, Config config)
            {
                this.agent = agent;
                this.config = config;
            }

            public void Apply(float dt)
            {
                if (config.HealPerSecond != 0)
                {
                    agent.Health = Math.Min(agent.HealthLimit, agent.Health + Math.Abs(config.HealPerSecond) * dt);
                }

                if(config.DamagePerSecond != 0 && !agent.Invulnerable && !Mission.DisableDying)
                {
                    var blow = new Blow(agent.Index);
                    blow.DamageType = DamageTypes.Blunt;
                    //blow.BlowFlag = BlowFlags.CrushThrough;
                    //blow.BlowFlag |= BlowFlags.KnockDown;
                    blow.BoneIndex = agent.Monster.HeadLookDirectionBoneIndex;
                    blow.Position = agent.Position;
                    blow.Position.z += agent.GetEyeGlobalHeight();
                    blow.BaseMagnitude = 0f;
                    blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
                    blow.InflictedDamage = (int) Math.Abs(config.DamagePerSecond * dt);
                    blow.SwingDirection = agent.LookDirection;
                    blow.SwingDirection.Normalize();
                    blow.Direction = blow.SwingDirection;
                    blow.DamageCalculated = true;
                    agent.RegisterBlow(blow);
                }

                if (config.ForceDropWeapons)
                {
                    var index = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                    if (index != EquipmentIndex.None)
                    {
                        agent.DropItem(index);
                    }
                    var index2 = agent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
                    if (index2 != EquipmentIndex.None)
                    {
                        agent.DropItem(index2);
                    }
                }

                // Doesn't work, doesn't something weird instead
                // if (config.ForceDismount)
                // {
                //     AccessTools.Method(typeof(Agent), "SetMountAgent").Invoke(agent, new object[] { null });
                // }

                if (config.Properties != null)
                {
                    ApplyPropertyModifiers(agent, config);
                }
            }

            public bool CheckRemove()
            {
                if (config.Duration.HasValue 
                    && MBCommon.GetTime(MBCommon.TimeType.Mission) > config.Duration.Value + started)
                {
                    Log.LogFeedEvent($"{config.Name} expired on {agent.Name}!");
                    if (!string.IsNullOrEmpty(config.DeactivateParticleEffect))
                    {
                        Mission.Current.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName(config.DeactivateParticleEffect), agent.AgentVisuals.GetGlobalFrame());
                    }
                    if (!string.IsNullOrEmpty(config.DeactivateSound))
                    {
                        Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(config.DeactivateSound), agent.AgentVisuals.GetGlobalFrame().origin, false, true, agent.Index, -1);
                    }
                    Stop();
                    return true;
                }
                return false;
            }

            public void Stop()
            {
                foreach (var s in state)
                {
                    if(s.weaponEffects != null) RemoveWeaponEffects(s.weaponEffects);
                    if(s.boneAttachments != null) RemoveAgentEffects(s.boneAttachments);
                }

                if (light != null)
                {
                    RemoveLight(agent, light);
                }
                
                agent.UpdateAgentProperties();
            }
        }

        internal class BLTEffectsBehaviour : MissionBehaviour
        {
            private readonly Dictionary<Agent, List<CharacterEffectState>> agentEffectsActive = new();

            private float accumulatedTime;

            public bool Contains(Agent agent, Config config)
            {
                return agentEffectsActive.TryGetValue(agent, out var effects)
                       && effects.Any(e => e.config.Name == config.Name);
            }

            public CharacterEffectState Add(Agent agent, Config config)
            {
                if (!agentEffectsActive.TryGetValue(agent, out var effects))
                {
                    effects = new List<CharacterEffectState>();
                    agentEffectsActive.Add(agent, effects);
                }

                var state = new CharacterEffectState(agent, config);
                effects.Add(state);
                return state;
            }

            public static BLTEffectsBehaviour Get()
            {
                var beh = Mission.Current.GetMissionBehaviour<BLTEffectsBehaviour>();
                if (beh == null)
                {
                    beh = new BLTEffectsBehaviour();
                    Mission.Current.AddMissionBehaviour(beh);
                }

                return beh;
            }

            public void ApplyHitDamage(Agent attackerAgent, Agent victimAgent,
                ref AttackCollisionData attackCollisionData)
            {
                try
                {
                    float[] hitDamageMultipliers = agentEffectsActive
                        .Where(e => ReferenceEquals(e.Key, attackerAgent))
                        .SelectMany(e => e.Value
                            .Select(f => f.config.DamageMultiplier ?? 0)
                            .Where(f => f != 0)
                        )
                        .ToArray();
                    if (hitDamageMultipliers.Any())
                    {
                        float forceMag = hitDamageMultipliers.Sum();
                        attackCollisionData.BaseMagnitude = (int) (attackCollisionData.BaseMagnitude * forceMag);
                        attackCollisionData.InflictedDamage = (int) (attackCollisionData.InflictedDamage * forceMag);
                    }
                }
                catch (Exception e)
                {
                    Log.Exception($"BLTEffectsBehaviour.ApplyHitDamage", e);
                }
            }

            // public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, int damage, in MissionWeapon affectorWeapon)
            // {
            //     float[] knockBackForces = agentEffectsActive
            //         .Where(e => ReferenceEquals(e.Key, affectorAgent))
            //         .SelectMany(e => e.Value
            //             .Select(f => f.config.DamageMultiplier ?? 0)
            //             .Where(f => f != 0)
            //         )
            //         .ToArray();
            //     if (knockBackForces.Any())
            //     {
            //         var direction = (affectedAgent.Frame.origin - affectorAgent.Frame.origin).NormalizedCopy();
            //         var force = knockBackForces.Select(f => direction * f).Aggregate((a, b) => a + b);
            //         affectedAgent.AgentVisuals.SetAgentLocalSpeed(force.AsVec2);
            //         //var entity = affectedAgent.AgentVisuals.GetEntity();
            //         // // entity.ActivateRagdoll();
            //         // entity.AddPhysics(0.1f, Vec3.Zero, null, force * 2000, Vec3.Zero, PhysicsMaterial.GetFromIndex(0), false, 0);
            //
            //         // entity.EnableDynamicBody();
            //         // entity.SetPhysicsState(true, true);
            //
            //         //entity.ApplyImpulseToDynamicBody(entity.GetGlobalFrame().origin, force);
            //         // foreach (float knockBackForce in knockBackForces)
            //         // {
            //         //     entity.ApplyImpulseToDynamicBody(entity.GetGlobalFrame().origin, direction * knockBackForce);
            //         // }
            //         // Mission.Current.AddTimerToDynamicEntity(entity, 3f + MBRandom.RandomFloat * 2f);
            //     }
            // }

            public override void OnAgentDeleted(Agent affectedAgent)
            {
                try
                {
                    if (agentEffectsActive.TryGetValue(affectedAgent, out var effectStates))
                    {
                        foreach (var e in effectStates)
                        {
                            e.Stop();
                        }

                        agentEffectsActive.Remove(affectedAgent);
                    }
                }
                catch (Exception e)
                {
                    Log.Exception($"BLTEffectsBehaviour.OnAgentDeleted", e);
                }
            }

            private readonly Dictionary<Agent, float[]> agentDrivenPropertiesCache = new();
            private readonly Dictionary<Agent, float> agentBaseScaleCache = new();

            public override void OnMissionTick(float dt)
            {
                base.OnMissionTick(dt);

                try
                {
                    foreach (var agent in agentEffectsActive
                        .Where(kv => !kv.Key.IsActive())
                        .ToArray())
                    {
                        // foreach (var effect in agent.Value.ToList())
                        // {
                        //     effect.Stop();
                        // }
                        agentEffectsActive.Remove(agent.Key);
                    }

                    const float Interval = 2;
                    accumulatedTime += dt;
                    if (accumulatedTime < Interval)
                        return;

                    accumulatedTime -= Interval;
                    foreach (var agentEffects in agentEffectsActive.ToArray())
                    {
                        var agent = agentEffects.Key;
                        // Restore all the properties from the cache to start with
                        if (!agentDrivenPropertiesCache.TryGetValue(agent, out float[] initialAgentDrivenProperties))
                        {
                            initialAgentDrivenProperties = new float[(int) DrivenProperty.Count];
                            for (int i = 0; i < (int) DrivenProperty.Count; i++)
                            {
                                initialAgentDrivenProperties[i] =
                                    agent.AgentDrivenProperties.GetStat((DrivenProperty) i);
                            }

                            agentDrivenPropertiesCache.Add(agent, initialAgentDrivenProperties);
                        }
                        else
                        {
                            for (int i = 0; i < (int) DrivenProperty.Count; i++)
                            {
                                agent.AgentDrivenProperties.SetStat((DrivenProperty) i,
                                    initialAgentDrivenProperties[i]);
                            }
                        }

                        if (!agentBaseScaleCache.TryGetValue(agent, out float baseAgentScale))
                        {
                            agentBaseScaleCache.Add(agent, agent.AgentScale);
                            baseAgentScale = agent.AgentScale;
                        }

                        float newAgentScale = baseAgentScale;
                        // Now update the dynamic properties
                        agent.UpdateAgentProperties();
                        // Then apply our effects as a stack
                        foreach (var effect in agentEffects.Value.ToList())
                        {
                            effect.Apply(Interval);
                            if (effect.CheckRemove())
                            {
                                agentEffects.Value.Remove(effect);
                            }

                            newAgentScale *= effect.config.Scale ?? 1;
                        }

                        if (newAgentScale != agent.AgentScale)
                        {
                            SetAgentScale(agent, baseAgentScale, newAgentScale);
                        }

                        // Finally commit our modified values to the engine
                        agent.UpdateCustomDrivenProperties();
                    }
                }
                catch (Exception e)
                {
                    Log.Exception($"BLTEffectsBehaviour.OnMissionTick", e);
                }
            }

            public override MissionBehaviourType BehaviourType => MissionBehaviourType.Other;
        }
    }
}