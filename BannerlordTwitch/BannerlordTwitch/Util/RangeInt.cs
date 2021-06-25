using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    public class RangeInt
    {
        [PropertyOrder(1), UsedImplicitly]
        public int Min { get; set; }
        [PropertyOrder(2), UsedImplicitly]
        public int Max { get; set; }

        public RangeInt() { }

        public RangeInt(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }
}