using System.Text.RegularExpressions;

namespace BannerlordTwitch.Util
{
    public static class StringHelpers
    {
        public static string SplitCamelCase(this string input)
            => Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
    }
}