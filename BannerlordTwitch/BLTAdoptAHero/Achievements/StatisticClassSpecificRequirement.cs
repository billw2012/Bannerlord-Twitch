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
    [YamlTagged]
    public class StatisticClassSpecificRequirement : StatisticRequirement, ILoaded
    {
        #region User Editable
        [Description("Requirement uses current class, useful for class power unlocks so you don't have to specify" +
                     "the class explicitly"), PropertyOrder(5), UsedImplicitly]
        public bool CurrentClass { get; set; }
        
        [Description("Class required to get this achievement. If (none) is specified " +
                     "then the achievement will apply ONLY when the hero doesn't have a class set."), 
         PropertyOrder(6), ItemsSource(typeof(HeroClassDef.ItemSource)), UsedImplicitly]
        public Guid RequiredClass { get; set; }
        #endregion
        
        #region Public Interface
        public override bool IsMet(Hero hero)
        {
            int stat = BLTAdoptAHeroCampaignBehavior.Current.GetAchievementClassStat(hero, 
                CurrentClass ? (hero.GetClass()?.ID ?? Guid.Empty) : RequiredClass, Statistic
                );
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

        public override string Description => $"{base.Description} [class: {(CurrentClass ? "(current)" : ClassConfig.GetClass(RequiredClass)?.Name ?? "(none)")}]";
        #endregion
        
        #region Implementation Details
        [YamlIgnore, Browsable(false)]
        private GlobalHeroClassConfig ClassConfig { get; set; }
        #endregion
        
        #region ILoaded
        public void OnLoaded(Settings settings)
        {
            ClassConfig = GlobalHeroClassConfig.Get(settings);
        }
        #endregion
    }
}