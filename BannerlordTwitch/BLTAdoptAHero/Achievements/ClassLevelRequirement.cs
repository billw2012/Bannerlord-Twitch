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
    public class ClassLevelRequirement : IAchievementRequirement, ILoaded
    {
        #region User Editable
        [Description("Minimum class level allowed"), PropertyOrder(1), UsedImplicitly]
        public int MinLevel { get; set; }

        [Description("Maximum class level allowed"), PropertyOrder(2), UsedImplicitly]
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
            => !MaxLevel.HasValue ? $"AT LEAST {MinLevel}" 
                : MinLevel == MaxLevel ? $"{MinLevel}" : $"{MinLevel} TO {MaxLevel}";
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

        public override string ToString() => $"CLASS LEVEL {Description}";
        #endregion
    }
}