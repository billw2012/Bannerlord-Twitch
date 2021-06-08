using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    public class HeroClassDef
    {
        [Description("Unique ID of the class, so you can change the Name without breaking units using that class"), PropertyOrder(1), ReadOnly(true)]
        public Guid ID { get; set; } = Guid.NewGuid();

        [Description("Name of the class that shall be passed to SetHeroClass actions"), PropertyOrder(1)]
        public string Name { get; set; } = "Enter Name Here";

        [Description("Which formation to add summoned units to"), PropertyOrder(2), ItemsSource(typeof(SummonHero.FormationItemSource))]
        public string Formation { get; set; } = "LightCavalry";

        private class MeleeWeaponItemSource : IItemsSource
        {
            public ItemCollection GetValues() => new() {
                ItemObject.ItemTypeEnum.Invalid, 
                ItemObject.ItemTypeEnum.OneHandedWeapon,
                ItemObject.ItemTypeEnum.TwoHandedWeapon,
                ItemObject.ItemTypeEnum.Polearm,
            };
        }
        
        [Description("First melee weapon to use"), ItemsSource(typeof(MeleeWeaponItemSource)), PropertyOrder(3)]
        public ItemObject.ItemTypeEnum MeleeWeapon1 { get; set; }
        
        [Description("Second melee weapon to use"), ItemsSource(typeof(MeleeWeaponItemSource)), PropertyOrder(4)]
        public ItemObject.ItemTypeEnum MeleeWeapon2 { get; set; }
        
        public class RangedWeaponItemSource : IItemsSource
        {
            public ItemCollection GetValues() => new() {
                ItemObject.ItemTypeEnum.Invalid, 
                ItemObject.ItemTypeEnum.Bow,
                ItemObject.ItemTypeEnum.Crossbow,
                ItemObject.ItemTypeEnum.Thrown,
            };
        }
        [Description("Ranged weapon to use"), ItemsSource(typeof(RangedWeaponItemSource)), PropertyOrder(5)]
        public ItemObject.ItemTypeEnum RangedWeapon { get; set; }

        [Description("Whether to use a horse or not"), PropertyOrder(6)]
        public bool Mounted { get; set; }

        [YamlIgnore, Browsable(false)]
        public IEnumerable<ItemObject.ItemTypeEnum> Weapons {
            get
            {
                if (MeleeWeapon1 != ItemObject.ItemTypeEnum.Invalid) yield return MeleeWeapon1;
                if (MeleeWeapon2 != ItemObject.ItemTypeEnum.Invalid) yield return MeleeWeapon2;
                if (RangedWeapon != ItemObject.ItemTypeEnum.Invalid) yield return RangedWeapon;
            }
        }

        [YamlIgnore, Browsable(false)]
        public IEnumerable<SkillObject> Skills
        {
            get
            {
                foreach (var skill in SkillGroup.GetSkillsForItem(Weapons).Select(SkillGroup.GetSkill).Distinct())
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

        public override string ToString() => $"{Name} : {string.Join(", ", Weapons.Select(s => s.ToString()))}" +
                                             (Mounted ? " (mounted)" : "");
    }
}