using System;

namespace BannerlordTwitch.UI
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class UIRangeAttribute : Attribute
    {
        public float Minimum;
        public float Maximum;
        public float Interval;

        public UIRangeAttribute(float minimum, float maximum, float interval)
        {
            Minimum = minimum;
            Maximum = maximum;
            Interval = interval;
        }
    }
}