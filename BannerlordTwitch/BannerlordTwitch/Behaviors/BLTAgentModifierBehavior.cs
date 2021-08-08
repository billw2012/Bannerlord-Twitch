using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch
{
    public class PropertyDef : ICloneable, INotifyPropertyChanged
    {
        public class DrivenPropertyItemSource : IItemsSource
        {
            public ItemCollection GetValues() => new() {
                { DrivenProperty.ArmorHead, "Armor Head" }, 
                { DrivenProperty.ArmorTorso, "Armor Torso" }, 
                { DrivenProperty.ArmorLegs, "Armor Legs" }, 
                { DrivenProperty.ArmorArms, "Armor Arms" },

                { DrivenProperty.ArmorEncumbrance, "Armor Encumbrance" },
                { DrivenProperty.WeaponsEncumbrance, "Weapons Encumbrance" }, 

                { DrivenProperty.TopSpeedReachDuration, "Top Speed Reach Duration" },
                { DrivenProperty.MaxSpeedMultiplier, "Max Speed Multiplier" }, 
                { DrivenProperty.CombatMaxSpeedMultiplier, "Combat Max Speed Multiplier" },

                { DrivenProperty.MountChargeDamage, "Mount Charge Damage" }, 
                { DrivenProperty.MountDifficulty, "Mount Difficulty" }, 
                { DrivenProperty.MountManeuver, "Mount Maneuver" }, 
                { DrivenProperty.MountSpeed, "Mount Speed" }, 
                { DrivenProperty.AttributeHorseArchery, "Horse Archery Skills" }, 
                
                { DrivenProperty.AttributeRiding, "Riding Skills" }, 
                { DrivenProperty.AttributeShield, "Shield Skills" }, 
                { DrivenProperty.HandlingMultiplier, "Handling Multiplier" }, 
                { DrivenProperty.WeaponInaccuracy, "Weapon Inaccuracy" }, 
                
                { DrivenProperty.SwingSpeedMultiplier, "Swing Speed Multiplier" },
                { DrivenProperty.ReloadSpeed, "Reload Speed" },
                { DrivenProperty.ReloadMovementPenaltyFactor, "Reload Movement Penalty" },
                
                { DrivenProperty.AttributeCourage, "Courage" }, 

                { DrivenProperty.AttributeShieldMissileCollisionBodySizeAdder, "Shield Missile Collision Body Size Adder" }, 
                
                { DrivenProperty.ThrustOrRangedReadySpeedMultiplier, "Thrust Or Ranged Ready Speed Multiplier" },

                { DrivenProperty.AiShootFreq, "AI Shoot Frequency" },
                { DrivenProperty.AiWaitBeforeShootFactor, "AI Wait Before Shoot Factor" },
                { DrivenProperty.AIBlockOnDecideAbility, "AI Block Chance" },
                { DrivenProperty.AIAttackOnDecideChance, "AI Attack Chance" },
                { DrivenProperty.AiKick, "AI Kick" }, 
                
                { DrivenProperty.UseRealisticBlocking, "UseRealisticBlocking" },
                { DrivenProperty.WeaponWorstMobileAccuracyPenalty, "WeaponWorstMobileAccuracyPenalty" }, 
                { DrivenProperty.WeaponWorstUnsteadyAccuracyPenalty, "WeaponWorstUnsteadyAccuracyPenalty" }, 
                { DrivenProperty.WeaponBestAccuracyWaitTime, "WeaponBestAccuracyWaitTime" }, 
                { DrivenProperty.WeaponUnsteadyBeginTime, "WeaponUnsteadyBeginTime" }, 
                { DrivenProperty.WeaponUnsteadyEndTime, "WeaponUnsteadyEndTime" }, 
                { DrivenProperty.WeaponRotationalAccuracyPenaltyInRadians, "WeaponRotationalAccuracyPenaltyInRadians" }, 
                { DrivenProperty.LongestRangedWeaponSlotIndex, "LongestRangedWeaponSlotIndex" }, 
                { DrivenProperty.LongestRangedWeaponInaccuracy, "LongestRangedWeaponInaccuracy" }, 
                { DrivenProperty.ShieldBashStunDurationMultiplier, "ShieldBashStunDurationMultiplier" }, 
                { DrivenProperty.KickStunDurationMultiplier, "KickStunDurationMultiplier" }, 
                { DrivenProperty.BipedalRangedReadySpeedMultiplier, "BipedalRangedReadySpeedMultiplier" }, 
                { DrivenProperty.BipedalRangedReloadSpeedMultiplier, "BipedalRangedReloadSpeedMultiplier" }, 

                { DrivenProperty.AiRangedHorsebackMissileRange, "AiRangedHorsebackMissileRange" }, 
                { DrivenProperty.AiFacingMissileWatch, "AiFacingMissileWatch" }, 
                { DrivenProperty.AiFlyingMissileCheckRadius, "AiFlyingMissileCheckRadius" }, 
                { DrivenProperty.AIParryOnDecideAbility, "AIParryOnDecideAbility" }, 
                { DrivenProperty.AiTryChamberAttackOnDecide, "AiTryChamberAttackOnDecide" }, 
                { DrivenProperty.AIAttackOnParryChance, "AIAttackOnParryChance" }, 
                { DrivenProperty.AiAttackOnParryTiming, "AiAttackOnParryTiming" }, 
                { DrivenProperty.AIDecideOnAttackChance, "AIDecideOnAttackChance" }, 
                { DrivenProperty.AIParryOnAttackAbility, "AIParryOnAttackAbility" }, 
                { DrivenProperty.AiAttackCalculationMaxTimeFactor, "AiAttackCalculationMaxTimeFactor" }, 
                { DrivenProperty.AiDecideOnAttackWhenReceiveHitTiming, "AiDecideOnAttackWhenReceiveHitTiming" }, 
                { DrivenProperty.AiDecideOnAttackContinueAction, "AiDecideOnAttackContinueAction" }, 
                { DrivenProperty.AiDecideOnAttackingContinue, "AiDecideOnAttackingContinue" }, 
                { DrivenProperty.AIParryOnAttackingContinueAbility, "AIParryOnAttackingContinueAbility" }, 
                { DrivenProperty.AIDecideOnRealizeEnemyBlockingAttackAbility, "AIDecideOnRealizeEnemyBlockingAttackAbility" }, 
                { DrivenProperty.AIRealizeBlockingFromIncorrectSideAbility, "AIRealizeBlockingFromIncorrectSideAbility" }, 
                { DrivenProperty.AiAttackingShieldDefenseChance, "AiAttackingShieldDefenseChance" }, 
                { DrivenProperty.AiAttackingShieldDefenseTimer, "AiAttackingShieldDefenseTimer" }, 
                { DrivenProperty.AiCheckMovementIntervalFactor, "AiCheckMovementIntervalFactor" }, 
                { DrivenProperty.AiMovemetDelayFactor, "AiMovemetDelayFactor" }, 
                { DrivenProperty.AiParryDecisionChangeValue, "AiParryDecisionChangeValue" }, 
                { DrivenProperty.AiDefendWithShieldDecisionChanceValue, "AiDefendWithShieldDecisionChanceValue" }, 
                { DrivenProperty.AiMoveEnemySideTimeValue, "AiMoveEnemySideTimeValue" }, 
                { DrivenProperty.AiMinimumDistanceToContinueFactor, "AiMinimumDistanceToContinueFactor" }, 
                { DrivenProperty.AiStandGroundTimerValue, "AiStandGroundTimerValue" }, 
                { DrivenProperty.AiStandGroundTimerMoveAlongValue, "AiStandGroundTimerMoveAlongValue" }, 
                { DrivenProperty.AiHearingDistanceFactor, "AiHearingDistanceFactor" }, 
                { DrivenProperty.AiChargeHorsebackTargetDistFactor, "AiChargeHorsebackTargetDistFactor" }, 
                { DrivenProperty.AiRangerLeadErrorMin, "AiRangerLeadErrorMin" }, 
                { DrivenProperty.AiRangerLeadErrorMax, "AiRangerLeadErrorMax" }, 
                { DrivenProperty.AiRangerVerticalErrorMultiplier, "AiRangerVerticalErrorMultiplier" }, 
                { DrivenProperty.AiRangerHorizontalErrorMultiplier, "AiRangerHorizontalErrorMultiplier" }, 
                { DrivenProperty.AiRaiseShieldDelayTimeBase, "AiRaiseShieldDelayTimeBase" }, 
                { DrivenProperty.AiUseShieldAgainstEnemyMissileProbability, "AiUseShieldAgainstEnemyMissileProbability" }, 
                { DrivenProperty.AiSpeciesIndex, "AiSpeciesIndex" }, 
                { DrivenProperty.AiRandomizedDefendDirectionChance, "AiRandomizedDefendDirectionChance" }, 
                { DrivenProperty.AiShooterError, "AiShooterError" }, 
                { DrivenProperty.AISetNoAttackTimerAfterBeingHitAbility, "AISetNoAttackTimerAfterBeingHitAbility" }, 
                { DrivenProperty.AISetNoAttackTimerAfterBeingParriedAbility, "AISetNoAttackTimerAfterBeingParriedAbility" }, 
                { DrivenProperty.AISetNoDefendTimerAfterHittingAbility, "AISetNoDefendTimerAfterHittingAbility" }, 
                { DrivenProperty.AISetNoDefendTimerAfterParryingAbility, "AISetNoDefendTimerAfterParryingAbility" }, 
                { DrivenProperty.AIEstimateStunDurationPrecision, "AIEstimateStunDurationPrecision" }, 
                { DrivenProperty.AIHoldingReadyMaxDuration, "AIHoldingReadyMaxDuration" }, 
                { DrivenProperty.AIHoldingReadyVariationPercentage, "AIHoldingReadyVariationPercentage" }, 
            };
        }
        
        [Description("The property to modify"),
         ItemsSource(typeof(DrivenPropertyItemSource)),
         PropertyOrder(1), UsedImplicitly]
        public DrivenProperty Name { get; set; }

        [Description("Add to the property value"), PropertyOrder(2), UsedImplicitly]
        public float? Add { get; set; }

        [Description("Multiply the property value"), PropertyOrder(3), UsedImplicitly]
        public float? Multiply { get; set; }

        public override string ToString()
        {
            var parts = new List<string> {Name.ToString().SplitCamelCase()};
            if (Multiply.HasValue && Multiply.Value != 0)
            {
                parts.Add($"{Multiply * 100:0}%");
            }

            if (Add.HasValue && Add.Value != 0)
            {
                parts.Add(Add > 0 ? $"+{Add}" : $"{Add}");
            }

            return string.Join(" ", parts);
        }

        public object Clone() => CloneHelpers.CloneProperties(this);
        public event PropertyChangedEventHandler PropertyChanged;
    }
    
    public sealed class AgentModifierConfig : IDocumentable, ICloneable
    {
        [Description("Scaling of the target"), PropertyOrder(1), UsedImplicitly]
        public float? Scale { get; set; }

        [Description("Apply to the mount of the target, instead of the target themselves"), PropertyOrder(2), UsedImplicitly]
        public bool ApplyToMount { get; set; }

        [Description("Properties to change, and how much by"), PropertyOrder(3), UsedImplicitly]
        public ObservableCollection<PropertyDef> Properties { get; set; } = new();

        public override string ToString()
        {
            string result = Scale.HasValue && Scale.Value != 1 ? $"Scale {Scale.Value} " : "";
            return result + string.Join(" ", Properties.Select(p => p.ToString())) + (ApplyToMount? " (on mount)" : "");
        }

        public object Clone()
        {
            return new AgentModifierConfig
            {
                Scale = Scale,
                ApplyToMount = ApplyToMount,
                Properties = new(Properties.Select(p => (PropertyDef)p.Clone())),
            };
        }

        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            string mountStr = ApplyToMount ? "Mount " : "";
            if(Scale.HasValue && Scale.Value != 1)
                generator.P($"{mountStr}{Scale.Value*100:0}% normal size");
            if(Scale.HasValue && Scale.Value != 1)
                generator.P($"{mountStr}{Scale.Value*100:0}% normal size");
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
                        newAgentScale *= effect.Scale ?? 1;
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
                float baseValue = target.AgentDrivenProperties.GetStat(prop.Name);
                if (prop.Multiply.HasValue)
                    baseValue *= prop.Multiply.Value;
                if (prop.Add.HasValue)
                    baseValue += prop.Add.Value;
                target.AgentDrivenProperties.SetStat(prop.Name, baseValue);
            }
        }
    }
}