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
        public IEnumerable<EquipmentType> Slots { get { yield return Slot1; yield return Slot2; yield return Slot3; yield return Slot4; } }
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<(EquipmentIndex index, EquipmentType type)> IndexedSlots 
        {
            get
            {
                yield return (EquipmentIndex.Weapon0, Slot1);
                yield return (EquipmentIndex.Weapon1, Slot2);
                yield return (EquipmentIndex.Weapon2, Slot3);
                yield return (EquipmentIndex.Weapon3, Slot4);
            } 
        }

        [YamlIgnore, Browsable(false)]
        public IEnumerable<EquipmentType> SlotItems 
            => Slots.Where(s => s is not EquipmentType.None);
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<EquipmentType> Weapons 
            => Slots.Where(s => s is not (EquipmentType.None or EquipmentType.Shield));
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<(EquipmentIndex index, EquipmentType type)> IndexedWeapons 
            => IndexedSlots.Where(s => s.type is not (EquipmentType.None or EquipmentType.Shield));
        
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
        
        public class ItemSource : IItemsSource
        {
            public static IEnumerable<HeroClassDef> ActiveList { get; set; }

            public ItemCollection GetValues()
            {
                var col = new ItemCollection();
                col.Add(Guid.Empty, "(none)");

                if (ActiveList != null)
                {
                    foreach (var item in ActiveList)
                    {
                        col.Add(item.ID, item.Name);
                    }
                }

                return col;
            }
        }
    }
}