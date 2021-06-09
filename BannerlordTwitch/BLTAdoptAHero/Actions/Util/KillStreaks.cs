using System.ComponentModel;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Actions.Util
{ 
    public class KillStreakRewards
    {
        [PropertyOrder(0)]
        public bool Enabled { get; [UsedImplicitly] set; }
        [PropertyOrder(1)]
        public string Name { get; [UsedImplicitly] set; }
        [PropertyOrder(2), Description("Text that displays when the kill streak is achieved, replacable values include: {name} for the Kill Streak name, {player} and {kills}.")]
        public string NotificationText { get; [UsedImplicitly] set; }
        [PropertyOrder(3), Description("Kills required to achieve the kill streak.")]
        public int KillsRequired { get; [UsedImplicitly] set; }
        [PropertyOrder(4), Description("Gold rewarded when the kill streak is achieved.")]
        public int GoldReward { get; [UsedImplicitly] set; }
        [PropertyOrder(5), Description("Experience granted when the kill streak is achieved.")]
        public int XPReward { get; [UsedImplicitly] set; }

        public override string ToString() => Name;
    }
}
