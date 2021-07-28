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
                 typeof(Agent), typeof(Agent), typeof(GameEntity), typeof(float), typeof(AttackCollisionData),
                 typeof(MissionWeapon), typeof(bool), typeof(bool), typeof(bool), typeof(WeaponComponentData),
                 typeof(CombatLogData)
             },
             new[]
             {
                 ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref,
                 ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out,
                 ArgumentType.Out
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