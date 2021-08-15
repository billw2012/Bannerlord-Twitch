using System;
using System.Collections.Generic;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.Towns;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=mpdzqyCK}Skill XP"), 
     LocDescription("{=Bc2HIByX}Improve adopted heroes skills"), 
     UsedImplicitly]
    internal class SkillXP : ImproveAdoptedHero
    {
        protected class SkillXPSettings : SettingsBase, IDocumentable
        {
            [LocDisplayName("{=rAGAEUIH}Skills"), 
             LocDescription("{=wo5VSDaj}What to improve"), 
             PropertyOrder(1), UsedImplicitly]
            public SkillsEnum Skills { get; set; }

            [LocDisplayName("{=LVp7Llay}Auto"), 
             LocDescription("{=CTCcqB3Y}Chooses a random skill to add XP to, prefering class skills, then skills for current equipment, then other skills. Skills setting is ignored when auto is used."),
             PropertyOrder(2), UsedImplicitly]
            public bool Auto { get; set; } = true;
            
            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                generator.PropertyValuePair(Naming.Skills, 
                    Auto 
                        ? "{=QJ9Cssr7}Automatic, based on class, equipment, and existing skills".Translate() 
                        : Skills.GetDisplayName());
                generator.PropertyValuePair(Naming.XP,
                    AmountLow == AmountHigh
                        ? AmountLow.ToString()
                        : "{=yVydxRHh}{From} to {To}".Translate(("From", AmountLow), ("To", AmountHigh)));
                if (GoldCost != 0)
                {
                    generator.PropertyValuePair("{=lz87CnyL}Costs".Translate(), $"{GoldCost}{Naming.Gold}");
                }
            }
        }

        protected override Type ConfigType => typeof(SkillXPSettings);

        protected override (bool success, string description) Improve(string userName,
            Hero adoptedHero, int amount, SettingsBase baseSettings, string args)
        {
            var settings = (SkillXPSettings) baseSettings;
            return ImproveSkill(adoptedHero, amount, settings.Skills, settings.Auto);
        }

        public static (bool success, string description) ImproveSkill(Hero hero, int amount, SkillsEnum skills, bool auto)
        {
            var skill = GetSkill(hero, skills, auto, so 
                => BLTAdoptAHeroModule.CommonConfig.UseRawXP && hero.GetSkillValue(so) < BLTAdoptAHeroModule.CommonConfig.RawXPSkillCap
                   || hero.HeroDeveloper.GetFocusFactor(so) > 0);
            if (skill == null) return (false, "{=vK5z2Naq}Couldn't find a skill to improve".Translate());
            float prevSkill = hero.HeroDeveloper.GetPropertyValue(skill);
            int prevLevel = hero.GetSkillValue(skill);
            hero.HeroDeveloper.AddSkillXp(skill, amount,
                isAffectedByFocusFactor: !BLTAdoptAHeroModule.CommonConfig.UseRawXP);
            // Force this immediately instead of waiting for the daily campaign tick
            #if e159 || e1510
            CharacterDevelopmentCampaignBehaivor.DevelopCharacterStats(hero);
            #else
            Campaign.Current?.GetCampaignBehavior<CharacterDevelopmentCampaignBehavior>()?.DevelopCharacterStats(hero);
            #endif

            float newXp = hero.HeroDeveloper.GetPropertyValue(skill);
            float realGainedXp = newXp - prevSkill;
            int newLevel = hero.GetSkillValue(skill);
            int gainedLevels = newLevel - prevLevel;
            return realGainedXp < 1f
                ? (false, "{=ozkK4mk7}{Skill} capped, get more focus points".Translate(("Skill", skill.Name)))
                : gainedLevels > 0
                    ? (true, $"{Naming.Inc}{gainedLevels} {Naming.Lvl} {GetShortSkillName(skill)}{Naming.To}{newLevel}")
                    : (true, $"{Naming.Inc}{realGainedXp:0} {Naming.XP} {GetShortSkillName(skill)}{Naming.To}{newXp}");
        }

        public static string GetShortSkillName(SkillObject skill)
        {
            return SkillMapping.TryGetValue(skill.StringId, out string shortSkillName) ? shortSkillName : skill.Name.ToString();
        }

        private static readonly Dictionary<string, string> SkillMapping = new()
        {
            {"OneHanded", "{=Ei8NyJSW}1H".Translate()},
            {"TwoHanded", "{=U38E72Ue}2H".Translate()},
            {"Polearm", "{=Cg885bns}PA".Translate()},
            {"Bow", "{=QpWRsKrc}Bow".Translate()},
            {"Crossbow", "{=71M512iJ}Xb".Translate()},
            {"Throwing", "{=jMRloOi0}Thr".Translate()},
            {"Riding", "{=ligWzK3s}Rid".Translate()},
            {"Athletics", "{=1MJnisqR}Ath".Translate()},
            {"Crafting", "{=VjV54wik}Smt".Translate()},
            {"Tactics", "{=6iG2GJW5}Tac".Translate()},
            {"Scouting", "{=AgaZsMEb}Sct".Translate()},
            {"Roguery", "{=6x33x91Z}Rog".Translate()},
            {"Charm", "{=N9X9VVsM}Cha".Translate()},
            {"Trade", "{=J8ElWB0P}Trd".Translate()},
            {"Steward", "{=BD65Qr83}Stw".Translate()},
            {"Leadership", "{=OYJ8tfhY}Ldr".Translate()},
            {"Medicine", "{=PmKb4QCv}Med".Translate()},
            {"Engineering", "{=wLkMFOky}Eng".Translate()},
        };
    }
}