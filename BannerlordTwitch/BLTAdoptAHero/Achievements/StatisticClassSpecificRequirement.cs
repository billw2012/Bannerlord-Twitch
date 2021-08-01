using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Achievements
{
    public class StatisticClassSpecificRequirement : StatisticRequirement, ILoaded
    {
        [Description("Class required to get this achievement. If (none) is specified " +
                     "then the achievement will apply ONLY when the hero doesn't have a class set."), 
         PropertyOrder(5), ItemsSource(typeof(HeroClassDef.ItemSource)), UsedImplicitly]
        public Guid RequiredClass { get; set; }
        
        [YamlIgnore, Browsable(false)]
        private GlobalHeroClassConfig ClassConfig { get; set; }
        
        public override bool IsMet(Hero hero)
        {
            int stat = BLTAdoptAHeroCampaignBehavior.Current.GetAchievementClassStat(hero, RequiredClass, Statistic);
            int val = OtherStatistic == AchievementStatsData.Statistic.None
                ? Value
                : BLTAdoptAHeroCampaignBehavior.Current.GetAchievementClassStat(hero, RequiredClass, OtherStatistic);
            return IsMet(stat, Comparison, val);
        }
        
        public StatisticClassSpecificRequirement()
        {
            // For when these are created via the configure tool
            ClassConfig = ConfigureContext.CurrentlyEditedSettings == null 
                ? null : GlobalHeroClassConfig.Get(ConfigureContext.CurrentlyEditedSettings);
        }

        #region ILoaded
        public void OnLoaded(Settings settings)
        {
            ClassConfig = GlobalHeroClassConfig.Get(settings);
        }
        #endregion

        public override string ToString() 
            => $"{base.ToString()} [{ClassConfig.GetClass(RequiredClass)?.Name ?? "(none)"}]";
    }
}