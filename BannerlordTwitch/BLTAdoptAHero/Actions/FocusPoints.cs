using System;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=WihQZ5uq}Focus Points"),
     LocDescription("{=01L8w1ZW}Add focus points to heroes skills"), 
     UsedImplicitly]
    internal class FocusPoints : ImproveAdoptedHero
    {
        protected class FocusPointsSettings : SettingsBase, IDocumentable
        {
            [LocDisplayName("{=QwuyvXBg}Skills"),
             LocDescription("{=w9liWZ9A}What skill to add focus to"), 
             PropertyOrder(10), UsedImplicitly]
            public SkillsEnum Skills { get; set; } = SkillsEnum.None;

            [LocDisplayName("{=RiUjwmS5}Auto"),
             LocDescription("{=EA4jsUhm}Chooses a random skill to add focus to, prefering class skills, then skills for current equipment, then other skills. Skills setting is ignored when auto is used."),
             PropertyOrder(12), UsedImplicitly]
            public bool Auto { get; set; } = true;
            
            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                generator.PropertyValuePair(
                    "{=QwuyvXBg}Skills".Translate(), 
                    Auto 
                        ? "{=SIM2l7ta}Automatic, based on class, equipment, and existing skills".Translate() 
                        : Skills.GetDisplayName());
                generator.PropertyValuePair("{=UpHpjzFk}Focus points".Translate(),
                    AmountLow == AmountHigh
                        ? AmountLow.ToString()
                        : "{=yVydxRHh}{From} to {To}".Translate(("From", AmountLow), ("To", AmountHigh)));
                if (GoldCost != 0)
                {
                    generator.PropertyValuePair("{=LnQoMDLT}Cost".Translate(), $"{GoldCost}{Naming.Gold}");
                }
            }
        }
        
        protected override Type ConfigType => typeof(FocusPointsSettings);
        
        protected override (bool success, string description) Improve(string userName,
            Hero adoptedHero, int amount, SettingsBase baseSettings, string args)
        {
            var settings = (FocusPointsSettings) baseSettings;

            return FocusSkill(adoptedHero, amount, settings.Skills, settings.Auto);
        }

        public static (bool success, string description) FocusSkill(Hero adoptedHero, int amount, SkillsEnum skills, bool auto)
        {
            var skill = GetSkill(adoptedHero, skills, auto, s => adoptedHero.HeroDeveloper.GetFocus(s) < 5);

            if (skill == null)
            {
                return (false, "{=VXvS5xji}Couldn't find a valid skill to add focus points to!".Translate());
            }

            amount = Math.Min(amount, 5 - adoptedHero.HeroDeveloper.GetFocus(skill));
            adoptedHero.HeroDeveloper.AddFocus(skill, amount, checkUnspentFocusPoints: false);
            return (true,
                (amount > 1
                    ? "{=6tcVqRIs}You have gained {Amount} focus points in {Skill}, you now have {NewAmount}!"
                    : "{=HLFMWOJA}You have gained a focus point in {Skill}, you now have {NewAmount}!")
                .Translate(
                    ("Amount", amount),
                    ("Skill", skill.Name.ToString()),
                    ("NewAmount", adoptedHero.HeroDeveloper.GetFocus(skill))));
        }
    }
}