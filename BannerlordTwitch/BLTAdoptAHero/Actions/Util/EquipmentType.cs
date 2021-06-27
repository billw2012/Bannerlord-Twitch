using System;
using System.Linq;
using TaleWorlds.Core;

namespace BLTAdoptAHero
{
    public enum EquipmentType
    {
        None,
        Dagger,
        OneHandedSword,
        TwoHandedSword,
        OneHandedAxe,
        TwoHandedAxe,
        OneHandedMace,
        TwoHandedMace,
        OneHandedLance,
        TwoHandedLance,
        OneHandedGlaive,
        TwoHandedGlaive,
        Bow,
        Crossbow,
        Arrows,
        Bolts,
        ThrowingKnives,
        ThrowingAxes,
        ThrowingJavelins,
        Shield,
        Num,
    }

    public static class EquipmentTypeHelpers
    {
        public static bool AnyWeaponMatches(this ItemObject item, Func<WeaponComponentData, bool> predicate)
            => item.Weapons?.Any(predicate) == true;

        public static bool NoWeaponMatches(this ItemObject item, Func<WeaponComponentData, bool> predicate)
            => item.Weapons?.Any(predicate) != true;

        public static bool IsSwingable(this ItemObject item) 
            => item.AnyWeaponMatches(w => w.IsMeleeWeapon && w.SwingDamageType != DamageTypes.Invalid);
        
        public static bool IsRanged(this ItemObject item) 
            => item.AnyWeaponMatches(w => w.IsRangedWeapon);

        public static bool HasWeaponClass(this ItemObject item, WeaponClass weaponClass)
            => item.AnyWeaponMatches(w => w.WeaponClass == weaponClass);
        
        public static bool PrimaryWeaponClass(this ItemObject item, WeaponClass weaponClass)
            => item.PrimaryWeapon?.WeaponClass == weaponClass;
        
        public static bool IsEquipmentType(this ItemObject item, EquipmentType equipmentType)
        {
            return equipmentType switch
            {
                EquipmentType.None => false,
                EquipmentType.Dagger => item.HasWeaponClass(WeaponClass.Dagger) && !item.IsRanged(),
                EquipmentType.OneHandedSword => item.HasWeaponClass(WeaponClass.OneHandedSword) && !item.IsRanged(),
                EquipmentType.TwoHandedSword => item.HasWeaponClass(WeaponClass.TwoHandedSword) && !item.IsRanged(),
                EquipmentType.OneHandedAxe => item.HasWeaponClass(WeaponClass.OneHandedAxe) && !item.IsRanged(),
                EquipmentType.TwoHandedAxe => item.HasWeaponClass(WeaponClass.TwoHandedAxe) && !item.IsRanged(),
                EquipmentType.OneHandedMace => item.HasWeaponClass(WeaponClass.Mace) && !item.IsRanged(),
                EquipmentType.TwoHandedMace => item.HasWeaponClass(WeaponClass.TwoHandedMace) && !item.IsRanged(),
                EquipmentType.OneHandedLance => item.HasWeaponClass(WeaponClass.OneHandedPolearm) && !item.IsSwingable() && !item.IsRanged(),
                EquipmentType.TwoHandedLance => item.HasWeaponClass(WeaponClass.TwoHandedPolearm) && !item.IsSwingable() && !item.IsRanged(),
                EquipmentType.OneHandedGlaive => item.HasWeaponClass(WeaponClass.OneHandedPolearm) && item.IsSwingable() && !item.IsRanged(),
                EquipmentType.TwoHandedGlaive => item.HasWeaponClass(WeaponClass.TwoHandedPolearm) && item.IsSwingable() && !item.IsRanged(),
                EquipmentType.Bow => item.ItemType == ItemObject.ItemTypeEnum.Bow,
                EquipmentType.Crossbow => item.ItemType == ItemObject.ItemTypeEnum.Crossbow,
                EquipmentType.Arrows => item.ItemType == ItemObject.ItemTypeEnum.Arrows,
                EquipmentType.Bolts => item.ItemType == ItemObject.ItemTypeEnum.Bolts,
                EquipmentType.ThrowingKnives => item.PrimaryWeaponClass(WeaponClass.ThrowingKnife),
                EquipmentType.ThrowingAxes => item.PrimaryWeaponClass(WeaponClass.ThrowingAxe),
                EquipmentType.ThrowingJavelins => item.PrimaryWeaponClass(WeaponClass.Javelin),
                EquipmentType.Shield => item.ItemType == ItemObject.ItemTypeEnum.Shield,
                _ => throw new ArgumentOutOfRangeException(nameof(equipmentType), equipmentType, null)
            };
        }

        public static EquipmentType GetEquipmentType(this ItemObject item)
        {
            for (var i = EquipmentType.None; i < EquipmentType.Num; i++)
            {
                if (item.IsEquipmentType(i))
                    return i;
            }
            return EquipmentType.None;
        }
        
        public static WeaponClass GetWeaponClass(EquipmentType equipmentType) =>
            equipmentType switch
            {
                EquipmentType.Dagger => WeaponClass.Dagger,
                EquipmentType.OneHandedSword => WeaponClass.OneHandedSword,
                EquipmentType.TwoHandedSword => WeaponClass.TwoHandedSword,
                EquipmentType.OneHandedAxe => WeaponClass.OneHandedAxe,
                EquipmentType.TwoHandedAxe => WeaponClass.TwoHandedAxe,
                EquipmentType.OneHandedMace => WeaponClass.Mace,
                EquipmentType.TwoHandedMace => WeaponClass.TwoHandedMace,
                EquipmentType.OneHandedLance => WeaponClass.OneHandedPolearm,
                EquipmentType.TwoHandedLance => WeaponClass.TwoHandedPolearm,
                EquipmentType.OneHandedGlaive => WeaponClass.OneHandedPolearm,
                EquipmentType.TwoHandedGlaive => WeaponClass.TwoHandedPolearm,
                EquipmentType.Bow => WeaponClass.Bow,
                EquipmentType.Crossbow => WeaponClass.Crossbow,
                EquipmentType.Arrows => WeaponClass.Arrow,
                EquipmentType.Bolts => WeaponClass.Bolt,
                EquipmentType.ThrowingKnives => WeaponClass.ThrowingKnife,
                EquipmentType.ThrowingAxes => WeaponClass.ThrowingAxe,
                EquipmentType.ThrowingJavelins => WeaponClass.Javelin,
                _ => throw new ArgumentOutOfRangeException(nameof(equipmentType), equipmentType, null)
            };
    }
}