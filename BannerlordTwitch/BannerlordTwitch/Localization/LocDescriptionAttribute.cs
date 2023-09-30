using System;
using System.ComponentModel;
using BannerlordTwitch.Util;

namespace BannerlordTwitch.Localization
{
    [AttributeUsage(AttributeTargets.All)]
    public class LocDescriptionAttribute : DescriptionAttribute
    {
        public LocDescriptionAttribute(string description) : base(description.Translate()) { }
        // public override string Description => base.Description.Translate();
    }
}