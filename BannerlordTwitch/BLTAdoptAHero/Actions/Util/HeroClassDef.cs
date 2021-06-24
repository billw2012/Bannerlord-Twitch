using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BLTAdoptAHero.Annotations;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

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
                EquipmentType.Dagger => item.PrimaryWeaponClass(WeaponClass.Dagger),
                EquipmentType.OneHandedSword => item.PrimaryWeaponClass(WeaponClass.OneHandedSword),
                EquipmentType.TwoHandedSword => item.PrimaryWeaponClass(WeaponClass.TwoHandedSword),
                EquipmentType.OneHandedAxe => item.PrimaryWeaponClass(WeaponClass.OneHandedAxe),
                EquipmentType.TwoHandedAxe => item.PrimaryWeaponClass(WeaponClass.TwoHandedAxe),
                EquipmentType.OneHandedMace => item.PrimaryWeaponClass(WeaponClass.Mace),
                EquipmentType.TwoHandedMace => item.PrimaryWeaponClass(WeaponClass.TwoHandedMace),
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
                _ => throw new ArgumentOutOfRangeException(nameof(equipmentType), equipmentType, null)
            };
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
    
    public class HeroClassDef
    {
        [Browsable(false), UsedImplicitly]
        public Guid ID { get; set; } = Guid.NewGuid();

        [Description("Name of the class that shall be passed to SetHeroClass actions"), PropertyOrder(1)]
        public string Name { get; set; } = "Enter Name Here";

        [Description("Which formation to add summoned units to"), PropertyOrder(2), ItemsSource(typeof(SummonHero.FormationItemSource))]
        public string Formation { get; set; } = "LightCavalry";
        
        [Description("Item type to put in slot 1"), PropertyOrder(3), UsedImplicitly]
        public EquipmentType Slot1 { get; set; }
        
        [Description("Item type to put in slot 2"), PropertyOrder(4), UsedImplicitly]
        public EquipmentType Slot2 { get; set; }
        
        [Description("Item type to put in slot 3"), PropertyOrder(5), UsedImplicitly]
        public EquipmentType Slot3 { get; set; }
        
        [Description("Item type to put in slot 4"), PropertyOrder(6), UsedImplicitly]
        public EquipmentType Slot4 { get; set; }

        [YamlIgnore, Browsable(false)]
        public IEnumerable<EquipmentType> SlotItems 
            => new[] {Slot1, Slot2, Slot3, Slot4,}.Where(s => s is not EquipmentType.None);
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<EquipmentType> Weapons 
            => new[] {Slot1, Slot2, Slot3, Slot4,}.Where(s => s is not (EquipmentType.None or EquipmentType.Shield));

        [Description("Whether to allow horse (can be combined with Use Camel)"), PropertyOrder(7), UsedImplicitly]
        public bool UseHorse { get; set; }
        
        [Description("Whether to allow camel (can be combined with Use Horse"), PropertyOrder(8), UsedImplicitly]
        public bool UseCamel { get; set; }

        [YamlIgnore, Browsable(false)]
        public bool Mounted => UseHorse || UseCamel;
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<SkillObject> Skills
        {
            get
            {
                foreach (var skill in SkillGroup.GetSkillsForEquipmentType(SlotItems)
                    .Where(s => s != SkillsEnum.None)
                    .Select(SkillGroup.GetSkill)
                    .Distinct())
                {
                    yield return skill;
                }

                if (Mounted)
                {
                    yield return DefaultSkills.Riding;
                }
                else
                {
                    yield return DefaultSkills.Athletics;
                }
            }
        }

        public override string ToString() => $"{Name} : {string.Join(", ", SlotItems.Select(s => s.ToString()))}" 
                                             + (UseHorse ? " (Use Horse)" : "")
                                             + (UseCamel ? " (Use Camel)" : "");
    }
}