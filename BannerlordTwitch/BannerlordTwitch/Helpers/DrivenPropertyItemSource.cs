using System.Linq;
using BannerlordTwitch.Util;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch.Helpers
{
    public class DrivenPropertyItemSource : IItemsSource
    {
        public static string GetFriendlyName(DrivenProperty property)
        {
            return DrivenPropertyMapping.FirstOrDefault(p => (DrivenProperty) p.Value == property)?.DisplayName 
                   ?? property.ToString().SplitCamelCase();
        }
            
        private static readonly ItemCollection DrivenPropertyMapping = new() {
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
            { DrivenProperty.AttributeHorseArchery, "Horse Archery Ability" }, 
                
            { DrivenProperty.AttributeRiding, "Riding Ability" }, 
            { DrivenProperty.AttributeShield, "Shield Ability" }, 
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

        public ItemCollection GetValues() => DrivenPropertyMapping;
    }
}