using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    public class SkillRangeDef
    {
        [Description("The skill or skill group"), PropertyOrder(1)]
        public SkillsEnum Skill { get; set; }

        [Description("The min level it should be (actual value will be randomly selected between min and max, " +
                     "valid values are 0 to 300)"),
         PropertyOrder(2)]
        public int MinLevel { get; set; } = 0;

        [Description("The max level it should be (actual value will be randomly selected between min and max, " +
                     "valid values are 0 to 300)"),
         PropertyOrder(3)]
        public int MaxLevel { get; set; } = 50;

        [YamlIgnore, Browsable(false)]
        public bool IsFixed => MinLevel == MaxLevel;
        
        public override string ToString()
        {
            return $"{Skill} {MinLevel} - {MaxLevel}";
        }
    }
}