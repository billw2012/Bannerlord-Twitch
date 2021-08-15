using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BannerlordTwitch.Util;

namespace BannerlordTwitch.UI
{
    public partial class RangeIntControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty RangeIntProperty = DependencyProperty.Register(
            "Value", typeof(RangeInt), typeof(RangeIntControl), 
            new PropertyMetadata(default(RangeInt)));

        public RangeInt Value
        {
            get => (RangeInt) GetValue(RangeIntProperty);
            set => SetValue(RangeIntProperty, value);
        }
        
        public int Min
        {
            get => Value.Min;
            set => Value = new RangeInt(value, Max);
        }
        
        public int Max
        {
            get => Value.Max;
            set => Value = new RangeInt(Min, value);
        }
        
        public string MinLabel => "{=TZN2VR8Q}Min".Translate();
        public string MaxLabel => "{=bySwFF1n}Max".Translate();
        
        public RangeIntControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}