using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BannerlordTwitch
{
    [UsedImplicitly]
    public class SimTestingConfig
    {
        [PropertyOrder(1), UsedImplicitly]
        public int UserCount { get; set; }
        [PropertyOrder(2), UsedImplicitly]
        public int UserStayTime { get; set; }
        [PropertyOrder(3), UsedImplicitly]
        public int IntervalMinMS { get; set; }
        [PropertyOrder(4), UsedImplicitly]
        public int IntervalMaxMS { get; set; }
        [PropertyOrder(5), UsedImplicitly]
        public ObservableCollection<SimTestingItem> Init { get; set; }

        [YamlIgnore, Browsable(false)]
        public IEnumerable<SimTestingItem> InitEnabled 
            => Init?.Where(i => i.Enabled) ?? Enumerable.Empty<SimTestingItem>();
        
        [PropertyOrder(6), UsedImplicitly]
        public ObservableCollection<SimTestingItem> Use { get; set; }
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<SimTestingItem> UseEnabled 
            => Use?.Where(i => i.Enabled) ?? Enumerable.Empty<SimTestingItem>();

        public override string ToString() => "Sim Testing Config";
    }
}