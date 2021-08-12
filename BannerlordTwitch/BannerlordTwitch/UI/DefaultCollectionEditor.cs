using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using BannerlordTwitch.Util;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace BannerlordTwitch.UI
{
    public class DefaultCollectionEditor : TypeEditor<CollectionPropertyEditor>
    {
        protected override void SetValueDependencyProperty()
        {
            ValueProperty = CollectionPropertyEditor.ItemsSourceProperty;
        }

        protected override void ResolveValueBinding(PropertyItem propertyItem)
        {
            Editor.PropertyName = propertyItem.PropertyDescriptor?.Name.SplitCamelCase();
            //Editor.ItemsSource = propertyItem.
            Editor.ItemsSourceType = propertyItem.PropertyType;
            // Editor.NewItemTypes = derivedTypes.Value;
            base.ResolveValueBinding(propertyItem);
        }
    }
}