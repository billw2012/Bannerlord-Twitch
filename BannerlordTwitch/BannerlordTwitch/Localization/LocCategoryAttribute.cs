using System;
using System.ComponentModel;
using BannerlordTwitch.Util;

namespace BannerlordTwitch.Localization
{
    [AttributeUsage(AttributeTargets.All)]
    public class LocCategoryAttribute : CategoryAttribute
    {
        private readonly string name;

        public LocCategoryAttribute(string category, string name) : base(category)
        {
            this.name = name;
        }

        protected override string GetLocalizedString(string value) => name.Translate();
    }
}