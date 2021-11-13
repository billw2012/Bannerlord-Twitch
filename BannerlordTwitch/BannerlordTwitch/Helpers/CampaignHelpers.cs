using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace BannerlordTwitch.Helpers
{
    public static class CampaignHelpers
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
                CharacterAttributesEnum.Vigor => "{=IyK1yg5j}Vigor".Translate(),
                CharacterAttributesEnum.Control => "{=dHlLNy7j}Control".Translate(),
                CharacterAttributesEnum.Endurance => "{=qKVblzEJ}Endurance".Translate(),
                CharacterAttributesEnum.Cunning => "{=bJbFeqMG}Cunning".Translate(),
                CharacterAttributesEnum.Social => "{=lSrkwcJV}Social".Translate(),
                CharacterAttributesEnum.Intelligence => "{=qGfZfaai}Intelligence".Translate(),
                _ => throw new ArgumentOutOfRangeException(nameof(val), val, null)
            };

        public static string GetShortAttributeName(CharacterAttributesEnum val) =>
            GetAttributeName(val).Substring(0, 3);

        public const CharacterAttributesEnum DefaultAttribute = default;
        
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

        public const CharacterAttribute DefaultAttribute = default;
        
        public static IEnumerable<SkillObject> AllSkillObjects => Skills.All;
        
        public static IEnumerable<Hero> DeadOrDisabledHeroes => Campaign.Current.DeadOrDisabledHeroes;
        public static void SetHeroName(Hero hero, TextObject name, TextObject firstName = null)
        {
            hero.SetName(name, firstName);
        }
        
        public static IEnumerable<ItemObject> AllItems => Items.All;
#endif
        
        public static float GetApplicationTime() => 
#if e159 || e1510 || e160 || e161 || e162
            MBCommon.GetTime(MBCommon.TimeType.Application)
#else
            MBCommon.GetApplicationTime()
#endif
        ;
        
        public static float GetTotalMissionTime() => 
#if e159 || e1510 || e160 || e161 || e162
            MBCommon.GetTime(MBCommon.TimeType.Mission)
#else
            MBCommon.GetTotalMissionTime()
#endif
        ;
        
        public static Crafting NewCrafting(CraftingTemplate craftingTemplate, BasicCultureObject culture) => 
#if e159 || e1510 || e160 || e161 || e162
            new Crafting(craftingTemplate, culture)
#else
            new Crafting(craftingTemplate, culture, craftingTemplate.TemplateName)
#endif
        ;

        public static IEnumerable<Hero> AliveHeroes => Campaign.Current.AliveHeroes;
        public static IEnumerable<Hero> AllHeroes => AliveHeroes.Concat(DeadOrDisabledHeroes).Distinct();

        public static IEnumerable<CultureObject> AllCultures => MBObjectManager.Instance.GetObjectTypeList<CultureObject>();
    }
}