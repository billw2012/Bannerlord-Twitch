using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using JetBrains.Annotations;

namespace BLTConfigure
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    [UsedImplicitly]
    public sealed class NonDefaultVisibilityConverter : IValueConverter
    {
        public object Default { get; set; }
        public Visibility DefaultVisibility { get; set; }
        public Visibility NonDefaultVisibility { get; set; }

        public NonDefaultVisibilityConverter()
        {
            // set defaults
            DefaultVisibility = Visibility.Visible;
            NonDefaultVisibility = Visibility.Collapsed;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DefaultVisibility;
            var converter = TypeDescriptor.GetConverter(value.GetType());
            object defaultTyped = converter.ConvertFrom(Default);
            return value.Equals(defaultTyped) ? DefaultVisibility : NonDefaultVisibility;    
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}