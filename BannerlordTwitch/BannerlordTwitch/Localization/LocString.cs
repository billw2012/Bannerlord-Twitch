using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using BannerlordTwitch.Annotations;
using TaleWorlds.Localization;

namespace BannerlordTwitch.Localization
{
    [TypeConverter(typeof(LocStringConverter))]
    public class LocString
    {
        public string Value { get; set; }

        [UsedImplicitly]
        public LocString() {}
        
        public LocString(string value)
        {
            Value = value;
        }
        
        public static implicit operator LocString(string str) => new(str);

        public override string ToString() => Translate(Value);
        
        public string ToString(params (string key, object value)[] arg) => Translate(Value, arg);

        public static bool IsNullOrEmpty(LocString ls) => ls == null || string.IsNullOrEmpty(ls.Value);

        public static string Translate(string str) 
            => !string.IsNullOrEmpty(str)
                ? new TextObject(str).ToString()
                : string.Empty;

        public static string Translate(string str, params (string key, object value)[] arg) 
            => !string.IsNullOrEmpty(str)
                ? new TextObject(str, arg.ToDictionary(
                    kv => kv.key,
                    kv => kv.value is LocString ? kv.value.ToString() : kv.value)).ToString()
                : string.Empty;
    }

    public class LocStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }
        
        public override bool CanConvertTo(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context,
            CultureInfo culture, object value)
        {
            return value is string 
                ? new LocString(value.ToString()) 
                : base.ConvertFrom(context, culture, value);
        }
        
        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            return destinationType == typeof(string) 
                ? ((LocString)value).ToString() 
                : base.ConvertTo(context, culture, value, destinationType);
        }
    }
}