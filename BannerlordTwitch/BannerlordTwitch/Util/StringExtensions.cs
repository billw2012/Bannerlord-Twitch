using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BannerlordTwitch.Localization;
using TaleWorlds.Localization;

namespace BannerlordTwitch.Util
{
    public static class StringExtensions
    {
        public static string SplitCamelCase(this string input)
            => Regex.Replace(
                Regex.Replace(input, "([a-z0-9])([A-Z])", "$1 $2"),
                "([A-Z])([A-Z])([a-z0-9])", "$1 $2$3")
                .Trim();

        public static string Translate(this string input) 
            => LocString.Translate(input);

        public static string Translate(this string input, params (string key, object value)[] args) 
            => LocString.Translate(input, args);
    }
}