using System;
using System.ComponentModel;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Achievements
{
    [YamlTagged]
    public class StatisticRequirement : IAchievementRequirement
    {
        #region User Editable
        [PropertyOrder(1), Description("The statistic this achievement relates to."), UsedImplicitly]
        public AchievementStatsData.Statistic Statistic { get; set; }

        public enum Operator
        {
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual
        }

        [PropertyOrder(2), 
         Description("Whether hero needs more or less than the specifed Value. For instance you can make " +
                     "requirements for the hero to have less than 10 deaths and more than 100 kills."), 
         UsedImplicitly]
        public Operator Comparison { get; set; } = Operator.GreaterOrEqual;
        
        [PropertyOrder(3), Description("Value needed to obtain the achievement."), UsedImplicitly]
        public int Value { get; set; }
        
        [PropertyOrder(4), Description("Other statistic to compare against (this overrides Value if specified)."), UsedImplicitly]
        public AchievementStatsData.Statistic OtherStatistic { get; set; }
        #endregion

        #region Public Interface

        #region IAchievementRequirement
        public virtual bool IsMet(Hero hero)
        {
            int stat = BLTAdoptAHeroCampaignBehavior.Current.GetAchievementTotalStat(hero, Statistic);
            int val = OtherStatistic == AchievementStatsData.Statistic.None
                ? Value
                : BLTAdoptAHeroCampaignBehavior.Current.GetAchievementTotalStat(hero, OtherStatistic);
            return IsMet(stat, Comparison, val);
        }

        public virtual string Description =>
            $"{Statistic.ToString().SplitCamelCase()} " +
            $"{ComparisonText} " +
            $"{(OtherStatistic == AchievementStatsData.Statistic.None ? Value : OtherStatistic.ToString().SplitCamelCase())}";

        #endregion
        
        private string ComparisonText => Comparison switch
        {
            Operator.Greater => ">",
            Operator.GreaterOrEqual => ">=",
            Operator.Less => "<",
            Operator.LessOrEqual => "<=",
            _ => throw new ArgumentOutOfRangeException()
        };

        public override string ToString() => Description;
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