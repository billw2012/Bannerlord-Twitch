using System;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero
{
    public abstract class HeroActionHandlerBase : ActionHandlerBase
    {
        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            ExecuteInternal(adoptedHero, context, config, onSuccess, onFailure);
        }

        protected abstract void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config,
            Action<string> onSuccess, Action<string> onFailure);
    }
}