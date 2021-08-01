using System.ComponentModel;
using JetBrains.Annotations;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    public struct RangeFloat
    {
        [PropertyOrder(1), UsedImplicitly]
        public float Min { get; set; }
        [PropertyOrder(2), UsedImplicitly]
        public float Max { get; set; }

        [YamlIgnore, Browsable(false)]
        public bool IsFixed => Min == Max;

        public RangeFloat(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float RandomInRange() => IsFixed ? Min : MBRandom.RandomFloatRanged(Min, Max);

        public override string ToString() => $"{Min} - {Max}";
    }
}