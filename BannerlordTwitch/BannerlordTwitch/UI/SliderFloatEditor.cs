using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace BannerlordTwitch.UI
{
    public class SliderFloatEditor : TypeEditor<SliderFloatControl>
    {
        protected override void SetValueDependencyProperty()
        {
            ValueProperty = SliderFloatControl.SliderFloatProperty;
        }

        protected override void SetControlProperties(PropertyItem propertyItem)
        {
            base.SetControlProperties(propertyItem);

            var rangeAttribute = propertyItem.PropertyDescriptor?.Attributes
                .OfType<RangeAttribute>().FirstOrDefault();
            if( rangeAttribute != null )
            {
                var converter = TypeDescriptor.GetConverter(typeof(float));
                Editor.Maximum = (float)converter.ConvertFrom( rangeAttribute.Maximum.ToString());
                Editor.Minimum = (float)converter.ConvertFrom( rangeAttribute.Minimum.ToString());
            }
        }
    }
}