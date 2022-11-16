using System;
using System.Linq;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch.Helpers
{
    public enum EquipmentType
    {
        [LocDisplayName("{=rMIAZRoT}None")] None,
        [LocDisplayName("{=2gjNbpQI}Dagger")] Dagger,
        [LocDisplayName("{=OTlpiu1X}One Handed Sword")] OneHandedSword,
        [LocDisplayName("{=LGaaSaY8}Two Handed Sword")] TwoHandedSword,
        [LocDisplayName("{=3teP1CbV}One Handed Axe")] OneHandedAxe,
        [LocDisplayName("{=AufoIUAI}Two Handed Axe")] TwoHandedAxe,
        [LocDisplayName("{=74iFLLnE}One Handed Mace")] OneHandedMace,
        [LocDisplayName("{=EVEYPnFF}Two Handed Mace")] TwoHandedMace,
        [LocDisplayName("{=bvOtwaZm}One Handed Lance")] OneHandedLance,
        [LocDisplayName("{=6fEElc9f}Two Handed Lance")] TwoHandedLance,
        [LocDisplayName("{=ICcCZnli}One Handed Glaive")] OneHandedGlaive,
        [LocDisplayName("{=KpYZoZE9}Two Handed Glaive")] TwoHandedGlaive,
        [LocDisplayName("{=HKYDdm9S}Bow")] Bow,
        [LocDisplayName("{=3jigLyuH}Crossbow")] Crossbow,
        [LocDisplayName("{=UjKvUAX1}Arrows")] Arrows,
        [LocDisplayName("{=QBgdwwa3}Bolts")] Bolts,
        [LocDisplayName("{=IUmqXtY6}Throwing Knives")] ThrowingKnives,
        [LocDisplayName("{=mSnnORJ1}Throwing Axes")] ThrowingAxes,
        [LocDisplayName("{=WUrrIbmH}Throwing Javelins")] ThrowingJavelins,
        [LocDisplayName("{=om9ZF9Mu}Shield")] Shield,
        [LocDisplayName("{=vIT4xG8X}Stone")] Stone,
        Num,
    }

    public class EquipmentTypeItemSource : IItemsSource
    {
        private static readonly ItemCollection items = new()
        {
            { EquipmentType.None, "{=i7XX56i9}None".Translate() },
            { EquipmentType.Dagger, "{=5HQtHOg4}Dagger".Translate() },
            { EquipmentType.OneHandedSword, "{=hZU8m8K9}OneHandedSword".Translate() },
            { EquipmentType.TwoHandedSword, "{=Lc70XOXH}TwoHandedSword".Translate() },
            { EquipmentType.OneHandedAxe, "{=amKrRDlF}OneHandedAxe".Translate() },
            { EquipmentType.TwoHandedAxe, "{=QPv8qS6h}TwoHandedAxe".Translate() },
            { EquipmentType.OneHandedMace, "{=jTI9J3nq}OneHandedMace".Translate() },
            { EquipmentType.TwoHandedMace, "{=YzVmN0wN}TwoHandedMace".Translate() },
            { EquipmentType.OneHandedLance, "{=pP2mtmXu}OneHandedLance".Translate() },
            { EquipmentType.TwoHandedLance, "{=Y6CxpaYL}TwoHandedLance".Translate() },
            { EquipmentType.OneHandedGlaive, "{=r22oB2bm}OneHandedGlaive".Translate() },
            { EquipmentType.TwoHandedGlaive, "{=BcmE6Y75}TwoHandedGlaive".Translate() },
            { EquipmentType.Bow, "{=rroxl8j3}Bow".Translate() },
            { EquipmentType.Crossbow, "{=CztMY8ZE}Crossbow".Translate() },
            { EquipmentType.Arrows, "{=DYYyhDUI}Arrows".Translate() },
            { EquipmentType.Bolts, "{=i281jKBH}Bolts".Translate() },
            { EquipmentType.ThrowingKnives, "{=C1Qx1ZqP}ThrowingKnives".Translate() },
            { EquipmentType.ThrowingAxes, "{=S1NUKL4r}ThrowingAxes".Translate() },
            { EquipmentType.ThrowingJavelins, "{=ND1x0R2V}ThrowingJavelins".Translate() },
            { EquipmentType.Shield, "{=p4eF4kc6}Shield".Translate() },
            { EquipmentType.Stone, "{=dYKgMqE8}Stone".Translate() },
        };

        public ItemCollection GetValues() => items;
        
        public static string GetFriendlyName(EquipmentType value) 
            => items.FirstOrDefault(p => (EquipmentType) p.Value == value)?.DisplayName ?? "(none)";
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
                EquipmentType.Dagger => item.PrimaryWeaponClass(WeaponClass.Dagger) && !item.IsRanged(),
                EquipmentType.OneHandedSword => item.PrimaryWeaponClass(WeaponClass.OneHandedSword) && !item.IsRanged(),
                EquipmentType.TwoHandedSword => item.PrimaryWeaponClass(WeaponClass.TwoHandedSword) && !item.IsRanged(),
                EquipmentType.OneHandedAxe => item.PrimaryWeaponClass(WeaponClass.OneHandedAxe) && !item.IsRanged(),
                EquipmentType.TwoHandedAxe => item.PrimaryWeaponClass(WeaponClass.TwoHandedAxe) && !item.IsRanged(),
                EquipmentType.OneHandedMace => item.PrimaryWeaponClass(WeaponClass.Mace) && !item.IsRanged(),
                EquipmentType.TwoHandedMace => item.PrimaryWeaponClass(WeaponClass.TwoHandedMace) && !item.IsRanged(),
                EquipmentType.OneHandedLance => item.PrimaryWeaponClass(WeaponClass.OneHandedPolearm) && !item.IsSwingable() && !item.IsRanged(),
                EquipmentType.TwoHandedLance => item.PrimaryWeaponClass(WeaponClass.TwoHandedPolearm) && !item.IsSwingable() && !item.IsRanged(),
                EquipmentType.OneHandedGlaive => item.PrimaryWeaponClass(WeaponClass.OneHandedPolearm) && item.IsSwingable() && !item.IsRanged(),
                EquipmentType.TwoHandedGlaive => item.PrimaryWeaponClass(WeaponClass.TwoHandedPolearm) && item.IsSwingable() && !item.IsRanged(),
                EquipmentType.Bow => item.ItemType == ItemObject.ItemTypeEnum.Bow,
                EquipmentType.Crossbow => item.ItemType == ItemObject.ItemTypeEnum.Crossbow,
                EquipmentType.Arrows => item.ItemType == ItemObject.ItemTypeEnum.Arrows,
                EquipmentType.Bolts => item.ItemType == ItemObject.ItemTypeEnum.Bolts,
                EquipmentType.ThrowingKnives => item.PrimaryWeaponClass(WeaponClass.ThrowingKnife),
                EquipmentType.ThrowingAxes => item.PrimaryWeaponClass(WeaponClass.ThrowingAxe),
                EquipmentType.ThrowingJavelins => item.PrimaryWeaponClass(WeaponClass.Javelin),
                EquipmentType.Shield => item.ItemType == ItemObject.ItemTypeEnum.Shield,
                EquipmentType.Stone => item.HasWeaponClass(WeaponClass.Stone) ,
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
                EquipmentType.Stone => WeaponClass.Stone,
                _ => throw new ArgumentOutOfRangeException(nameof(equipmentType), equipmentType, null)
            };
    }
}