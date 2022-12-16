using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace BannerlordTwitch.Helpers
{
    public static class AgentExtensions
    {
        public static Hero GetHero(this Agent agent)
        {
            return (agent?.Character as CharacterObject)?.HeroObject;
        }
    }
}