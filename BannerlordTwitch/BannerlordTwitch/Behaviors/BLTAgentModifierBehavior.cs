using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch.Util;
using dnlib.DotNet;
using HarmonyLib;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch
{
    public class PropertyDef
    {
        [Description("The property to modify"), PropertyOrder(1), UsedImplicitly]
        public DrivenProperty Name { get; set; }

        [Description("Add to the property value"), PropertyOrder(2), UsedImplicitly]
        public float? Add { get; set; }

        [Description("Multiply the property value"), PropertyOrder(3), UsedImplicitly]
        public float? Multiply { get; set; }

        public override string ToString()
        {
            var parts = new List<string> {Name.ToString()};
            if (Multiply.HasValue && Multiply.Value != 0)
            {
                parts.Add($"* {Multiply}");
            }

            if (Add.HasValue && Add.Value != 0)
            {
                parts.Add(Add > 0 ? $"+ {Add}" : $"{Add}");
            }

            return string.Join(" ", parts);
        }
    }
    
    public class AgentModifierConfig
    {
        [Description("Scaling of the target"), PropertyOrder(1), UsedImplicitly]
        public float? Scale { get; set; }

        [Description("Properties to change, and how much by"), PropertyOrder(2), UsedImplicitly]
        public List<PropertyDef> Properties { get; set; } = new();

        public override string ToString()
        {
            string result = Scale.HasValue && Scale.Value != 1 ? $"Scale {Scale.Value} " : "";
            return result + string.Join(" ", Properties.Select(p => p.ToString()));
        }
    }
    
    public class AgentModifierState
    {
        public Agent Agent { get; }
        public AgentModifierConfig Config { get; }

        internal AgentModifierState(Agent agent, AgentModifierConfig config)
        {
            Agent = agent;
            Config = config;
        }

        internal void Stop()
        {
            Agent.UpdateAgentProperties();
        }

        internal void Apply()
        {
            if (Config.Properties != null)
            {
                ApplyPropertyModifiers(Agent, Config);
            }
        }

        private static void ApplyPropertyModifiers(Agent target, AgentModifierConfig config)
        {
            foreach (var prop in config.Properties)
            {
                float baseValue = target.AgentDrivenProperties.GetStat(prop.Name);
                if (prop.Multiply.HasValue)
                    baseValue *= prop.Multiply.Value;
                if (prop.Add.HasValue)
                    baseValue += prop.Add.Value;
                target.AgentDrivenProperties.SetStat(prop.Name, baseValue);
            }
            // target.UpdateCustomDrivenProperties();
        }
    }

    public class BLTAgentModifierBehavior : AutoMissionBehavior<BLTAgentModifierBehavior>
    {
        private readonly Dictionary<Agent, List<AgentModifierState>> agentModifiersActive = new();

        private float accumulatedTime;

        // public bool Contains(Agent agent, AgentEffectConfig config)
        // {
        //     return agentEffectsActive.TryGetValue(agent, out var effects)
        //            && effects.Any(e => e.Config.Name == config.Name);
        // }

        public AgentModifierState Add(Agent agent, AgentModifierConfig config)
        {
            if (!agentModifiersActive.TryGetValue(agent, out var effects))
            {
                effects = new();
                agentModifiersActive.Add(agent, effects);
            }

            var state = new AgentModifierState(agent, config);
            effects.Add(state);
            return state;
        }

        public void Remove(AgentModifierState effectState)
        {
            if (agentModifiersActive.TryGetValue(effectState.Agent, out var effects))
            {
                effectState.Stop();
                effects.Remove(effectState);
            }
        }

        // public void ApplyHitDamage(Agent attackerAgent, Agent victimAgent,
        //     ref AttackCollisionData attackCollisionData)
        // {
        //     try
        //     {
        //         float[] hitDamageMultipliers = agentEffectsActive
        //             .Where(e => ReferenceEquals(e.Key, attackerAgent))
        //             .SelectMany(e => e.Value
        //                 .Select(f => f.Config.DamageMultiplier ?? 0)
        //                 .Where(f => f != 0)
        //             )
        //             .ToArray();
        //         if (hitDamageMultipliers.Any())
        //         {
        //             float forceMag = hitDamageMultipliers.Sum();
        //             attackCollisionData.BaseMagnitude = (int) (attackCollisionData.BaseMagnitude * forceMag);
        //             attackCollisionData.InflictedDamage = (int) (attackCollisionData.InflictedDamage * forceMag);
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         Log.Exception($"BLTEffectsBehaviour.ApplyHitDamage", e);
        //     }
        // }

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
                if (agentModifiersActive.TryGetValue(affectedAgent, out var effectStates))
                {
                    foreach (var e in effectStates)
                    {
                        e.Stop();
                    }

                    agentModifiersActive.Remove(affectedAgent);
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
                foreach (var agent in agentModifiersActive
                    .Where(kv => !kv.Key.IsActive())
                    .ToArray())
                {
                    // foreach (var effect in agent.Value.ToList())
                    // {
                    //     effect.Stop();
                    // }
                    agentModifiersActive.Remove(agent.Key);
                }

                const float Interval = 2;
                accumulatedTime += dt;
                if (accumulatedTime < Interval)
                    return;

                accumulatedTime -= Interval;
                foreach (var agentEffects in agentModifiersActive.ToArray())
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
                        effect.Apply();
                        // if (effect.CheckRemove())
                        // {
                        //     agentEffects.Value.Remove(effect);
                        // }

                        newAgentScale *= effect.Config.Scale ?? 1;
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

        private static void SetAgentScale(Agent agent, float baseScale, float scale)
        {
            AccessTools.Method(typeof(Agent), "SetInitialAgentScale").Invoke(agent, new []{ (object) scale });
            // Doesn't have any affect...
            //AgentVisualsNativeData agentVisualsNativeData = agent.Monster.FillAgentVisualsNativeData();
            //AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(agent.Character.GetStepSize() * scale / baseScale , false);
            // animationSystemData.WalkingSpeedLimit *= scale;
            // animationSystemData.CrouchWalkingSpeedLimit *= scale;
            //animationSystemData.NumPaces = 10;
            //agent.SetActionSet(ref agentVisualsNativeData, ref animationSystemData);
        }
    }
}