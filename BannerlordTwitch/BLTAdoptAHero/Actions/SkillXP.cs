using System;
using System.ComponentModel;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.Towns;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Improve adopted heroes skills")]
    internal class SkillXP : ImproveAdoptedHero
    {
        protected class SkillXPSettings : SettingsBase
        {
            [Description("What to improve"), PropertyOrder(1)]
            public Skills Skills { get; set; }
            [Description("Chooses a random skill to add XP to, prefering class skills, " +
                         "then skills for current equipment, then other skills. " +
                         "Skills setting is ignored when auto is used."),
             PropertyOrder(2)]
            public bool Auto { get; set; } = true;
        }
        
        protected override Type ConfigType => typeof(SkillXPSettings);

        protected override (bool success, string description) Improve(string userName,
            Hero adoptedHero, int amount, SettingsBase baseSettings)
        {
            var settings = (SkillXPSettings) baseSettings;
            return ImproveSkill(adoptedHero, amount, settings.Skills, settings.Auto);
        }

        public static (bool success, string description) ImproveSkill(Hero hero, int amount, Skills skills, bool auto)
        {
            var skill = GetSkill(hero, skills, auto, so => BLTAdoptAHeroModule.CommonConfig.UseRawXP || hero.HeroDeveloper.GetFocusFactor(so) > 0);
            if (skill == null) return (false, $"Couldn't find a skill to improve");
            float prevSkill = hero.HeroDeveloper.GetPropertyValue(skill);
            int prevLevel = hero.GetSkillValue(skill);
            hero.HeroDeveloper.AddSkillXp(skill, amount, isAffectedByFocusFactor: !BLTAdoptAHeroModule.CommonConfig.UseRawXP);
            // Force this immediately instead of waiting for the daily campaign tick
            CharacterDevelopmentCampaignBehaivor.DevelopCharacterStats(hero);
            
            float realGainedXp = hero.HeroDeveloper.GetPropertyValue(skill) - prevSkill;
            int newLevel = hero.GetSkillValue(skill);
            int gainedLevels = newLevel - prevLevel;
            return realGainedXp < 1f
                ? (false, $"{skill.Name} capped, get more focus points")
                : gainedLevels > 0
                    ? (true, $"+{gainedLevels} lvl in {skill.Name} ({newLevel})")
                    : (true,
                        $"+{realGainedXp:0}xp in {skill.Name} ({hero.HeroDeveloper.GetSkillXpProgress(skill)}xp)");
        }
    }
}