using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BannerlordTwitch.Util;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch
{
    public static class DocumentationHelpers
    {
        public static void AutoDocument(IDocumentationGenerator generator, object obj)
        {
            if (obj is bool b)
            {
                generator.P(b ? "Enabled" : "Disabled");
            }
            else if (obj.GetType().IsPrimitive || obj.GetType().IsEnum || obj is string)
            {
                generator.P(obj.ToString());
            }
            else if (obj is IEnumerable col)
            {
                AutoDocumentCollection(generator, col);
            }
            else
            {
                AutoDocumentObject(generator, obj);
            }
        }

        private static void AutoDocumentCollection(IDocumentationGenerator generator, IEnumerable obj)
        {
            generator.Table(() =>
            {
                foreach (object o in obj)
                {
                    generator.TR(() => generator.TD(() => AutoDocument(generator, o)));
                }
            });
        }
        
        private static void AutoDocumentObject(IDocumentationGenerator generator, object obj)
        {
            var objType = obj.GetType();

            var categoryOrders = objType
                .GetCustomAttributes(inherit: true)
                .OfType<CategoryOrderAttribute>()
                .ToList();

            var properties = objType.GetProperties()
                .Select(p => (p, doc: p.GetCustomAttribute<DocumentAttribute>()))
                .Where(p => p.doc != null)
                .GroupBy(p => p.p.GetCustomAttribute<CategoryAttribute>())
                .OrderBy(g => categoryOrders.FirstOrDefault(c => c.Category == g.Key?.Category)?.Order ?? -int.MaxValue);
            
            foreach (var c in properties)
            {
                if (c.Key?.Category != null)
                {
                    generator.H2(c.Key.Category);
                }
                generator.Table(() =>
                {
                    var categoryItems = c.OrderBy(p =>
                        p.p.GetCustomAttribute<PropertyOrderAttribute>()?.Order ?? -int.MaxValue);
                    foreach (var p in categoryItems)
                    {
                        string name = p.doc?.Name ?? p.p.Name.SplitCamelCase();
                        string desc = p.doc?.Description ?? p.p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
                        object value = p.p.GetValue(obj);

                        generator.TR(() =>
                        {
                            generator.TD(name);
                            generator.TD(() =>
                            {
                                if (value != null) AutoDocument(generator, value);
                            });
                            generator.TD(desc);
                        });
                    }
                });
            }
        }
    }
}