using System;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;

namespace BLTBuffet
{
    [Description("Allows viewers to send a message that will appear in game"), UsedImplicitly]
    public class SendMessage : ActionHandlerBase
    {
        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            if (string.IsNullOrEmpty(context.Args))
            {
                onFailure("No message provided!");
                return;
            }
            
            var hero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            Log.ShowInformation((hero == null ? $"{context.UserName}: " : "") + context.Args, hero?.CharacterObject, Log.Sound.Notification1);

            onSuccess("Sent");
        }
    }
}