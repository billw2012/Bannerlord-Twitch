using System;

namespace BannerlordTwitch.Util
{
    public static class ConfigureContext
    {
        public static Settings DefaultSettings { get; set; }
        public static Settings CurrentlyEditedSettings { get; set; }
    }
}