using System.ComponentModel;
using JetBrains.Annotations;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    public struct RangeInt
    {
        [PropertyOrder(1), UsedImplicitly]
        public int Min { get; set; }
        [PropertyOrder(2), UsedImplicitly]
        public int Max { get; set; }
        
        [YamlIgnore, Browsable(false)]
        public bool IsFixed => Min == Max;

        public RangeInt(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public int RandomInRange() => IsFixed ? Min : MBRandom.RandomInt(Min, Max);
        
        public override string ToString() => $"{Min} - {Max}";
    }
}