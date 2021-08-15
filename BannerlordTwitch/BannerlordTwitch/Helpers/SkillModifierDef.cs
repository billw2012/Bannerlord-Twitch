using System;
using System.ComponentModel;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BannerlordTwitch.Helpers
{
    public class SkillModifierDef : ICloneable, INotifyPropertyChanged
    {
        [LocDisplayName("{=OEMBeawy}Skill"), 
         LocDescription("{=46oj12tA}Skill or skill group to modifer (all skills in a group will be modified)"),
         PropertyOrder(1), UsedImplicitly, Document]
        public SkillsEnum Skill { get; set; } = SkillsEnum.All;

        [LocDisplayName("{=jgNUxcBa}ModifierPercent"), 
         LocDescription("{=S34uXWMu}Percent modifier to apply to the skill (100% will result in no chance)"),
         PropertyOrder(2),
         UIRange(0, 1000, 5f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly, Document]
        public float ModifierPercent { get; set; } = 100f;

        [LocDisplayName("{=Au82n7kL}Add"), 
         LocDescription("{=hPQpBmRR}How much to add or subtract from the skill (0 will result in no change). " +
                        "This is applied AFTER Modifier."),
         PropertyOrder(3),
         UsedImplicitly, Document]
        public int Add { get; set; }
        
        public float Apply(float skill) => skill * ModifierPercent / 100f + Add;

        [YamlIgnore, Browsable(false)]
        public string SkillUIName => Skill.ToString().SplitCamelCase();
        
        [YamlIgnore, Browsable(false)]
        public string ModifiersDescription
        {
            get
            {
                string result = string.Empty;
                if (ModifierPercent != 100) result += $"{ModifierPercent}% ";
                if (Add != 0) result += Add > 0 ? $"+{Add}" : $"{Add}";
                if (ModifierPercent == 100 && Add == 0)
                {
                    result += "{=PsUcJvPZ}(no change)".Translate();
                }
                return result;
            }
        }
        
        public override string ToString() => $"{SkillUIName}: {ModifiersDescription}";

        public object Clone() => CloneHelpers.CloneProperties(this);
        
        public event PropertyChangedEventHandler PropertyChanged;
    }
}