using System;
using System.ComponentModel;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Actions.Util
{ 
    public class KillStreakDef : INotifyPropertyChanged
    {
        [UsedImplicitly, ReadOnly(true)]
        public Guid ID { get; set; } = Guid.NewGuid();
        
        [PropertyOrder(0), UsedImplicitly]
        public bool Enabled { get; set; }
        
        [PropertyOrder(2), UsedImplicitly]
        public bool ShowNotification { get; set; }
        
        [InstanceName, PropertyOrder(3), UsedImplicitly]
        public string Name { get; set; } = "New Kill Streak";
        
        [PropertyOrder(4), 
         Description("Text that displays when the kill streak is achieved. " +
                     "Placeholders: {viewer} for the viewers name, and {name} for the Kill Streak name, and {kills} " +
                     "for the Kills Required."), UsedImplicitly]
        public string NotificationText { get; set; }
        
        [PropertyOrder(5), Description("Kills required to achieve the kill streak."), UsedImplicitly]
        public int KillsRequired { get; set; }
        
        [PropertyOrder(6), Description("Gold rewarded when the kill streak is achieved."), UsedImplicitly]
        public int GoldReward { get; set; }
        
        [PropertyOrder(7), Description("Experience granted when the kill streak is achieved."), UsedImplicitly]
        public int XPReward { get; set; }

        public string RewardsDescription => (GoldReward > 0 ? $"{GoldReward}{Naming.Gold} " : "") + (XPReward > 0 ? $"{XPReward}{Naming.XP} " : "");

        public override string ToString() => $"{Name} {KillsRequired} kills " + RewardsDescription;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
