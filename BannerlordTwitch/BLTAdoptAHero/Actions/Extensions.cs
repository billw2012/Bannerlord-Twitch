using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    internal static class AgentExtensions
    {
        internal static bool IsAdopted(this Agent agent) 
            => (agent.Character as CharacterObject)?.HeroObject?.IsAdopted() == true;
    }

    internal static class HeroExtensions
    {
        internal static bool IsAdopted(this Hero hero) => hero.Name.Contains(BLTAdoptAHeroModule.Tag);
    }
}