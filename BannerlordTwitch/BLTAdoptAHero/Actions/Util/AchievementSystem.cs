using System.ComponentModel;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System;
using BannerlordTwitch.Util;

namespace BLTAdoptAHero.Actions.Util
{
    public class AchievementSystem
    {
        [ReadOnly(true), UsedImplicitly]
        public Guid ID { get; set; } //{ get => ObjectIDRegistry.Get(this); set => ObjectIDRegistry.Set(this, value); }

        [PropertyOrder(1)]
        public bool Enabled { get; [UsedImplicitly] set; }

        [PropertyOrder(2)]
        public string Name { get; [UsedImplicitly] set; }

        public enum AchievementTypes
        {
            None,
            TotalKills,
            TotalBLTKills,
            TotalMainKills,
            Summons,
            Attacks,
            Deaths,
            TotalTournamentWins,
            TotalTournamentLosses,
            TotalTournamentChampionships
        };

        [PropertyOrder(3), Description("Type of achievement this will be.")]
        public AchievementTypes Type { get; [UsedImplicitly] set; }

        [PropertyOrder(4), Description("Text that will display when the achievement is gained and when the player lists their achievements.  Can use {player} for the viewers name and {name} for the name of the achievement.")]
        public string NotificationText { get; [UsedImplicitly] set; }

        [PropertyOrder(5), Description("Value needed to obtain the achievement.")]
        public int Value { get; [UsedImplicitly] set; }

        [PropertyOrder(6), Description("Gold awarded for gaining this achievement.")]
        public int GoldGain { get; [UsedImplicitly] set; }

        [PropertyOrder(7), Description("Experience awarded for gaining this achievement.")]
        public int XPGain { get; [UsedImplicitly] set; }

        public override string ToString() => Name;
    }
}
