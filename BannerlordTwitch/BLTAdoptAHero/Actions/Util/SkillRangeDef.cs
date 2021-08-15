using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=uUWdyER4}Skill Range")]
    public class SkillRangeDef : INotifyPropertyChanged
    {
        [LocDisplayName("{=OEMBeawy}Skill"), 
         LocDescription("{=WcQpCpdI}The skill or skill group"), 
         PropertyOrder(1), UsedImplicitly]
        public SkillsEnum Skill { get; set; }

        [LocDisplayName("{=s4N0bhG1}Min Level"), 
         LocDescription("{=fozHOKis}The min level it should be (actual value will be randomly selected between min and max, valid values are 0 to 1023)"),
         Range(0, 1023),
         PropertyOrder(2), UsedImplicitly]
        public int MinLevel { get; set; } = 0;

        [LocDisplayName("{=BT59iHQ5}Max Level"), 
         LocDescription("{=I6hTb2OY}The max level it should be (actual value will be randomly selected between min and max, valid values are 0 to 1023)"),
         Range(0, 1023),
         PropertyOrder(3), UsedImplicitly]
        public int MaxLevel { get; set; } = 50;

        [YamlIgnore, Browsable(false)]
        public bool IsFixed => MinLevel == MaxLevel;
        
        public override string ToString()
        {
            return $"{Skill} {MinLevel} - {MaxLevel}";
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}