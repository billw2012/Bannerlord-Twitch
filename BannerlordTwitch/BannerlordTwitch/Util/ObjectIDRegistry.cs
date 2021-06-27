using System;
using System.Collections.Generic;
using System.Linq;

namespace BannerlordTwitch.Util
{
    public static class ObjectIDRegistry
    {
        private static readonly Dictionary<object, Guid> registry = new();
        
        public static Guid Set(object obj, Guid id)
        {
            registry[obj] = id;
            return id;
        }

        public static Guid Get(object obj)
        {
            return registry.TryGetValue(obj, out var id) 
                ? id 
                : Set(obj, Guid.NewGuid());
        }
    }
}