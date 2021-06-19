using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace BLTAdoptAHero.Actions.Util
{
    public static class ItemModifierAccess
    {
        public static void SetName(this ItemModifier item, TextObject name) => 
            AccessTools.Property(typeof(ItemModifier), nameof(ItemModifier.Name)).SetValue(item, name);
        public static void SetDamageModifier(this ItemModifier item, int value) => 
            AccessTools.Field(typeof(ItemModifier), "_damage").SetValue(item, value);
        public static void SetSpeedModifier(this ItemModifier item, int value) => 
            AccessTools.Field(typeof(ItemModifier), "_speed").SetValue(item, value);
        public static void SetMissileSpeedModifier(this ItemModifier item, int value) => 
            AccessTools.Field(typeof(ItemModifier), "_missileSpeed").SetValue(item, value);
        public static void SetArmorModifier(this ItemModifier item, int value) => 
            AccessTools.Field(typeof(ItemModifier), "_armor").SetValue(item, value);
        public static void SetHitPointsModifier(this ItemModifier item, short value) => 
            AccessTools.Field(typeof(ItemModifier), "_hitPoints").SetValue(item, value);
        public static void SetStackCountModifier(this ItemModifier item, short value) => 
            AccessTools.Field(typeof(ItemModifier), "_stackCount").SetValue(item, value);
        public static void SetMountSpeedModifier(this ItemModifier item, float value) => 
            AccessTools.Field(typeof(ItemModifier), "_mountSpeed").SetValue(item, value);
        public static void SetManeuverModifier(this ItemModifier item, float value) => 
            AccessTools.Field(typeof(ItemModifier), "_maneuver").SetValue(item, value);
        public static void SetChargeDamageModifier(this ItemModifier item, float value) => 
            AccessTools.Field(typeof(ItemModifier), "_chargeDamage").SetValue(item, value);
        public static void SetMountHitPointsModifier(this ItemModifier item, float value) => AccessTools.Field(typeof(ItemModifier), "_mountHitPoints").SetValue(item, value);    
    }
}