using System;
using System.Collections.Generic;
using System.ComponentModel;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BannerlordTwitch.Helpers
{
    public class PropertyModifierDef : ICloneable, INotifyPropertyChanged
    {
        [Description("The property to modify"),
         ItemsSource(typeof(DrivenPropertyItemSource)),
         PropertyOrder(1), UsedImplicitly]
        public DrivenProperty Name { get; set; } = DrivenProperty.ArmorHead;

        [Description("Percent modifier to apply to the property (100% will result in no chance)"),
         PropertyOrder(2),
         UIRange(0, 1000, 5f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly, Document]
        public float ModifierPercent { get; set; } = 100f;
        
        [Description("How much to add or subtract from the property (0 will result in no change). " +
                     "This is applied AFTER Modifier."),
         PropertyOrder(3),
         UsedImplicitly, Document]
        public float Add { get; set; }
        
        public float Apply(float property) => property * ModifierPercent / 100f + Add;
        
        public override string ToString()
        {
            string result = $"{DrivenPropertyItemSource.GetFriendlyName(Name)}: ";
            if (ModifierPercent != 100) result += $"{ModifierPercent}% ";
            if (Add != 0) result += Add > 0 ? $"+{Add}" : $"{Add}";
            if (ModifierPercent == 100 && Add == 0)
            {
                result += $"(no change)";
            }
            return result;
        }

        public object Clone() => CloneHelpers.CloneProperties(this);
        
        public event PropertyChangedEventHandler PropertyChanged;
    }
}