using System.Collections.Generic;
using System.ComponentModel;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    [CategoryOrder("General", 1)]
    [CategoryOrder("Match Rewards", 2)]
    [CategoryOrder("Prize", 3)]
    [CategoryOrder("Prize Tier", 4)]
    [CategoryOrder("Custom Prize", 5)]
    internal class GlobalTournamentConfig
    {
        private const string ID = "Adopt A Hero - Tournament Config";
        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalTournamentConfig));
        internal static GlobalTournamentConfig Get() => ActionManager.GetGlobalConfig<GlobalTournamentConfig>(ID);

        [Category("General"), Description("Amount to multiply normal starting health by"), PropertyOrder(1)]
        public float StartHealthMultiplier { get; set; } = 2;

        [Category("General"), Description("Remove horses completely from the BLT tournaments (the horse AI is terrible)"), PropertyOrder(2)]
        public bool NoHorses { get; set; } = true;
        
        [Category("General"), Description("Replaces all lances and spears with swords, because lance and spear combat is terrible"), PropertyOrder(3)]
        public bool NoSpears { get; set; } = true;
        
        [Category("General"), 
         Description("Replaces all armor with fixed tier armor, based on Culture if possible " +
                     "(tier specified by Normalized Armor Tier below)"), 
         PropertyOrder(4), UsedImplicitly]
        public bool NormalizeArmor { get; set; }

        [Category("General"),
         Description("Armor tier to set all contenstants to (1 to 6), if Normalize Armor is enabled"),
         PropertyOrder(5), UsedImplicitly]
        public int NormalizeArmorTier { get; set; } = 3;

        [Category("Rewards"), Description("Gold won if the hero wins the tournaments"), PropertyOrder(1)]
        public int WinGold { get; set; } = 50000;

        [Category("Rewards"), Description("XP given if the hero wins the tournaments"), PropertyOrder(2)]
        public int WinXP { get; set; } = 50000;

        [Category("Rewards"), Description("XP given if the hero participates in a tournament"), PropertyOrder(3)]
        public int ParticipateXP { get; set; } = 10000;

        [Category("Match Rewards"), Description("Gold won if the hero wins their match"), PropertyOrder(1)]
        public int WinMatchGold { get; set; } = 10000;

        [Category("Match Rewards"), Description("XP given if the hero wins their match"), PropertyOrder(2)]
        public int WinMatchXP { get; set; } = 10000;

        [Category("Match Rewards"), Description("XP given if the hero participates in a match"), PropertyOrder(3)]
        public int ParticipateMatchXP { get; set; } = 2500;

        [Category("Prize"), Description("Relative proportion of prizes that will be weapons. This includes all one handed, two handed, ranged and ammo."), PropertyOrder(1)]
        public float PrizeWeaponWeight { get; set; } = 1f;

        [Category("Prize"), Description("Relative proportion of prizes that will be armor"), PropertyOrder(2)]
        public float PrizeArmorWeight { get; set; } = 1f;

        [Category("Prize"), Description("Relative proportion of prizes that will be mounts"), PropertyOrder(3)]
        public float PrizeMountWeight { get; set; } = 0.1f;
        
        // Prizes:
        // Random vanilla equipment, chance for each tier
        // Generated vanilla equip,ent

        [Category("Prize Tier"), Description("Relative proportion of prizes that will be Tier 1"), PropertyOrder(1)]
        public float PrizeTier1Weight { get; set; } = 0f;

        [Category("Prize Tier"), Description("Relative proportion of prizes that will be Tier 2"), PropertyOrder(2)]
        public float PrizeTier2Weight { get; set; } = 0f;

        [Category("Prize Tier"), Description("Relative proportion of prizes that will be Tier 3"), PropertyOrder(3)]
        public float PrizeTier3Weight { get; set; } = 0f;

        [Category("Prize Tier"), Description("Relative proportion of prizes that will be Tier 4"), PropertyOrder(4)]
        public float PrizeTier4Weight { get; set; } = 0f;

        [Category("Prize Tier"), Description("Relative proportion of prizes that will be Tier 5"), PropertyOrder(5)]
        public float PrizeTier5Weight { get; set; } = 3f;

        [Category("Prize Tier"), Description("Relative proportion of prizes that will be Tier 6"), PropertyOrder(6)]
        public float PrizeTier6Weight { get; set; } = 2f;

        [Category("Prize Tier"), Description("Relative proportion of prizes that will be Custom (Tier 6 with modifiers as per the Custom Prize settings below)"), PropertyOrder(7)]
        public float PrizeCustomWeight { get; set; } = 1f;

        [Browsable(false), YamlIgnore]
        public IEnumerable<(int tier, float weight)> PrizeTierWeights
        {
            get
            {
                yield return (tier: 0, weight: PrizeTier1Weight);
                yield return (tier: 1, weight: PrizeTier2Weight);
                yield return (tier: 2, weight: PrizeTier3Weight);
                yield return (tier: 3, weight: PrizeTier4Weight);
                yield return (tier: 4, weight: PrizeTier5Weight);
                yield return (tier: 5, weight: PrizeTier6Weight);
                yield return (tier: 6, weight: PrizeCustomWeight);
            }
        }

        public class CustomPrizeConfig
        {
            [Description("Custom prize power, a global multiplier for the values below"), PropertyOrder(1)]
            public float Power { get; set; } = 1f;

            [Description("Weapon damage modifier for custom weapon prize"), PropertyOrder(2), UsedImplicitly, ExpandableObject]
            public RangeInt WeaponDamage { get; set; } = new(25, 50);
            
            [Description("Speed modifier for custom weapon prize"), PropertyOrder(3), UsedImplicitly, ExpandableObject]
            public RangeInt WeaponSpeed { get; set; } = new(25, 50);
            
            [Description("Missile speed modifier for custom weapon prize"), PropertyOrder(4), UsedImplicitly, ExpandableObject]
            public RangeInt WeaponMissileSpeed { get; set; } = new(25, 50);
            
            [Description("Ammo damage modifier for custom ammo prize"), PropertyOrder(5), UsedImplicitly, ExpandableObject]
            public RangeInt AmmoDamage { get; set; } = new (10, 30);
              
            [Description("Arrow stack size modifier for custom arrow prize"), PropertyOrder(6), UsedImplicitly, ExpandableObject]
            public RangeInt ArrowStack { get; set; } = new(25, 50);
              
            [Description("Throwing stack size modifier for custom throwing prize"), PropertyOrder(7), UsedImplicitly, ExpandableObject]
            public RangeInt ThrowingStack { get; set; } = new(2, 6);
            
            [Description("Armor modifier for custom armor prize"), PropertyOrder(8), UsedImplicitly, ExpandableObject]
            public RangeInt Armor { get; set; } = new(10, 20);
            
            [Description("Maneuver multiplier for custom mount prize"), PropertyOrder(9), UsedImplicitly, ExpandableObject]
            public RangeFloat MountManeuver { get; set; } = new(1.25f, 2f);
            
            [Description("Speed multiplier for custom mount prize"), PropertyOrder(10), UsedImplicitly, ExpandableObject]
            public RangeFloat MountSpeed { get; set; } = new(1.25f, 2f);
              
            [Description("Charge damage multiplier for custom mount prize"), PropertyOrder(11), UsedImplicitly, ExpandableObject]
            public RangeFloat MountChargeDamage { get; set; } = new(1.25f, 2f);

            [Description("Hitpoints multiplier for custom mount prize"), PropertyOrder(12), UsedImplicitly, ExpandableObject]
            public RangeFloat MountHitPoints { get; set; } = new(1.25f, 2f);
        }

        [Category("Custom Prize"), Description("Custom prize configuration"), PropertyOrder(1), ExpandableObject, UsedImplicitly]
        public CustomPrizeConfig CustomPrize { get; set; } = new();

        public enum PrizeType
        {
            Weapon,
            Armor,
            Mount
        }

        [Browsable(false), YamlIgnore]
        public IEnumerable<(PrizeType type, float weight)> PrizeTypeWeights {
            get
            {
                yield return (type: PrizeType.Weapon, weight: PrizeWeaponWeight);
                yield return (type: PrizeType.Armor, weight: PrizeArmorWeight);
                yield return (type: PrizeType.Mount, weight: PrizeMountWeight);
            }
        }
    }
}