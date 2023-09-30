using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch
{
    public sealed class AgentModifierConfig : IDocumentable, ICloneable
    {
        [LocDisplayName("{=JzII0KRZ}Scale Percent"), 
         LocDescription("{=fc1SmPXH}Changes the size of the target"),
         UIRange(50, 150, 5),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(1), UsedImplicitly]
        public float ScalePercent { get; set; } = 100f;
        
        [LocDisplayName("{=0vqiI434}Apply To Mount"), 
         LocDescription("{=U0wJHDSa}Apply to the mount of the target, instead of the target themselves (only some properties are valid on mounts)"), 
         PropertyOrder(2), UsedImplicitly]
        public bool ApplyToMount { get; set; }

        [LocDisplayName("{=RdCw0xo9}Properties"), 
         LocDescription("{=ZKN8fZsA}Properties to change, and how much by"), 
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(3), UsedImplicitly]
        public ObservableCollection<PropertyModifierDef> Properties { get; set; } = new();
        
        [LocDisplayName("{=iPI9zoqR}Skills"), 
         LocDescription("{=PanMmDx9}Skills to change, and how much by (these aren't compatible with Apply To Mount)"), 
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(3), UsedImplicitly]
        public ObservableCollection<SkillModifierDef> Skills { get; set; } = new();
        
        public override string ToString()
        {
            return (ScalePercent != 100 
                       ? "{=EkKFoK73}Scale {ScalePercent}%".Translate(("ScalePercent", (int)ScalePercent)) + " " 
                       : "")
                   + string.Join(" ", Properties.Select(p => p.ToString()))
                   + string.Join(" ", Skills.Select(p => p.ToString()))
                   + (ApplyToMount? " " + "{=l7w0bPt0}(on mount)".Translate() : "");
        }

        public object Clone()
        {
            return new AgentModifierConfig
            {
                ScalePercent = ScalePercent,
                ApplyToMount = ApplyToMount,
                Properties = new(Properties.Select(p => (PropertyModifierDef)p.Clone())),
                Skills = new(Skills.Select(p => (SkillModifierDef)p.Clone())),
            };
        }

        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            string mountStr = ApplyToMount ? "{=qhIXgPGK}Mount".Translate() + " " : "";
            if(ScalePercent != 100)
                generator.P(ApplyToMount 
                    ? "{qhIXgPGK}Mount {ScalePercent}% normal size".Translate(("ScalePercent", (int)ScalePercent))
                    : "{yLT1lfRC}{ScalePercent}% normal size".Translate(("ScalePercent", (int)ScalePercent))
                );
            foreach (var p in Properties)
            {
                generator.P(p.ToString());
            }
        }
    }
    
    public class BLTAgentModifierBehavior : AutoMissionBehavior<BLTAgentModifierBehavior>
    {
        private readonly Dictionary<Agent, List<AgentModifierConfig>> agentModifiersActive = new();
        private float accumulatedTime;

        public void Add(Agent agent, AgentModifierConfig config)
        {
            if (!agentModifiersActive.TryGetValue(agent, out var effects))
            {
                effects = new();
                agentModifiersActive.Add(agent, effects);
            }
            effects.Add(config);
        }

        public void Remove(Agent agent, AgentModifierConfig config)
        {
            if (agentModifiersActive.TryGetValue(agent, out var effects))
            {
                effects.Remove(config);
                if (effects.Count == 0)
                {
                    agentModifiersActive.Remove(agent);
                }
            }
        }
        
        public override void OnAgentDeleted(Agent affectedAgent)
        {
            SafeCall(() =>
            {
                agentModifiersActive.Remove(affectedAgent);
                effectedAgents.Remove(affectedAgent);
            });
        }

        private readonly Dictionary<Agent, float[]> agentDrivenPropertiesCache = new();
        private readonly Dictionary<Agent, float> agentBaseScaleCache = new();
        private readonly HashSet<Agent> effectedAgents = new();

        public override void OnMissionTick(float dt)
        {
            SafeCall(() =>
            {
                const float Interval = 2;
                accumulatedTime += dt;
                if (accumulatedTime < Interval)
                    return;

                accumulatedTime -= Interval;

                UpdateAgents();
            });
        }

        private void UpdateAgents()
        {
            // We can remove inactive agents from further processing
            foreach (var agent in agentModifiersActive
                .Where(kv => !kv.Key.IsActive())
                .ToArray())
            {
                agentModifiersActive.Remove(agent.Key);
            }

            effectedAgents.RemoveWhere(a => !a.IsActive());

            // Agents that need update is all agents with current modifications applied + all ones that have new
            // modifiers to apply

            // Group all effects by the final target agent
            var realTargetsAndModifiers = agentModifiersActive
                .SelectMany(kv
                    => kv.Value.Select(m => (
                        agent: m.ApplyToMount ? kv.Key.MountAgent : kv.Key,
                        modifier: m
                    )))
                .Where(x => x.agent != null)
                .GroupBy(x => x.agent)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(m => m.modifier).ToList()
                );

            // Make sure all previously and newly affected agents are in the list
            foreach (var r in realTargetsAndModifiers)
            {
                effectedAgents.Add(r.Key);
            }

            // Apply / update all agents (copy the agent list, so we can remove ones from the original list as we 
            // get to them)
            foreach (var agent in effectedAgents.ToList())
            {
                realTargetsAndModifiers.TryGetValue(agent, out var modifiers);

                // Restore all the base properties from the cache to start with
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
                        agent.AgentDrivenProperties.SetStat((DrivenProperty) i, initialAgentDrivenProperties[i]);
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

                // Apply modifier stack if we have one
                if (modifiers != null)
                {
                    foreach (var effect in modifiers)
                    {
                        ApplyPropertyModifiers(agent, effect);
                        newAgentScale *= effect.ScalePercent / 100f;
                    }
                }
                else
                {
                    // If we don't have a modifer stack then we can remove this agent from future processing
                    effectedAgents.Remove(agent);
                }

                if (Math.Abs(newAgentScale - agent.AgentScale) > 1E-05)
                {
                    SetAgentScale(agent, baseAgentScale, newAgentScale);
                }

                // Finally commit our modified values to the engine
                agent.UpdateCustomDrivenProperties();
            }
        }

        private static void SetAgentScale(Agent agent, float baseScale, float scale)
        {
            AgentHelpers.SetAgentScale(agent, baseScale, scale);
            
            // AccessTools.Method(typeof(Agent), "SetInitialAgentScale").Invoke(agent, new []{ (object) scale });
            // // Doesn't have any affect...
            // AgentVisualsNativeData agentVisualsNativeData = agent.Monster.FillAgentVisualsNativeData();
            // AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(agent.Character.GetStepSize() * scale / baseScale , false);
            //  animationSystemData.WalkingSpeedLimit *= scale;
            //  animationSystemData.CrouchWalkingSpeedLimit *= scale;
            // animationSystemData.NumPaces = 10;
            // agent.SetActionSet(ref agentVisualsNativeData, ref animationSystemData);
        }
        
        private static void ApplyPropertyModifiers(Agent target, AgentModifierConfig config)
        {
            if (config.Properties == null)
                return;

            foreach (var prop in config.Properties)
            {
                target.AgentDrivenProperties.SetStat(prop.Name, 
                    prop.Apply(target.AgentDrivenProperties.GetStat(prop.Name)));
            }
        }
    }
}