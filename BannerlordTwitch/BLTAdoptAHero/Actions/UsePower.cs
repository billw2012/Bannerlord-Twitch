
using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=Wpx7tHFL}Use Power"),
     LocDescription("{=FJwn6kGW}Allows activation of the adopted heroes class active powers"), 
     UsedImplicitly]
    public class UsePower : HeroActionHandlerBase
    {
        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config, 
            Action<string> onSuccess, Action<string> onFailure)
        {
            var heroClass = adoptedHero.GetClass();
            if (heroClass == null)
            {
                onFailure("{=85HYGklq}You don't have a class, so no powers are available!".Translate());
                return;
            }

            (bool canActivate, string failReason) = heroClass.ActivePower.CanActivate(adoptedHero);
            if (!canActivate)
            {
                onFailure("{=nXSUvuyD}You cannot activate your powers now: {FailReason}!"
                    .Translate(("FailReason", failReason)));
                return;
            }
            
            if (heroClass.ActivePower.IsActive(adoptedHero))
            {
                onFailure("{=o23xAj6M}Your powers are already active!".Translate());
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