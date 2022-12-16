using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace BannerlordTwitch.UI
{
    [AttributeUsage(AttributeTargets.Property)]
    public class InstanceNameAttribute : Attribute { }
    
    public class DefaultCollectionEditor : TypeEditor<CollectionPropertyEditor>
    {
        protected override void SetValueDependencyProperty()
        {
            ValueProperty = CollectionPropertyEditor.ItemsSourceProperty;
        }

        protected override void ResolveValueBinding(PropertyItem propertyItem)
        {
            Editor.PropertyName = GetQualifiedName(propertyItem); 
            // propertyItem.PropertyDescriptor?.Name.SplitCamelCase();
            //Editor.ItemsSource = propertyItem.
            Editor.ItemsSourceType = propertyItem.PropertyType;
            // Editor.NewItemTypes = derivedTypes.Value;
            base.ResolveValueBinding(propertyItem);
        }

        public static string GetObjectName(object obj)
        {
            var prop = obj.GetType().GetProperties().FirstOrDefault(p => p.GetCustomAttribute<InstanceNameAttribute>() != null);
            return prop?.GetValue(obj)?.ToString() ?? obj.ToString();
        }

        public static string GetQualifiedName(PropertyItem propertyItem)
        {
            var parentItems = new List<string> { $"{propertyItem.DisplayName}" };
            var parent = propertyItem.ParentElement;
            while (parent is PropertyItem parentItem)
            {
                parentItems.Add(parentItem.DisplayName);
                parent = parentItem.ParentElement;
            }

            if (parent is PropertyGrid propertyGrid)
            {
                parentItems.Add(GetObjectName(propertyGrid.SelectedObject));
            }
            
            parentItems.Reverse();
            var qualifiedName = string.Join(" > ", parentItems);
            return qualifiedName;
        }
    }
}