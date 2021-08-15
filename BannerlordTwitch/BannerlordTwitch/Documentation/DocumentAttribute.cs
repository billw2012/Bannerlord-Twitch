using System;
using BannerlordTwitch.Util;

namespace BannerlordTwitch
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DocumentAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public DocumentAttribute(string name = null, string description = null)
        {
            Name = name.Translate();
            Description = description.Translate();
        }
    }
}