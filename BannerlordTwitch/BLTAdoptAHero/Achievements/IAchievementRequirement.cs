using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero.Achievements
{
    public interface IAchievementRequirement
    {
        bool IsMet(Hero hero);
    }
}