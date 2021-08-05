using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace BannerlordTwitch.Util
{
    public class DerivedClassCollectionEditor<T> : TypeEditor<CollectionControlButton>
    {
        private static readonly Lazy<IList<Type>> derivedTypes = new(() =>
        {
            var tType = typeof(T);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => !p.IsAbstract && tType.IsAssignableFrom(p))
                .ToList();
        });

        protected override void SetValueDependencyProperty()
        {
            ValueProperty = CollectionControlButton.ItemsSourceProperty;
        }

        protected override void ResolveValueBinding(PropertyItem propertyItem)
        {
            Editor.ItemsSourceType = propertyItem.PropertyType;
            Editor.NewItemTypes = derivedTypes.Value;
            base.ResolveValueBinding(propertyItem);
        }
    }
}