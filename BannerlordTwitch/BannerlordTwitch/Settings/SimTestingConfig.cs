using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch.UI;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BannerlordTwitch
{
    [UsedImplicitly]
    public class SimTestingConfig : IUpdateFromDefault
    {
        #region User Editable
        [PropertyOrder(1), UsedImplicitly]
        public int UserCount { get; set; }
        [PropertyOrder(2), UsedImplicitly]
        public int UserStayTime { get; set; }
        [PropertyOrder(3), UsedImplicitly]
        public int IntervalMinMS { get; set; }
        [PropertyOrder(4), UsedImplicitly]
        public int IntervalMaxMS { get; set; }
        [PropertyOrder(5),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         UsedImplicitly]
        public ObservableCollection<SimTestingItem> Init { get; set; } = new();
        [PropertyOrder(6),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         UsedImplicitly]
        public ObservableCollection<SimTestingItem> Use { get; set; } = new();
        #endregion

        #region Public Interface
        [YamlIgnore, Browsable(false)]
        public IEnumerable<SimTestingItem> InitEnabled 
            => Init?.Where(i => i.Enabled) ?? Enumerable.Empty<SimTestingItem>();
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<SimTestingItem> UseEnabled 
            => Use?.Where(i => i.Enabled) ?? Enumerable.Empty<SimTestingItem>();

        public override string ToString() => "Sim Testing Config";
        #endregion
        
        #region IUpdateFromDefault
        public void OnUpdateFromDefault(Settings defaultSettings)
        {
            Init ??= new();
            Use ??= new();

            SettingsHelpers.MergeCollections(
                Init, 
                defaultSettings.SimTesting.Init,
                (a, b) => a.ID == b.ID
            );
            SettingsHelpers.MergeCollections(
                Use, 
                defaultSettings.SimTesting.Use,
                (a, b) => a.ID == b.ID
            );
        }
        #endregion
    }
}