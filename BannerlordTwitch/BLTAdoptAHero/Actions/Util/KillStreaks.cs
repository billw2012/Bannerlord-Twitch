using System;
using System.ComponentModel;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Actions.Util
{ 
    public class KillStreakRewards : INotifyPropertyChanged
    {
        [PropertyOrder(0), UsedImplicitly]
        public bool Enabled { get; set; }
        
        [PropertyOrder(2), UsedImplicitly]
        public bool ShowNotification { get; set; }
        
        [PropertyOrder(3), UsedImplicitly]
        public string Name { get; set; } = "New Kill Streak";
        
        [PropertyOrder(4), 
         Description("Text that displays when the kill streak is achieved, replacable values include: {name} for the " +
                     "Kill Streak name, {player} and {kills}."), UsedImplicitly]
        public string NotificationText { get; set; }
        
        [PropertyOrder(5), Description("Kills required to achieve the kill streak."), UsedImplicitly]
        public int KillsRequired { get; set; }
        
        [PropertyOrder(6), Description("Gold rewarded when the kill streak is achieved."), UsedImplicitly]
        public int GoldReward { get; set; }
        
        [PropertyOrder(7), Description("Experience granted when the kill streak is achieved."), UsedImplicitly]
        public int XPReward { get; set; }

        public override string ToString() => Name;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
