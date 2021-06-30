
using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using BLTAdoptAHero.Powers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Allows activation of the adopted heroes class active powers")]
    public class UsePower : HeroActionHandlerBase
    {
        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config, 
            Action<string> onSuccess, Action<string> onFailure)
        {
            var heroClass = adoptedHero.GetClass();
            if (heroClass == null)
            {
                onFailure($"You don't have a class, so no powers are available!");
                return;
            }

            (bool canActivate, string failReason) = heroClass.ActivePower.CanActivate(adoptedHero);
            if (!canActivate)
            {
                onFailure($"You cannot active your powers now: {failReason}!");
                return;
            }
            
            if (heroClass.ActivePower.IsActive(adoptedHero))
            {
                onFailure($"Your powers are already active!");
                return;
            }

            (bool allowed, string message) = heroClass.ActivePower.Activate(adoptedHero, context);

            if (allowed)
            {
                onSuccess(message);
            }
            else
            {
                onFailure(message);
            }
        }
    }
}