using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero;
using JetBrains.Annotations;

namespace BLTBuffet
{
    [Description("Allows viewers to send a message that will appear in game"), UsedImplicitly]
    public class SendMessage : ActionHandlerBase
    {
        protected override Type ConfigType => typeof(Config);
        
        internal class Config
        {
            [Description("Alert sound to play for the message")]
            public Log.Sound AlertSound { get; set; } = Log.Sound.Notification1;
        }
        
        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Config) config;
            
            if (string.IsNullOrEmpty(context.Args))
            {
                onFailure("No message provided!");
                return;
            }
            
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            Log.ShowInformation((adoptedHero == null ? $"{context.UserName}: " : "") + context.Args,
                adoptedHero?.CharacterObject, settings.AlertSound);

            onSuccess("Sent");
        }
    }
}