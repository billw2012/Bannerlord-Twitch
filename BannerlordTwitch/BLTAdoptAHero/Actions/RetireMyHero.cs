using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Retires the hero, allowing the viewer to adopt another (if that is enabled)")]
    public class RetireMyHero : ActionHandlerBase
    {
        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            if (context.Args != "yes")
            {
                onFailure($"You must enter 'yes' at the prompt to retire your hero");
                return;
            }
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            if (Mission.Current != null)
            {
                onFailure($"You cannot retire your hero, as a mission is active!");
                return;
            }
            Log.ShowInformation($"{adoptedHero.Name} has retired!", adoptedHero.CharacterObject, Log.Sound.Horns3);
            BLTAdoptAHeroCampaignBehavior.Current.RetireHero(adoptedHero);
        }
    }
}