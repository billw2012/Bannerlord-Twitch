using System;
using System.ComponentModel;
using BannerlordTwitch.Util;

namespace BannerlordTwitch.Localization
{
    [AttributeUsage(AttributeTargets.All)]
    public class LocDisplayNameAttribute : DisplayNameAttribute
    {
        public LocDisplayNameAttribute(string displayName) : base(displayName.Translate()) { }
    }
}