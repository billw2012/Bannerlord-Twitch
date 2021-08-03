using System.Collections.Generic;

namespace BannerlordTwitch.Util
{
    public static class MiscExtensions
    {
        public static string Truncate(this string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }
        
        // Deconstruct a KeyValuePair
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> source, out TKey Key, out TValue Value)
        {
            Key = source.Key;
            Value = source.Value;
        }
    }
}