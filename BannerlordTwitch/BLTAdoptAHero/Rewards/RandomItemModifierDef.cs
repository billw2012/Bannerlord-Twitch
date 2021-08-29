using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
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
        [LocDisplayName("{=1NLO6Gee}Power"),
         LocDescription("{=LXo3mTnr}Custom prize power, a global multiplier for the values below"),
         Range(0.1, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         PropertyOrder(1), UsedImplicitly]
        public float Power { get; set; } = 1f;
        
        [LocDisplayName("{=g2DZKK7M}Weapon Damage"),
         LocDescription("{=R9lwIsrX}Weapon damage modifier for custom weapon prize"), 
         PropertyOrder(2), UsedImplicitly]
        public RangeInt WeaponDamage { get; set; } = new(25, 50);

        [LocDisplayName("{=7TgyXbNe}Weapon Speed"),
         LocDescription("{=1ZZDexjJ}Speed modifier for custom weapon prize"), 
         PropertyOrder(3), UsedImplicitly]
        public RangeInt WeaponSpeed { get; set; } = new(25, 50);

        [LocDisplayName("{=4zhm204e}Weapon Missile Speed"),
         LocDescription("{=gFO8L8CR}Missile speed modifier for custom weapon prize"), 
         PropertyOrder(4), UsedImplicitly]
        public RangeInt WeaponMissileSpeed { get; set; } = new(25, 50);

        [LocDisplayName("{=ny6l1USK}Ammo Damage"),
         LocDescription("{=oTSCOXpf}Ammo damage modifier for custom ammo prize"), 
         PropertyOrder(5), UsedImplicitly]
        public RangeInt AmmoDamage { get; set; } = new(10, 30);

        [LocDisplayName("{=2ehIq5eD}Arrow Stack"),
         LocDescription("{=inawRS8e}Arrow stack size modifier for custom arrow prize"),
         PropertyOrder(6), UsedImplicitly]
        public RangeInt ArrowStack { get; set; } = new(25, 50);

        [LocDisplayName("{=JUXQcKMR}Throwing Stack"),
         LocDescription("{=QAUf8zj0}Throwing stack size modifier for custom throwing prize"),
         PropertyOrder(7), UsedImplicitly]
        public RangeInt ThrowingStack { get; set; } = new(2, 6);

        [LocDisplayName("{=MrOZ69tP}Armor"),
         LocDescription("{=1x2YSVSz}Armor modifier for custom armor prize"),
         PropertyOrder(8), UsedImplicitly]
        public RangeInt Armor { get; set; } = new(10, 20);

        [LocDisplayName("{=BYekTOq6}Mount Maneuver"),
         LocDescription("{=7cZ5JfEk}Maneuver multiplier for custom mount prize"),
         PropertyOrder(9), UsedImplicitly]
        public RangeFloat MountManeuver { get; set; } = new(1.25f, 2f);

        [LocDisplayName("{=6yURxqNY}Mount Speed"),
         LocDescription("{=eJZxwZc1}Speed multiplier for custom mount prize"),
         PropertyOrder(10), UsedImplicitly]
        public RangeFloat MountSpeed { get; set; } = new(1.25f, 2f);

        [LocDisplayName("{=ZmiOntW1}Mount Charge Damage"),
         LocDescription("{=MZm0DJcs}Charge damage multiplier for custom mount prize"),
         PropertyOrder(11), UsedImplicitly]
        public RangeFloat MountChargeDamage { get; set; } = new(1.25f, 2f);

        [LocDisplayName("{=M8drveMF}Mount Hit Points"),
         LocDescription("{=ewC5EV3Q}Hitpoints multiplier for custom mount prize"), 
         PropertyOrder(12), UsedImplicitly]
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