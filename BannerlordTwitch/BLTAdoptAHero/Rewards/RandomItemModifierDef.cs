using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BannerlordTwitch;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.TwoDimension;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    public class RandomItemModifierDef : ICloneable, INotifyPropertyChanged
    {
        [Description("Custom prize power, a global multiplier for the values below"),
         PropertyOrder(1),
         Range(0.1, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float Power { get; set; } = 1f;
        
        [Description("Weapon damage modifier for custom weapon prize"), PropertyOrder(2), UsedImplicitly]
        public RangeInt WeaponDamage { get; set; } = new(25, 50);

        [Description("Speed modifier for custom weapon prize"), PropertyOrder(3),
         UsedImplicitly]
        public RangeInt WeaponSpeed { get; set; } = new(25, 50);

        [Description("Missile speed modifier for custom weapon prize"), PropertyOrder(4),
         UsedImplicitly]
        public RangeInt WeaponMissileSpeed { get; set; } = new(25, 50);

        [Description("Ammo damage modifier for custom ammo prize"), PropertyOrder(5),
         UsedImplicitly]
        public RangeInt AmmoDamage { get; set; } = new(10, 30);

        [Description("Arrow stack size modifier for custom arrow prize"),
         PropertyOrder(6), UsedImplicitly]
        public RangeInt ArrowStack { get; set; } = new(25, 50);

        [Description("Throwing stack size modifier for custom throwing prize"),
         PropertyOrder(7), UsedImplicitly]
        public RangeInt ThrowingStack { get; set; } = new(2, 6);

        [Description("Armor modifier for custom armor prize"), PropertyOrder(8),
         UsedImplicitly]
        public RangeInt Armor { get; set; } = new(10, 20);

        [Description("Maneuver multiplier for custom mount prize"), PropertyOrder(9),
         UsedImplicitly]
        public RangeFloat MountManeuver { get; set; } = new(1.25f, 2f);

        [Description("Speed multiplier for custom mount prize"), PropertyOrder(10),
         UsedImplicitly]
        public RangeFloat MountSpeed { get; set; } = new(1.25f, 2f);

        [Description("Charge damage multiplier for custom mount prize"),
         PropertyOrder(11), UsedImplicitly]
        public RangeFloat MountChargeDamage { get; set; } = new(1.25f, 2f);

        [Description("Hitpoints multiplier for custom mount prize"), PropertyOrder(12),
         UsedImplicitly]
        public RangeFloat MountHitPoints { get; set; } = new(1.25f, 2f);

        public override string ToString()
        {
            return $"{nameof(Power)}: {Power}, " +
                   $"{nameof(WeaponDamage)}: {WeaponDamage}, " +
                   $"{nameof(WeaponSpeed)}: {WeaponSpeed}, " +
                   $"{nameof(WeaponMissileSpeed)}: {WeaponMissileSpeed}, " +
                   $"{nameof(AmmoDamage)}: {AmmoDamage}, " +
                   $"{nameof(ArrowStack)}: {ArrowStack}, " +
                   $"{nameof(ThrowingStack)}: {ThrowingStack}, " +
                   $"{nameof(Armor)}: {Armor}, " +
                   $"{nameof(MountManeuver)}: {MountManeuver}, " +
                   $"{nameof(MountSpeed)}: {MountSpeed}, " +
                   $"{nameof(MountChargeDamage)}: {MountChargeDamage}, " +
                   $"{nameof(MountHitPoints)}: {MountHitPoints}";
        }

        public ItemModifier Generate(ItemObject item, string customItemName, float customItemPower)
        {
            float modifierPower = Power * customItemPower;
            if (item.WeaponComponent?.PrimaryWeapon?.IsMeleeWeapon == true
                || item.WeaponComponent?.PrimaryWeapon?.IsPolearm == true
                || item.WeaponComponent?.PrimaryWeapon?.IsRangedWeapon == true
            )
            {
                return BLTCustomItemsCampaignBehavior.Current.CreateWeaponModifier(
                    customItemName,
                    (int) Mathf.Ceil(WeaponDamage.RandomInRange() * modifierPower),
                    (int) Mathf.Ceil(WeaponSpeed.RandomInRange() * modifierPower),
                    (int) Mathf.Ceil(WeaponMissileSpeed.RandomInRange() * modifierPower),
                    (short) Mathf.Ceil(ThrowingStack.RandomInRange() * modifierPower)
                );
            }
            else if (item.WeaponComponent?.PrimaryWeapon?.IsAmmo == true)
            {
                return BLTCustomItemsCampaignBehavior.Current.CreateAmmoModifier(
                    customItemName,
                    (int) Mathf.Ceil(AmmoDamage.RandomInRange() * modifierPower),
                    (short) Mathf.Ceil(ArrowStack.RandomInRange() * modifierPower)
                );
            }
            else if (item.HasArmorComponent)
            {
                return BLTCustomItemsCampaignBehavior.Current.CreateArmorModifier(
                    customItemName,
                    (int) Mathf.Ceil(Armor.RandomInRange() * modifierPower)
                );
            }
            else if (item.IsMountable)
            {
                return BLTCustomItemsCampaignBehavior.Current.CreateMountModifier(
                    customItemName,
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

        #region ICloneable
        public object Clone() => CloneHelpers.CloneProperties(this);
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
    }
}