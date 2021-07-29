using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero.Powers
{
    /// <summary>
    /// Specifies the required behaviour of a passive hero power
    /// </summary>
    public interface IHeroPowerPassive
    {
        /// <summary>
        /// Called the first time this hero is spawned in a mission (regardless of whether it was by being
        /// in an existing party, or being summoned).
        /// Attach listeners to the events in <paramref name="handlers"/> to implement the power behaviour.
        /// </summary>
        /// <param name="hero"></param>
        /// <param name="handlers"></param>
        void OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers);
    }
}