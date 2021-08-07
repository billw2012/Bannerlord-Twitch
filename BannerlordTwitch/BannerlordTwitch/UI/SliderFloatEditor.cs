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
                .OfType<UIRangeAttribute>().FirstOrDefault();
            if( rangeAttribute != null )
            {
                var converter = TypeDescriptor.GetConverter(typeof(float));
                Editor.Minimum = (float)converter.ConvertFrom( rangeAttribute.Minimum.ToString());
                Editor.Maximum = (float)converter.ConvertFrom( rangeAttribute.Maximum.ToString());
                Editor.Interval = (float)converter.ConvertFrom( rangeAttribute.Interval.ToString());
            }
            else
            {
                var range2Attribute = propertyItem.PropertyDescriptor?.Attributes
                    .OfType<RangeAttribute>().FirstOrDefault();
                if( range2Attribute != null )
                {
                    var converter = TypeDescriptor.GetConverter(typeof(float));
                    Editor.Minimum = (float)converter.ConvertFrom( range2Attribute.Minimum.ToString());
                    Editor.Maximum = (float)converter.ConvertFrom( range2Attribute.Maximum.ToString());
                }
            }
        }
    }
}