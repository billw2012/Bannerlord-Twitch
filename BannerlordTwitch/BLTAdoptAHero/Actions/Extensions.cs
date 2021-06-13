using System;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    public static class AgentExtensions
    {
        public static bool IsAdopted(this Agent agent) 
            => (agent.Character as CharacterObject)?.HeroObject?.IsAdopted() == true;
        public static bool IsAdoptedBy(this Agent agent, string user)
        {
            if (agent.Character is not CharacterObject charObj) return false;
            return charObj.HeroObject?.IsAdopted() == true
                   && string.Equals(user, charObj.HeroObject?.FirstName?.Raw(), StringComparison.CurrentCultureIgnoreCase);
        }
    }

    public static class HeroExtensions
    {
        public static bool IsAdopted(this Hero hero) => hero.Name.Contains(BLTAdoptAHeroModule.Tag);
        public static bool IsAdoptedBy(this Hero hero, string user) 
            => hero.IsAdopted()
            && string.Equals(hero.FirstName?.Raw(), user, StringComparison.CurrentCultureIgnoreCase);
    }
}