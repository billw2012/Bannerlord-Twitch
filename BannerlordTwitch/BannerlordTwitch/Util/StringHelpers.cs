using System.Text.RegularExpressions;

namespace BannerlordTwitch.Util
{
    public static class StringHelpers
    {
        public static string SplitCamelCase(this string input)
            => Regex.Replace(
                Regex.Replace(input, "([a-z0-9])([A-Z])", "$1 $2"), 
                "([A-Z])([a-z0-9])", " $1$2")
                .Trim();
    }
}