using System;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Actions.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Improve adopted heroes attribute points")]
    internal class AttributePoints : ImproveAdoptedHero
    {
        protected class AttributePointsSettings : SettingsBase, IDocumentable
        {
            [Description("If set a random attribute is improved, otherwise the viewer should provide part of the name of the attribute to improve (this works best as a chat command)"), PropertyOrder(1), UsedImplicitly]
            public bool Random { get; set; } = true;

            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                generator.P(Random ? "Random attribute" : "Provide the attribute name (or part of it) when calling this");
                generator.PropertyValuePair("Amount", $"{AmountLow}" + (AmountLow == AmountHigh ? $"" : $" to {AmountHigh}"));
                if (GoldCost != 0)
                {
                    generator.P($"Costs {GoldCost}{Naming.Gold}");
                }
            }
        }
        
        protected override Type ConfigType => typeof(AttributePointsSettings);
        
        protected override (bool success, string description) Improve(string userName,
            Hero adoptedHero, int amount, SettingsBase baseSettings, string args)
        {
            var settings = (AttributePointsSettings) baseSettings;

            return IncreaseAttribute(adoptedHero, amount, settings.Random, args);
        }

        private static (bool success, string description) IncreaseAttribute(Hero adoptedHero, int amount, bool random,
            string args)
        {
            // Get attributes that can be buffed
            var improvableAttributes = HeroHelpers.AllAttributes
                .Where(a => adoptedHero.GetAttributeValue(a) < 10)
                .ToList();

            if (!improvableAttributes.Any())
            {
                return (false, $"Couldn't improve any attributes, they are all at max level!");
            }

            if (!random && string.IsNullOrEmpty(args))
            {
                return (false, $"Provide the attribute name to improve (or part of it)");
            }

            if (random)
            {
                var attribute = improvableAttributes.SelectRandom();
                amount = Math.Min(amount, 10 - adoptedHero.GetAttributeValue(attribute));
                adoptedHero.HeroDeveloper.AddAttribute(attribute, amount, checkUnspentPoints: false);
                return (true, $"You have gained {amount} point{(amount > 1 ? "s" : "")} in {attribute}, you now have {adoptedHero.GetAttributeValue(attribute)}!");
            }
            else
            {
                // We do this because in <=1.5.10 attributes are an enum, which doesn't have a useful default for FirstOrDefault (default is the same as the first enum value)
                if (!HeroHelpers.AllAttributes.Any(a => HeroHelpers.GetAttributeName(a).ToLower().Contains(args.ToLower())))
                {
                    return (false, $"Couldn't find attribute matching '{args}'!");
                } 
                var attribute = HeroHelpers.AllAttributes.First(a => HeroHelpers.GetAttributeName(a).ToLower().Contains(args.ToLower()));
                if (!improvableAttributes.Contains(attribute))
                {
                    return (false, $"Couldn't improve {attribute} attributes, it is already at max level!");
                }

                amount = Math.Min(amount, 10 - adoptedHero.GetAttributeValue(attribute));
                adoptedHero.HeroDeveloper.AddAttribute(attribute, amount, checkUnspentPoints: false);
                return (true, $"You have gained {amount} point{(amount > 1 ? "s" : "")} in {attribute}, you now have {adoptedHero.GetAttributeValue(attribute)}!");
            }
        }
    }
}