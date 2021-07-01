using System.Linq;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace BLTAdoptAHero.Powers
{
    internal class HeroPowerCollectionEditor : TypeEditor<CollectionControlButton>
    {
        protected override void SetValueDependencyProperty()
        {
            ValueProperty = CollectionControlButton.ItemsSourceProperty;
        }

        protected override void ResolveValueBinding(PropertyItem propertyItem)
        {
            var type = propertyItem.PropertyType;
            Editor.ItemsSourceType = type;
            Editor.NewItemTypes = HeroPowerDefBase.RegisteredPowerDefTypes.ToList();
            base.ResolveValueBinding(propertyItem);
        }
    }
}