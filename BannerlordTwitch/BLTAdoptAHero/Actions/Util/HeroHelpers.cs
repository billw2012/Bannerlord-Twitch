using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace BLTAdoptAHero.Actions.Util
{
    public class HeroHelpers
    {
#if e159 || e1510
        public static IEnumerable<CharacterAttributesEnum> AllAttributes =>
            new[]
            {
                CharacterAttributesEnum.Vigor,
                CharacterAttributesEnum.Control,
                CharacterAttributesEnum.Endurance,
                CharacterAttributesEnum.Cunning,
                CharacterAttributesEnum.Social,
                CharacterAttributesEnum.Intelligence,
            };

        public static string GetAttributeName(CharacterAttributesEnum val) => 
            val switch
            {
                CharacterAttributesEnum.Vigor => "Vigor",
                CharacterAttributesEnum.Control => "Control",
                CharacterAttributesEnum.Endurance => "Endurance",
                CharacterAttributesEnum.Cunning => "Cunning",
                CharacterAttributesEnum.Social => "Social",
                CharacterAttributesEnum.Intelligence => "Intelligence",
                _ => throw new ArgumentOutOfRangeException(nameof(val), val, null)
            };

        public static string GetShortAttributeName(CharacterAttributesEnum val) =>
            GetAttributeName(val).Substring(0, 3);
        
        public static IEnumerable<SkillObject> AllSkillObjects => SkillObject.All;
        
        public static IEnumerable<Hero> DeadOrDisabledHeroes => Campaign.Current.DeadAndDisabledHeroes;
        public static void SetHeroName(Hero hero, TextObject name, TextObject firstName = null)
        {
            hero.Name = name;
            if(firstName != null)
            {
                hero.FirstName = firstName;
            }
        }
        
        public static IEnumerable<ItemObject> AllItems => ItemObject.All;
#else
        public static IEnumerable<CharacterAttribute> AllAttributes => Attributes.All;
        public static string GetAttributeName(CharacterAttribute val) => val.Name.ToString();
        public static string GetShortAttributeName(CharacterAttribute val)
        {
            string str = val.Name.ToString();
            return str.Substring(0, Math.Min(3, str.Length));
        }
        public static IEnumerable<SkillObject> AllSkillObjects => Skills.All;
        
        public static IEnumerable<Hero> DeadOrDisabledHeroes => Campaign.Current.DeadOrDisabledHeroes;
        public static void SetHeroName(Hero hero, TextObject name, TextObject firstName = null)
        {
            hero.SetName(name, firstName);
        }
        
        public static IEnumerable<ItemObject> AllItems => Items.All;
#endif

        public static IEnumerable<Hero> AliveHeroes => Campaign.Current.AliveHeroes;
        public static IEnumerable<Hero> AllHeroes => AliveHeroes.Concat(DeadOrDisabledHeroes).Distinct();
    }
}