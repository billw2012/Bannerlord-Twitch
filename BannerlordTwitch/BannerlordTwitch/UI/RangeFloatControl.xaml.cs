using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BannerlordTwitch.Util;

namespace BannerlordTwitch.UI
{
    public partial class RangeFloatControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty RangeFloatProperty = DependencyProperty.Register(
            "Value", typeof(RangeFloat), typeof(RangeFloatControl), 
            new PropertyMetadata(default(RangeFloat)));

        public RangeFloat Value
        {
            get => (RangeFloat) GetValue(RangeFloatProperty);
            set => SetValue(RangeFloatProperty, value);
        }
        
        public float Min
        {
            get => Value.Min;
            set => Value = new RangeFloat(value, Max);
        }
        
        public float Max
        {
            get => Value.Max;
            set => Value = new RangeFloat(Min, value);
        }

        public string MinLabel => "{=TZN2VR8Q}Min".Translate();
        public string MaxLabel => "{=bySwFF1n}Max".Translate();
        
        public RangeFloatControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}