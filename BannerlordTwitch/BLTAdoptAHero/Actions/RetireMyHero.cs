using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=sZhEiXyn}Retire Hero"),
     LocDescription("{=Eh0m4R54}Retires the hero, allowing the viewer to adopt another (if that is enabled)"), 
     UsedImplicitly]
    public class RetireMyHero : ActionHandlerBase
    {
        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            if (context.Args != "{=xSbB2Zw5}yes".Translate())
            {
                onFailure("{=0qVpLYfb}You must enter '{Prompt}' at the prompt to retire your hero"
                    .Translate(("Prompt", "{=xSbB2Zw5}yes".Translate())));
                return;
            }
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(AdoptAHero.NoHeroMessage);
                return;
            }
            if (Mission.Current != null)
            {
                onFailure("{=po20lHyz}You cannot retire your hero, as a mission is active!".Translate());
                return;
            }
            Log.ShowInformation("{=ihG4fs1r}{Name} has retired!"
                .Translate(("Name", adoptedHero.Name)), adoptedHero.CharacterObject, Log.Sound.Horns3);
            BLTAdoptAHeroCampaignBehavior.Current.RetireHero(adoptedHero);
        }
    }
}