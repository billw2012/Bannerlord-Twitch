using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BannerlordTwitch.Util;

namespace BannerlordTwitch
{
    public static class SettingsHelpers
    {
        public static void CallInDepth<T>(object root, Action<T> call)
        {
            var visited = new HashSet<object>();

            var stack = new List<(string route, object item)> {
                ("root", root)
            };
            while (stack.Count > 0)
            {
                (string route, object item) = stack[0];
                stack.RemoveAt(0);

                if (item is T tItem)
                {
                    try
                    {
                        call(tItem);
                    }
                    catch (Exception ex)
                    {
                        Log.Exception($"Call of {typeof(T).Name} failed at {route}, on {item.GetType().Name}", ex);
                    }
                }
                
                if (item is IDictionary dItem)
                {
                    foreach (object key in dItem.Keys.ExceptNull()) // probably doesn't make sense in most cases
                    {
                        if(visited.Add(key))
                            stack.Add((route + ":key", key));
                    }
                    foreach (object value in dItem.Values.ExceptNull())
                    {
                        if(visited.Add(value))
                            stack.Add((route + ":value", value));
                    }
                }
                else if (item is ICollection eItem)
                {
                    foreach (object i in eItem.ExceptNull())
                    {
                        if(visited.Add(i))
                            stack.Add((route + "[*]", i));
                    }
                }
                
                foreach((string r, object o) in item.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => 
                        // Only want writable properties
                        p.CanWrite 
                        // Exclude indexer properties
                        && p.GetMethod.GetParameters().Length == 0)
                    .Where(p => p.GetValue(item) != null)
                    .Select(p => (route + $".{p.Name}", p.GetValue(item))))
                {
                    if(visited.Add(o))
                        stack.Add((r, o));
                }
            }
        }

        public static void MergeCollections<T>(
            ICollection<T> target, 
            IEnumerable<T> source,
            Func<T, T, bool> matchFn
            )
        {
            foreach (var item in source.Where(s => !target.Any(t => matchFn(s, t))))
            {
                target.Add(item);
            }
        }
        
        public static void MergeCollectionsSorted<T>(
            ICollection<T> target, 
            IEnumerable<T> source,
            Func<T, T, bool> matchFn,
            Comparison<T> compareFn
        )
        {
            foreach (var item in source.Where(s => !target.Any(t => matchFn(s, t))))
            {
                target.Add(item);
            }

            var list = target.ToList();
            list.Sort(compareFn);
            target.Clear();
            
            foreach (var item in list)
            {
                target.Add(item);
            }
        }
    }
}