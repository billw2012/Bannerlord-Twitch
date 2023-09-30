using System;
using System.ComponentModel;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Actions.Util
{ 
    [LocDisplayName("{=InTnlqz1}Kill Streak")]
    public class KillStreakDef : INotifyPropertyChanged
    {
        [UsedImplicitly, ReadOnly(true)]
        public Guid ID { get; set; } = Guid.NewGuid();
        
        [LocDisplayName("{=sPKWnVA0}Enabled"),
         PropertyOrder(0), UsedImplicitly]
        public bool Enabled { get; set; }
        
        [LocDisplayName("{=poe8m8rn}Show Notification"),
         PropertyOrder(2), UsedImplicitly]
        public bool ShowNotification { get; set; }
        
        [LocDisplayName("{=uUzmy7Lh}Name"),
         InstanceName, PropertyOrder(3), UsedImplicitly]
        public LocString Name { get; set; } = "{=KtvBk2nR}New Kill Streak";
        
        [LocDisplayName("{=RQh3jIZW}Notification Text"),
         LocDescription("{=04wTH1xo}Text that displays when the kill streak is achieved. Placeholders: [viewer] for the viewers name, and [name] for the Kill Streak name, and [kills] for the Kills Required."), 
         PropertyOrder(4), UsedImplicitly]
        public LocString NotificationText { get; set; } = "{=ipowOInD}[viewer] got [name] ([kills] kills)";
        
        [LocDisplayName("{=mG7HzT0z}Kills Required"),
         LocDescription("{=mLElTwb6}Kills required to achieve the kill streak."), 
         PropertyOrder(5), UsedImplicitly]
        public int KillsRequired { get; set; }
        
        [LocDisplayName("{=zEqJgISl}Gold Reward"),
         LocDescription("{=CYg79sAT}Gold rewarded when the kill streak is achieved."), 
         PropertyOrder(6), UsedImplicitly]
        public int GoldReward { get; set; }
        
        [LocDisplayName("{=vzCgh9PN}XP Reward"),
         LocDescription("{=Q1TcztBP}Experience granted when the kill streak is achieved."), 
         PropertyOrder(7), UsedImplicitly]
        public int XPReward { get; set; }

        [YamlIgnore, Browsable(false)]
        public string RewardsDescription => (GoldReward > 0 ? $"{GoldReward}{Naming.Gold} " : "") + (XPReward > 0 ? $"{XPReward}{Naming.XP} " : "");

        public override string ToString() 
            => $"{Name} " +
               "{=YZJ60Pxb}{KillsRequired} kills".Translate(("KillsRequired", KillsRequired)) +
               " " + RewardsDescription;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
