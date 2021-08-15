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
    [LocDisplayName("{=XLXBwBEj}Class Level Requirement")]
    public class ClassLevelRequirement : IAchievementRequirement, ILoaded
    {
        #region User Editable
        [LocDisplayName("{=s4N0bhG1}Min Level"),
         LocDescription("{=phf8JXQC}Minimum class level allowed"), 
         PropertyOrder(1), UsedImplicitly]
        public int MinLevel { get; set; }

        [LocDisplayName("{=BT59iHQ5}Max Level"),
         LocDescription("{=lk60ZYwc}Maximum class level allowed"), 
         PropertyOrder(2), UsedImplicitly]
        public int? MaxLevel { get; set; }
        #endregion
        
        #region IAchievementRequirement
        public virtual bool IsMet(Hero hero)
        {
            int classLevel = ClassConfig.GetHeroClassLevel(hero);
            return classLevel >= MinLevel && (!MaxLevel.HasValue || classLevel <= MaxLevel);
        }
        
        [YamlIgnore, Browsable(false)]
        public virtual string Description 
            => !MaxLevel.HasValue ? "{=j3QY6MkC}AT LEAST {MinLevel}".Translate(("MinLevel", MinLevel)) 
                : MinLevel == MaxLevel
                    ? $"{MinLevel}" 
                    : "{=puNSSMYD}{MinLevel} TO {MaxLevel}".Translate(("MinLevel", MinLevel), ("MaxLevel", MaxLevel));
        #endregion
        
        #region ILoaded
        public void OnLoaded(Settings settings)
        {
            ClassConfig = GlobalHeroClassConfig.Get(settings);   
        }
        
        private GlobalHeroClassConfig ClassConfig { get; set; }
        #endregion
        
        #region Public Interface
        public ClassLevelRequirement()
        {
            // For when these are created via the configure tool
            ClassConfig = ConfigureContext.CurrentlyEditedSettings == null 
                ? null : GlobalHeroClassConfig.Get(ConfigureContext.CurrentlyEditedSettings);
        }

        public override string ToString() => "{=boWoZsH4}CLASS LEVEL {Description}".Translate(("Description", Description));
        #endregion
    }
}