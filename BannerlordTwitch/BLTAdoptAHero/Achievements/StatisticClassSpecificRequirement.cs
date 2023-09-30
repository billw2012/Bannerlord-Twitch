using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Achievements
{
    [YamlTagged]
    [LocDisplayName("{=ziuB7eSV}Statistic Class Specific Requirement")]
    public class StatisticClassSpecificRequirement : StatisticRequirement, ILoaded
    {
        #region User Editable
        [LocDisplayName("{=WwKyvotH}Current Class"),
         LocDescription("{=S9kGgf0r}Requirement uses current class, useful for class power unlocks so you don't have to specify the class explicitly"), 
         PropertyOrder(5), UsedImplicitly]
        public bool CurrentClass { get; set; }
        
        [LocDisplayName("{=5QwSEOc3}Required Class"),
         LocDescription("{=G34P8lCu}Class required to get this achievement. If (none) is specified then the achievement will apply ONLY when the hero doesn't have a class set."), 
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

        [YamlIgnore, Browsable(false)]
        public override string Description 
            => base.Description + " [" + "{=j3YG9dCh}class".Translate() + ": " +
               ("Class",
                   CurrentClass
                    ? "{=RXXuHIdN}(current)".Translate() 
                    : ClassConfig.GetClass(RequiredClass)?.Name ?? "{=dPEnuHsk}(none)".Translate());
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