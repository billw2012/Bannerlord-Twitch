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
            { DrivenProperty.ArmorHead, "{=oUgW3q7p}Armor Head".Translate() }, 
            { DrivenProperty.ArmorTorso, "{=NYoNwE3R}Armor Torso".Translate() }, 
            { DrivenProperty.ArmorLegs, "{=VKAAkKbm}Armor Legs".Translate() }, 
            { DrivenProperty.ArmorArms, "{=dzqBXvaS}Armor Arms".Translate() },
            { DrivenProperty.ArmorEncumbrance, "{=2EaDPdxT}Armor Encumbrance".Translate() },
            { DrivenProperty.WeaponsEncumbrance, "{=Kd1ZpiXo}Weapons Encumbrance".Translate() },
            { DrivenProperty.TopSpeedReachDuration, "{=kZ2KuXHY}Top Speed Reach Duration".Translate() },
            { DrivenProperty.MaxSpeedMultiplier, "{=9yu9o5nH}Max Speed Multiplier".Translate() }, 
            { DrivenProperty.CombatMaxSpeedMultiplier, "{=nvHAoLpy}Combat Max Speed Multiplier".Translate() },
            { DrivenProperty.MountChargeDamage, "{=RIMIU69S}Mount Charge Damage".Translate() }, 
            { DrivenProperty.MountDifficulty, "{=9mYdZQI8}Mount Difficulty".Translate() }, 
            { DrivenProperty.MountManeuver, "{=EaenR5S2}Mount Maneuver".Translate() }, 
            { DrivenProperty.MountSpeed, "{=NJpg1RsS}Mount Speed".Translate() }, 
            { DrivenProperty.AttributeHorseArchery, "{=UtGZ9B6F}Horse Archery Ability".Translate() },
            { DrivenProperty.AttributeRiding, "{=pmUxn1vO}Riding Ability".Translate() }, 
            { DrivenProperty.AttributeShield, "{=wLmAnuDu}Shield Ability".Translate() }, 
            { DrivenProperty.HandlingMultiplier, "{=YZt69g1x}Handling Multiplier".Translate() }, 
            { DrivenProperty.WeaponInaccuracy, "{=Yem5BVmg}Weapon Inaccuracy".Translate() },
            { DrivenProperty.SwingSpeedMultiplier, "{=DUuJ6bM5}Swing Speed Multiplier".Translate() },
            { DrivenProperty.ReloadSpeed, "{=djGCJeM4}Reload Speed".Translate() },
            { DrivenProperty.ReloadMovementPenaltyFactor, "{=4XrdvYDR}Reload Movement Penalty".Translate() },
            { DrivenProperty.AttributeCourage, "{=GJftwazi}Courage".Translate() },
            { DrivenProperty.AttributeShieldMissileCollisionBodySizeAdder, "{=8WAxev1i}Shield Missile Collision Body Size Adder".Translate() },
            { DrivenProperty.ThrustOrRangedReadySpeedMultiplier, "{=xxkAFaWD}Thrust Or Ranged Ready Speed Multiplier".Translate() },
            { DrivenProperty.AiShootFreq, "{=iaxxNiiV}AI Shoot Frequency".Translate() },
            { DrivenProperty.AiWaitBeforeShootFactor, "{=3HlCPScr}AI Wait Before Shoot Factor".Translate() },
            { DrivenProperty.AIBlockOnDecideAbility, "{=7xW74SvW}AI Block Chance".Translate() },
            { DrivenProperty.AIAttackOnDecideChance, "{=vIjQ6b5w}AI Attack Chance".Translate() },
            { DrivenProperty.AiKick, "{=FgSI5C4F}AI Kick".Translate() }, 
                
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
            #if !e159
            { DrivenProperty.AiShooterError, "AiShooterError" },
            #endif
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