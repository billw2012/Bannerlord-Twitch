using System.ComponentModel;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch
{
    [UsedImplicitly]
    public class GlobalConfig
    {
        [Browsable(false)]
        public string Id { get; set; }
        [ExpandableObject, ReadOnly(true)]
        public object Config { get; set; }
        
        public override string ToString() => Id;
    }
}