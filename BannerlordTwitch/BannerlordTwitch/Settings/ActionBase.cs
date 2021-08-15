using System;
using System.ComponentModel;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
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

        [LocDisplayName("{=sPKWnVA0}Enabled"), LocCategory("General", "{=C5T5nnix}General"), 
         LocDescription("{=YM1rP7sP}Whether this is enabled or not"), 
         PropertyOrder(-100), UsedImplicitly]
        public bool Enabled { get; set; }
        [LocDisplayName("{=86rc4cz6}Respond In Twitch"), LocCategory("General", "{=C5T5nnix}General"), 
         LocDescription("{=7IBsjc51}Show response in the twitch chat"), 
         PropertyOrder(-99), UsedImplicitly]
        public bool RespondInTwitch { get; set; }
        [LocDisplayName("{=0MgBhdtw}Respond In Overlay"), LocCategory("General", "{=C5T5nnix}General"), 
         LocDescription("{=UQRfOFjs}Show response in the overlay window feed"), 
         PropertyOrder(-98), UsedImplicitly]
        public bool RespondInOverlay { get; set; }
        
        [LocDisplayName("{=dteVl09D}Handler"), LocCategory("General", "{=C5T5nnix}General"), 
         LocDescription("{=ErhzbqFu}Name of the handler"), 
         ReadOnly(true), PropertyOrder(1), UsedImplicitly, 
         SuppressPropertyChangedWarnings]
        public abstract string Handler { get; set; }

        [LocDisplayName("{=Zj8ni08E}Handler Config"), LocCategory("General", "{=C5T5nnix}General"), 
         LocDescription("{=cdEbk84n}Custom config for the handler"),
         ExpandableObject, Expand, ReadOnly(true), 
         PropertyOrder(2), UsedImplicitly]
        public object HandlerConfig { get; set; }
        
        [LocDisplayName("{=UP0DjNMM}Documentation"), LocCategory("General", "{=C5T5nnix}General"), 
         LocDescription("{=W6pzg6VJ}What to show in the generated documentation"), 
         PropertyOrder(3), UsedImplicitly]
        public LocString Documentation { get; set; }

        [YamlIgnore, Browsable(false), UsedImplicitly]
        public virtual bool IsValid => !Enabled || !string.IsNullOrEmpty(Handler);

        public event PropertyChangedEventHandler PropertyChanged;
    }
}