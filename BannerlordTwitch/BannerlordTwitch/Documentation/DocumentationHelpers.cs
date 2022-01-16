using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BannerlordTwitch.Localization;
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
                generator.P(b ? "{=kpv03CqK}Enabled".Translate() : "{=soS8qlsg}Disabled".Translate());
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
                        string name = p.p.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName?.Translate() 
                                      ?? p.p.Name.SplitCamelCase();
                        string desc = p.p.GetCustomAttribute<DescriptionAttribute>()?.Description?.Translate() 
                                      ?? string.Empty;
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
        
        public static IDocumentationGenerator PropertyValuePair(this IDocumentationGenerator generator, 
            string property, string value)
        {
            return generator.Div("value", () => generator.P($"<strong class=\"value\">{property}</strong>: {value}"));
        }
        
        public static IDocumentationGenerator PropertyValuePair(this IDocumentationGenerator generator, 
            string property, Action content)
        {
            return generator.Div("value", () =>
            {
                generator.P($"<strong class=\"value\">{property}</strong>:");
                content();
            });
        }
        
        public static IDocumentationGenerator Value(this IDocumentationGenerator generator, string value)
        {
            return generator.P("value", value);
        }        
        
        public static IDocumentationGenerator Value(this IDocumentationGenerator generator, Action content)
        {
            return generator.Div("value", content);
        }
    }
}