using System;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    public enum CharacterAttributes
    {
        Vigor = 0,
        Control = 1,
        Endurance = 2,
        Cunning = 3,
        Social = 4,
        Intelligence = 5,
        Random = 6,
    }
    
    [UsedImplicitly]
    [Description("Improve adopted heroes attribute points")]
    internal class AttributePoints : ImproveAdoptedHero
    {
        protected class AttributePointsSettings : SettingsBase
        {
            [Description("Which attribute to improve (specify one only)"), PropertyOrder(1)]
            public CharacterAttributes Attribute { get; set; } = CharacterAttributes.Random;
        }
        
        protected override Type ConfigType => typeof(AttributePointsSettings);
        
        protected override (bool success, string description) Improve(string userName,
            Hero adoptedHero, int amount, SettingsBase baseSettings)
        {
            var settings = (AttributePointsSettings) baseSettings;

            return IncreaseAttribute(adoptedHero, amount, settings.Attribute);
        }

        private static (bool success, string description) IncreaseAttribute(Hero adoptedHero, int amount, CharacterAttributes attribToIncrease)
        {
            // Get attributes that can be buffed
            var improvableAttributes = AdoptAHero.CharAttributes
                .Select(c => c.val)
                .Where(a => adoptedHero.GetAttributeValue(a) < 10)
                .ToList();

            if (!improvableAttributes.Any())
            {
                return (false, $"Couldn't improve any attributes, they are all at max level!");
            }

            var attribute = attribToIncrease != CharacterAttributes.Random
                ? (CharacterAttributesEnum) attribToIncrease
                : improvableAttributes.SelectRandom();

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