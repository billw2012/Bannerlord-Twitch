using System.Collections.Generic;

namespace BannerlordTwitch.Util
{
    public static class MiscExtensions
    {
        public static string Truncate(this string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }
        
        
        public static void AddInt<K>(this Dictionary<K, int> @this, K key, int valueToAdd)
        {
            @this.TryGetValue(key, out int currValue);
            @this[key] = currValue + valueToAdd;
        }
        
        public static int GetInt<K>(this Dictionary<K, int> @this, K key, int defaultValue = 0)
        {
            return @this.TryGetValue(key, out int currValue) ? currValue : defaultValue;
        }
        
        // Deconstruct a KeyValuePair
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> source, out TKey Key, out TValue Value)
        {
            Key = source.Key;
            Value = source.Value;
        }
    }
}