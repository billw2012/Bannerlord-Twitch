using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero;
using JetBrains.Annotations;

namespace BLTBuffet
{
    [LocDisplayName("{=rXQWpj95}Send Message"),
     LocDescription("{=htpylYKb}Allows viewers to send a message that will appear in game"), 
     UsedImplicitly]
    public class SendMessage : ActionHandlerBase
    {
        protected override Type ConfigType => typeof(Config);
        
        internal class Config
        {
            [LocDisplayName("{=Anvcnpbh}Alert Sound"),
             LocDescription("{=V36Hg85n}Alert sound to play for the message"),
             UsedImplicitly]
            public Log.Sound AlertSound { get; set; } = Log.Sound.Notification1;
        }
        
        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Config) config;
            
            if (string.IsNullOrEmpty(context.Args))
            {
                onFailure("{=cA8NQlDm}No message provided!".Translate());
                return;
            }
            
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            Log.ShowInformation((adoptedHero == null ? $"{context.UserName}: " : "") + context.Args,
                adoptedHero?.CharacterObject, settings.AlertSound);

            onSuccess("{=tFHfVCDK}Sent".Translate());
        }
    }
}