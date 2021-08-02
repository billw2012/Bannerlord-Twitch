using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BannerlordTwitch.Util
{
    public static class CloneHelpers
    {
        public static T CloneProperties<T>(T from)
        {
            var newObj = (T)Activator.CreateInstance(from.GetType()); // still use GetType, in-case T is a base class 
            CloneProperties(from, newObj);
            return newObj;
        }

        public static void CloneProperties(object from, object to)
        {
            foreach(var pi in from.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => 
                    // Only want writable properties
                    p.CanWrite 
                    // Exclude indexer properties
                    && p.GetMethod.GetParameters().Length == 0))
            {
                if (typeof(ICloneable).IsAssignableFrom(pi.PropertyType))
                {
                    pi.SetValue(to, ((ICloneable) pi.GetValue(from))?.Clone());
                }
                else
                {
                    pi.SetValue(to, pi.GetValue(from));
                }
            }
        }
        
        // public static T CloneFields<T>(T from)
        // {
        //     var newObj = (T)Activator.CreateInstance(from.GetType()); // still use GetType, in-case T is a base class 
        //     CloneFields(from, newObj);
        //     return newObj;
        // }
        //
        // public static void CloneFields(object from, object to)
        // {
        //     var type = from.GetType();
        //     while (type != null)
        //     {
        //         var fields = type
        //             .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        //
        //         foreach (var field in fields)
        //         {
        //             if (typeof(ICloneable).IsAssignableFrom(field.FieldType))
        //             {
        //                 field.SetValue(to, ((ICloneable) field.GetValue(from))?.Clone());
        //             }
        //             else
        //             {
        //                 field.SetValue(to, field.GetValue(from));
        //             }
        //         }
        //
        //         type = type.BaseType;
        //     }
        // }
        
        public static IEnumerable<T> CloneCollection<T>(IEnumerable<T> from) =>
            @from.Select(o =>
            {
                if(o is ICloneable c)
                {
                    return (T)c.Clone();
                }
                else
                {
                    return CloneProperties(o);
                }
            });
    }
}