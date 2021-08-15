using System;
using System.ComponentModel;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Achievements
{
    [YamlTagged]
    [LocDisplayName("{=CkgaxlDY}Statistic Requirement")]
    public class StatisticRequirement : IAchievementRequirement, ICloneable
    {
        #region User Editable
        [LocDisplayName("{=ienuHzOP}Statistic"),
         LocDescription("{=OKuz3zwY}The statistic this achievement relates to."), 
         PropertyOrder(1), UsedImplicitly]
        public AchievementStatsData.Statistic Statistic { get; set; }

        public enum Operator
        {
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual
        }

        [LocDisplayName("{=dXBsEW65}Comparison"),
         LocDescription("{=4yoAe9WV}Whether hero needs more or less than the specifed Value. For instance you can make requirements for the hero to have less than 10 deaths and more than 100 kills."), 
         PropertyOrder(2), UsedImplicitly]
        public Operator Comparison { get; set; } = Operator.GreaterOrEqual;
        
        [LocDisplayName("{=a8Josr4g}Value"),
         LocDescription("{=B9LEjEq8}Value needed to obtain the achievement."), 
         PropertyOrder(3), UsedImplicitly]
        public int Value { get; set; }
        
        [LocDisplayName("{=nau8vg7o}Other Statistic"),
         LocDescription("{=A3EKQ7up}Other statistic to compare against (this overrides Value if specified)."), 
         PropertyOrder(4), UsedImplicitly]
        public AchievementStatsData.Statistic OtherStatistic { get; set; }
        #endregion

        #region IAchievementRequirement
        public virtual bool IsMet(Hero hero)
        {
            int stat = BLTAdoptAHeroCampaignBehavior.Current.GetAchievementTotalStat(hero, Statistic);
            int val = OtherStatistic == AchievementStatsData.Statistic.None
                ? Value
                : BLTAdoptAHeroCampaignBehavior.Current.GetAchievementTotalStat(hero, OtherStatistic);
            return IsMet(stat, Comparison, val);
        }

        [YamlIgnore, Browsable(false)]
        public virtual string Description =>
            $"{Statistic.GetDisplayName()} " +
            $"{ComparisonText} " +
            $"{(OtherStatistic == AchievementStatsData.Statistic.None ? Value : OtherStatistic.GetDisplayName())}";
        #endregion

        #region Public Interface
        private string ComparisonText => Comparison switch
        {
            Operator.Greater => ">",
            Operator.GreaterOrEqual => ">=",
            Operator.Less => "<",
            Operator.LessOrEqual => "<=",
            _ => throw new ArgumentOutOfRangeException()
        };

        public override string ToString() => Description;
        
        public object Clone() => CloneHelpers.CloneProperties(this);

        #endregion

        #region Implementation Details
        protected static bool IsMet(int stat, Operator comparison, int val)
        {
            return comparison switch
            {
                Operator.Greater => stat > val,
                Operator.GreaterOrEqual => stat >= val,
                Operator.Less => stat < val,
                Operator.LessOrEqual => stat <= val,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        #endregion
    }
}