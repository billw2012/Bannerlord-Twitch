using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;


namespace BLTAdoptAHero.Actions.Util
{   public class KillStreakRewards
    {
        [PropertyOrder(1)]
        public string Name { get; set; }
        [PropertyOrder(2), Description("Text that displays when the kill streak is achieved, replacable values include: {name} for the Kill Streak name, {player} and {kills}.")]
        public string NotificationText { get; set; }
        [PropertyOrder(3), Description("Kills required to achieve the kill streak.")]
        public int KillsRequired { get; set; }
        [PropertyOrder(4), Description("Gold rewarded when the kill streak is achieved.")]
        public int GoldReward { get; set; }
        [PropertyOrder(5), Description("Experience granted when the kill streak is achieved.")]
        public int XPReward { get; set; }

        public KillStreakRewards() { }
        public KillStreakRewards(string text)
        {
            Name = text;
        }

        public override string ToString() => Name;


    }
}
