using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    internal abstract class ImproveAdoptedHero : ActionHandlerBase
    {
        protected class SettingsBase
        {
            [Description("Lower bound of amount to improve"), PropertyOrder(11)]
            public int AmountLow { get; set; }
            [Description("Upper bound of amount to improve"), PropertyOrder(12)]
            public int AmountHigh { get; set; }
            [Description("Gold that will be taken from the hero"), PropertyOrder(13)]
            public int GoldCost { get; set; }
        }

        // protected override Type ConfigType => typeof(SettingsBase);

        protected override void ExecuteInternal(ReplyContext context, object config,
            Action<string> onSuccess,
            Action<string> onFailure) 
        {
            var settings = (SettingsBase)config;
            var adoptedHero = AdoptAHero.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }

            if (adoptedHero.Gold < settings.GoldCost)
            {
                onFailure($"You do not have enough gold: you need {settings.GoldCost}, and you only have {adoptedHero.Gold}!");
                return;
            }
            
            var amount = MBRandom.RandomInt(settings.AmountLow, settings.AmountHigh);
            var (success, description) = Improve(context.UserName, adoptedHero, amount, settings);
            if (success)
            {
                onSuccess(description);
                adoptedHero.Gold -= settings.GoldCost;
            }
            else
            {
                onFailure(description);
            }
        }

        protected abstract (bool success, string description) Improve(string userName, Hero adoptedHero, int amount, SettingsBase settings);

        private static string[] EnumFlagsToArray<T>(T flags) =>
            flags.ToString().Split(',').Select(s => s.Trim()).ToArray();
        private static string[][] SkillGroups =
        {
            SkillGroup.SkillsToStrings(Skills.Melee),
            SkillGroup.SkillsToStrings(Skills.Ranged),
            SkillGroup.SkillsToStrings(Skills.Support),
            SkillGroup.SkillsToStrings(Skills.Movement),
            SkillGroup.SkillsToStrings(Skills.Personal),
        };
        
        protected static SkillObject GetSkill(Hero hero, Skills skills, bool random, bool auto, Func<SkillObject, bool> predicate = null)
        {
            IEnumerable<SkillObject> GetSkills(IEnumerable<string> sk) => sk
                .Select(sn => DefaultSkills.GetAllSkills().FirstOrDefault(so =>
                    string.Equals(so.StringId, sn, StringComparison.CurrentCultureIgnoreCase)))
                .Where(predicate ?? (s => true));
                
            IEnumerable<SkillObject> selectedSkills;
            if (auto)
            {
                // We will select automatically which skill from groups
                selectedSkills = SkillGroups
                    .Select(GetSkills)
                    .Where(g => g.Any())
                    .SelectRandom();
            }
            else
            {
                selectedSkills = GetSkills(SkillGroup.SkillsToStrings(skills));
            }

            return random 
                ? selectedSkills?.SelectRandom() 
                : selectedSkills?.OrderByDescending(hero.GetSkillValue).FirstOrDefault();
        }
    }
}