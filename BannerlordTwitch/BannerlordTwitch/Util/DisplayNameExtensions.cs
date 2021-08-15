using System;
using System.ComponentModel;
using System.Reflection;

namespace BannerlordTwitch.Util
{
    public static class DisplayNameExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            if (fieldInfo == null) return null;
            var attribute = fieldInfo.GetCustomAttribute<DisplayNameAttribute>();
            return attribute?.DisplayName ?? value.ToString().SplitCamelCase();
        }
        
        public static string GetDisplayName(this Type type) 
            => type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
    }
}