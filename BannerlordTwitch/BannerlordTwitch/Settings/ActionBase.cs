using System;
using System.ComponentModel;
using JetBrains.Annotations;
using PropertyChanged;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BannerlordTwitch
{
    [CategoryOrder("General", 0)]
    public abstract class ActionBase : INotifyPropertyChanged
    {
        // Unique ID for this action 
        [ReadOnly(true), UsedImplicitly]
        public Guid ID { get; set; } = Guid.NewGuid();

        [Category("General"), Description("Whether this is enabled or not"), PropertyOrder(-100), UsedImplicitly]
        public bool Enabled { get; set; }
        [Category("General"), Description("Show response in the twitch chat"), PropertyOrder(-99), UsedImplicitly]
        public bool RespondInTwitch { get; set; }
        [Category("General"), 
         Description("Show response in the overlay window feed"), PropertyOrder(-98), UsedImplicitly]
        public bool RespondInOverlay { get; set; }
        
        [Category("General"), 
         Description("Name of the handler"), ReadOnly(true), PropertyOrder(1), UsedImplicitly, 
         SuppressPropertyChangedWarnings]
        public abstract string Handler { get; set; }

        [Category("General"), 
         Description("Custom config for the handler"), ExpandableObject, ReadOnly(true), 
         PropertyOrder(2), UsedImplicitly]
        public object HandlerConfig { get; set; }
        
        [Category("General"), 
         Description("What to show in the generated documentation"), PropertyOrder(3), UsedImplicitly]
        public string Documentation { get; set; }

        [YamlIgnore, Browsable(false), UsedImplicitly]
        public virtual bool IsValid => !Enabled || !string.IsNullOrEmpty(Handler);

        public event PropertyChangedEventHandler PropertyChanged;
    }
}