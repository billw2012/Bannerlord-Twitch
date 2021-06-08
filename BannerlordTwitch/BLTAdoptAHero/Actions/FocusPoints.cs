using System;
using System.ComponentModel;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Add focus points to heroes skills")]
    internal class FocusPoints : ImproveAdoptedHero
    {
        protected class FocusPointsSettings : SettingsBase
        {
            [Description("What skill to add focus to"), PropertyOrder(1)]
            public Skills Skills { get; set; } = Skills.None;

            [Description("Chooses a random skill to add focus to, prefering class skills, " +
                         "then skills for current equipment, then other skills. " +
                         "Skills setting is ignored when auto is used."),
             PropertyOrder(2)]
            public bool Auto { get; set; } = true;
        }
        
        protected override Type ConfigType => typeof(FocusPointsSettings);
        
        protected override (bool success, string description) Improve(string userName,
            Hero adoptedHero, int amount, SettingsBase baseSettings)
        {
            var settings = (FocusPointsSettings) baseSettings;

            return FocusSkill(adoptedHero, amount, settings.Skills, settings.Auto);
        }

        public static (bool success, string description) FocusSkill(Hero adoptedHero, int amount, Skills skills, bool auto)
        {
            var skill = GetSkill(adoptedHero, skills, auto, s => adoptedHero.HeroDeveloper.GetFocus(s) < 5);

            if (skill == null)
            {
                return (false, $"Couldn't find a valid skill to add focus points to!");
            }

            amount = Math.Min(amount, 5 - adoptedHero.HeroDeveloper.GetFocus(skill));
            adoptedHero.HeroDeveloper.AddFocus(skill, amount, checkUnspentFocusPoints: false);
            return (true,
                $"You have gained {amount} focus point{(amount > 1 ? "s" : "")} in {skill}, you now have {adoptedHero.HeroDeveloper.GetFocus(skill)}!");
        }
    }
}