using System.ComponentModel;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch
{
    [LocDescription("{=KaFWtYmc}Channel points reward definition")]
    public class Reward : ActionBase
    {
        [LocDisplayName("{=sF4buTM8}Reward Specification"), LocCategory("General", "{=C5T5nnix}General"), 
         LocDescription("{=zq79vZqY}Twitch channel points reward definition"),
         ExpandableObject, Expand, ReadOnly(true), PropertyOrder(1)]
        public RewardSpec RewardSpec { get; set; }

        public override string ToString() => $"{RewardSpec?.Title ?? "unnamed reward"} ({Handler})";
        
        [ItemsSource(typeof(RewardHandlerItemsSource))]
        public override string Handler { get; set; }
    }
}