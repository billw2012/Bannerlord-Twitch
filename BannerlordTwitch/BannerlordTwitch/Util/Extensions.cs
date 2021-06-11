using System.Reflection;
using HarmonyLib;
using TaleWorlds.Localization;

namespace BannerlordTwitch.Util
{
    public static class Extensions
    {
        private static readonly FieldInfo TextObjectValue = AccessTools.Field(typeof(TextObject), "Value");
        public static string Raw(this TextObject obj) => (string)TextObjectValue.GetValue(obj);
    }
}