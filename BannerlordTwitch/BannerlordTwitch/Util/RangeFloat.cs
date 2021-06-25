using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    public class RangeFloat
    {
        [PropertyOrder(1), UsedImplicitly]
        public float Min { get; set; }
        [PropertyOrder(2), UsedImplicitly]
        public float Max { get; set; }

        public RangeFloat() {}
        
        public RangeFloat(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}