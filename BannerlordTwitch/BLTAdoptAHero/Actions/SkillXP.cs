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
            [Description("Improve a random skill from the Skills specified, rather than the best one"), PropertyOrder(2)]
            public bool Random { get; set; }
            [Description("If this is specified then the best skill from a random skill group will be improved, Skills list is ignored. Groups are melee (One Handed, Two Handed, Polearm), ranged (Bow, Crossbow, Throwing), support (Smithing, Scouting, Trade, Steward, Engineering), movement (Riding, Athletics), personal (Tactics, Roguery, Charm, Leadership)"), PropertyOrder(3)]
            public bool Auto { get; set; }
        }
        
        protected override Type ConfigType => typeof(SkillXPSettings);

        protected override (bool success, string description) Improve(string userName,
            Hero adoptedHero, int amount, SettingsBase baseSettings)
        {
            var settings = (SkillXPSettings) baseSettings;
            return ImproveSkill(adoptedHero, amount, settings.Skills, settings.Random, settings.Auto);
        }

        public static (bool success, string description) ImproveSkill(Hero hero, int amount, Skills skills, bool random, bool auto)
        {
            var skill = GetSkill(hero, skills, random, auto, so => hero.HeroDeveloper.GetFocusFactor(so) > 0);
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