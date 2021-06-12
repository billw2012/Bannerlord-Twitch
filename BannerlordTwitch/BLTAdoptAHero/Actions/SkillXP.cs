using System;
using System.Collections.Generic;
using System.ComponentModel;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.Towns;
using TaleWorlds.Core;
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
            var skill = GetSkill(hero, skills, auto,
                so => BLTAdoptAHeroModule.CommonConfig.UseRawXP || hero.HeroDeveloper.GetFocusFactor(so) > 0);
            if (skill == null) return (false, $"Couldn't find a skill to improve");
            float prevSkill = hero.HeroDeveloper.GetPropertyValue(skill);
            int prevLevel = hero.GetSkillValue(skill);
            hero.HeroDeveloper.AddSkillXp(skill, amount,
                isAffectedByFocusFactor: !BLTAdoptAHeroModule.CommonConfig.UseRawXP);
            // Force this immediately instead of waiting for the daily campaign tick
            CharacterDevelopmentCampaignBehaivor.DevelopCharacterStats(hero);

            float newXp = hero.HeroDeveloper.GetPropertyValue(skill);
            float realGainedXp = newXp - prevSkill;
            int newLevel = hero.GetSkillValue(skill);
            int gainedLevels = newLevel - prevLevel;
            return realGainedXp < 1f
                ? (false, $"{skill.Name} capped, get more focus points")
                : gainedLevels > 0
                    ? (true, $"{Naming.Inc}{gainedLevels}lvl {GetShortSkillName(skill)}")
                    : (true,
                        $"{Naming.Inc}{realGainedXp:0}xp {GetShortSkillName(skill)}");
        }

        public static string GetShortSkillName(SkillObject skill)
        {
            return SkillMapping.TryGetValue(skill.StringId, out string shortSkillName) ? shortSkillName : skill.Name.ToString();
        }

        private static readonly Dictionary<string, string> SkillMapping = new()
        {
            {"OneHanded", "1H"},
            {"TwoHanded", "2H"},
            {"Polearm", "PA"},
            {"Bow", "Bow"},
            {"Crossbow", "Xb"},
            {"Throwing", "Thr"},
            {"Riding", "Rid"},
            {"Athletics", "Ath"},
            {"Crafting", "Smt"},
            {"Tactics", "Tac"},
            {"Scouting", "Sct"},
            {"Roguery", "Rog"},
            {"Charm", "Cha"},
            {"Trade", "Trd"},
            {"Steward", "Stw"},
            {"Leadership", "Ldr"},
            {"Medicine", "Med"},
            {"Engineering", "Eng"},
        };
    }
}