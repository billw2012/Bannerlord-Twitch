using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace BannerlordTwitch.UI
{
    public class RangeIntEditor : TypeEditor<RangeIntControl>
    {
        protected override void SetValueDependencyProperty()
        {
            ValueProperty = RangeIntControl.RangeIntProperty;
        }
    }
}