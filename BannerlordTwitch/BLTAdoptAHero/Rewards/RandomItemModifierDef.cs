using System;
using System.ComponentModel;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.TwoDimension;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    public class RandomItemModifierDef : ICloneable
    {
        [Description("Name format for modified items, {ITEMNAME} is the placeholder for the base item name."),
         PropertyOrder(0), UsedImplicitly]
        public string Name { get; set; } = "Modified {ITEMNAME}";
        
        [Description("Custom prize power, a global multiplier for the values below"),
         PropertyOrder(1), UsedImplicitly]
        public float Power { get; set; } = 1f;

        [Description("Weapon damage modifier for custom weapon prize"), PropertyOrder(2),
         UsedImplicitly, ExpandableObject]
        public RangeInt WeaponDamage { get; set; } = new(25, 50);

        [Description("Speed modifier for custom weapon prize"), PropertyOrder(3),
         UsedImplicitly, ExpandableObject]
        public RangeInt WeaponSpeed { get; set; } = new(25, 50);

        [Description("Missile speed modifier for custom weapon prize"), PropertyOrder(4),
         UsedImplicitly, ExpandableObject]
        public RangeInt WeaponMissileSpeed { get; set; } = new(25, 50);

        [Description("Ammo damage modifier for custom ammo prize"), PropertyOrder(5),
         UsedImplicitly, ExpandableObject]
        public RangeInt AmmoDamage { get; set; } = new(10, 30);

        [Description("Arrow stack size modifier for custom arrow prize"),
         PropertyOrder(6), UsedImplicitly, ExpandableObject]
        public RangeInt ArrowStack { get; set; } = new(25, 50);

        [Description("Throwing stack size modifier for custom throwing prize"),
         PropertyOrder(7), UsedImplicitly, ExpandableObject]
        public RangeInt ThrowingStack { get; set; } = new(2, 6);

        [Description("Armor modifier for custom armor prize"), PropertyOrder(8),
         UsedImplicitly, ExpandableObject]
        public RangeInt Armor { get; set; } = new(10, 20);

        [Description("Maneuver multiplier for custom mount prize"), PropertyOrder(9),
         UsedImplicitly, ExpandableObject]
        public RangeFloat MountManeuver { get; set; } = new(1.25f, 2f);

        [Description("Speed multiplier for custom mount prize"), PropertyOrder(10),
         UsedImplicitly, ExpandableObject]
        public RangeFloat MountSpeed { get; set; } = new(1.25f, 2f);

        [Description("Charge damage multiplier for custom mount prize"),
         PropertyOrder(11), UsedImplicitly, ExpandableObject]
        public RangeFloat MountChargeDamage { get; set; } = new(1.25f, 2f);

        [Description("Hitpoints multiplier for custom mount prize"), PropertyOrder(12),
         UsedImplicitly, ExpandableObject]
        public RangeFloat MountHitPoints { get; set; } = new(1.25f, 2f);

        public ItemModifier Generate(ItemObject item)
        {
            float modifierPower = Power;
            if (item.WeaponComponent?.PrimaryWeapon?.IsMeleeWeapon == true
                || item.WeaponComponent?.PrimaryWeapon?.IsPolearm == true
                || item.WeaponComponent?.PrimaryWeapon?.IsRangedWeapon == true
            )
            {
                return BLTCustomItemsCampaignBehavior.Current.CreateWeaponModifier(
                    Name,
                    (int) Mathf.Ceil(WeaponDamage.RandomInRange() * modifierPower),
                    (int) Mathf.Ceil(WeaponSpeed.RandomInRange() * modifierPower),
                    (int) Mathf.Ceil(WeaponMissileSpeed.RandomInRange() * modifierPower),
                    (short) Mathf.Ceil(ThrowingStack.RandomInRange() * modifierPower)
                );
            }
            else if (item.WeaponComponent?.PrimaryWeapon?.IsAmmo == true)
            {
                return BLTCustomItemsCampaignBehavior.Current.CreateAmmoModifier(
                    Name,
                    (int) Mathf.Ceil(AmmoDamage.RandomInRange() * modifierPower),
                    (short) Mathf.Ceil(ArrowStack.RandomInRange() * modifierPower)
                );
            }
            else if (item.HasArmorComponent)
            {
                return BLTCustomItemsCampaignBehavior.Current.CreateArmorModifier(
                    Name,
                    (int) Mathf.Ceil(Armor.RandomInRange() * modifierPower)
                );
            }
            else if (item.IsMountable)
            {
                return BLTCustomItemsCampaignBehavior.Current.CreateMountModifier(
                    Name,
                    MountManeuver.RandomInRange() * modifierPower,
                    MountSpeed.RandomInRange() * modifierPower,
                    MountChargeDamage.RandomInRange() * modifierPower,
                    MountHitPoints.RandomInRange() * modifierPower
                );
            }
            else
            {
                Log.Error($"Cannot generate modifier for {item.Name}: its modifier requirements could not be determined");
                return null;
            }
        }
        
        public object Clone() => CloneHelpers.CloneProperties(this);
    }
}