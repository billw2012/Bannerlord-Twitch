using System;
using System.Linq;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    public static class HeroExtensions
    {
        public static bool IsAdopted(this Hero hero) => hero.Name.Contains(BLTAdoptAHeroModule.Tag);
        public static bool IsAdoptedBy(this Hero hero, string user) 
            => hero.IsAdopted()
            && string.Equals(hero.FirstName?.Raw(), user, StringComparison.CurrentCultureIgnoreCase);

        public static HeroClassDef GetClass(this Hero hero) 
            => BLTAdoptAHeroCampaignBehavior.Current.GetClass(hero);
        
        public static Agent GetAgent(this Hero hero)
            => Mission.Current?.Agents?.FirstOrDefault(a => a.Character == hero.CharacterObject);
    }
}