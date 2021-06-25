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