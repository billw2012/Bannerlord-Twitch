using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero.Achievements
{
    [YamlTagged]
    public interface IAchievementRequirement
    {
        bool IsMet(Hero hero);
    }
}