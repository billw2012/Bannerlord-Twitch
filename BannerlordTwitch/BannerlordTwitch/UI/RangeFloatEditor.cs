using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace BannerlordTwitch.UI
{
    public class RangeFloatEditor : TypeEditor<RangeFloatControl>
    {
        protected override void SetValueDependencyProperty()
        {
            ValueProperty = RangeFloatControl.RangeFloatProperty;
        }
    }
}