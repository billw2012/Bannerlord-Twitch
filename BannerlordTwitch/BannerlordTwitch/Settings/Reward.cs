using System.ComponentModel;
using BannerlordTwitch.Util;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch
{
    [Description("Channel points reward definition")]
    public class Reward : ActionBase
    {
        [Category("General"), 
         Description("Twitch channel points reward definition"),
         ExpandableObject, Expand, ReadOnly(true), PropertyOrder(1)]
        public RewardSpec RewardSpec { get; set; }

        public override string ToString() => $"{RewardSpec?.Title ?? "unnamed reward"} ({Handler})";
        
        [ItemsSource(typeof(RewardHandlerItemsSource))]
        public override string Handler { get; set; }
    }
}