using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace BannerlordTwitch.Util
{
    public static class ObjectIDRegistry
    {
        //private static readonly Dictionary<object, Guid> registry = new();
        private static readonly Dictionary<long, Guid> registry = new();
        private static readonly ObjectIDGenerator generator = new();
        
        public static Guid Set(object obj, Guid id)
        {
            registry[generator.GetId(obj, out _)] = id;
            return id;
        }

        public static Guid Get(object obj)
        {
            long objHandle = generator.GetId(obj, out _);
            if (registry.TryGetValue(objHandle, out var id))
                return id;
            else
                return Set(objHandle, Guid.NewGuid());
        }
    }
}