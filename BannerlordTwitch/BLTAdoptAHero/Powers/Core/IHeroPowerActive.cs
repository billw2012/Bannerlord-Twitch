using System;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero.Powers
{
    /// <summary>
    /// Specifies the required behaviour of an active hero power
    /// </summary>
    public interface IHeroPowerActive
    {
        /// <summary>
        /// Should return true if the power can currently be activated on <paramref name="hero"/>,
        /// or false if it can't, giving a reason.
        /// </summary>
        /// <param name="hero"></param>
        /// <returns></returns>
        (bool canActivate, string failReason) CanActivate(Hero hero);

        /// <summary>
        /// Should return true if the power is current active on <paramref name="hero"/>.
        /// </summary>
        /// <param name="hero"></param>
        /// <returns></returns>
        bool IsActive(Hero hero);

        /// <summary>
        /// Activate the power for <paramref name="hero"/>.
        /// <paramref name="expiryCallback"/> should be called when the power expires. 
        /// </summary>
        /// <param name="hero"></param>
        /// <param name="expiryCallback"></param>
        void Activate(Hero hero, Action expiryCallback);

        /// <summary>
        /// Should return the total duration and remaining duration of the power, for <paramref name="hero"/>
        /// </summary>
        /// <param name="hero"></param>
        /// <returns></returns>
        (float duration, float remaining) DurationRemaining(Hero hero);
    }
}