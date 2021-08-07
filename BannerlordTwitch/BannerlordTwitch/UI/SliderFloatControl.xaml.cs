using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace BannerlordTwitch.UI
{
    public partial class SliderFloatControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty SliderFloatProperty = DependencyProperty.Register(
            "Value", typeof(float), typeof(SliderFloatControl), 
            new PropertyMetadata(default(float)));

        public float Value
        {
            get => (float) GetValue(SliderFloatProperty);
            set => SetValue(SliderFloatProperty, value);
        }
        
        public float Minimum { get; set; }
        
        public float Maximum { get; set; }

        public float Interval { get; set; } = 0.05f;
        
        public SliderFloatControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}