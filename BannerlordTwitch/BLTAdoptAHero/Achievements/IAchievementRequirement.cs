using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero.Achievements
{
    public interface IAchievementRequirement
    {
        string Description { get; }
        bool IsMet(Hero hero);
    }
}