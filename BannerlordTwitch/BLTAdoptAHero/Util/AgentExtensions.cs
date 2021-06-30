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

        public static Hero GetHero(this Agent agent)
        {
            return (agent?.Character as CharacterObject)?.HeroObject;
        }
        
        public static Hero GetAdoptedHero(this Agent agent)
        {
            var hero = agent.GetHero();
            return hero?.IsAdopted() == true ? hero : null;
        }
    }
}