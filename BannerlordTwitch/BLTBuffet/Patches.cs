using HarmonyLib;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace BLTBuffet
{
    [HarmonyPatch, UsedImplicitly]
    public static class Patches
    {
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(Mission), "GetAttackCollisionResults",
             new[]
             {
                 typeof(Agent), // attackerAgent
                 typeof(Agent), // victimAgent
                 typeof(GameEntity), // hitObject
                 typeof(float), // momentumRemaining
                 typeof(MissionWeapon), // attackerWeapon
                 typeof(bool), // crushedThrough
                 typeof(bool), // cancelDamage
                 typeof(bool), // crushedThroughWithoutAgentCollision
                 typeof(AttackCollisionData), // attackCollisionData
                 typeof(WeaponComponentData), // shieldOnBack
                 typeof(CombatLogData), // combatLogData
             },
            new[]
             {
                 ArgumentType.Normal, // attackerAgent
                 ArgumentType.Normal, // victimAgent
                 ArgumentType.Normal, // hitObject
                 ArgumentType.Normal, // momentumRemaining
                 ArgumentType.Ref, // in attackerWeapon
                 ArgumentType.Normal, // crushedThrough
                 ArgumentType.Normal, // cancelDamage
                 ArgumentType.Normal, // crushedThroughWithoutAgentCollision
                 ArgumentType.Ref, // ref attackCollisionData
                 ArgumentType.Out, // out shieldOnBack
                 ArgumentType.Out, // out combatLogData
                 
             })]

        public static void GetAttackCollisionResultsPostfix(Mission __instance, Agent attackerAgent, Agent victimAgent, ref AttackCollisionData attackCollisionData)
        {
            CharacterEffect.BLTEffectsBehaviour.Get().ApplyHitDamage(attackerAgent, victimAgent, ref attackCollisionData);
        }

        // [UsedImplicitly]
        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(Mission), "AddActiveMissionObject")]
        // public static void AddActiveMissionObjectPostfix()
        // {
        //     Log.Screen("Test");
        // }
    }
}