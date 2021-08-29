using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTBuffet
{
    [LocDisplayName("{=HSuPuNDk}Add Gold To Player"),
     LocDescription("{=wqR23RYf}Allows viewers to give the player gold, either for channel points and/or from their own hero"),
     UsedImplicitly]
    public class AddGoldToPlayer : ActionHandlerBase
    {
        protected override Type ConfigType => typeof(Config);

        internal class Config
        {
            [LocDisplayName("{=F51J5Qd5}Gold Amount To Give"),
             LocDescription("{=Jg6qoYth}How much gold to give the player"), 
             PropertyOrder(1), UsedImplicitly]
            public int GoldAmountToGive { get; set; } = 10000;
            
            [LocDisplayName("{=yGnwXshi}From Adopted Hero"),
             LocDescription("{=uL8hjmZ9}Whether to take the gold from the viewers adopted hero"), 
             PropertyOrder(2), UsedImplicitly]
            public bool FromAdoptedHero { get; set; }

            [LocDisplayName("{=sNkcGnrw}Alert Sound"),
             LocDescription("{=TSi4MZiT}Alert sound to play for the message"), 
             PropertyOrder(3), UsedImplicitly]
            public Log.Sound AlertSound { get; set; } = Log.Sound.Notification1;
        }

        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Config) config;
            
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (settings.FromAdoptedHero)
            {
                if (adoptedHero == null)
                {
                    onFailure(AdoptAHero.NoHeroMessage);
                    return;
                }

                int availableGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(adoptedHero);
                if (availableGold < settings.GoldAmountToGive)
                {
                    onFailure(Naming.NotEnoughGold(settings.GoldAmountToGive, availableGold));
                    return;
                }
                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(adoptedHero, -settings.GoldAmountToGive);
            }
            Log.ShowInformation(
                (string.IsNullOrEmpty(context.Args) 
                    ? "{=awjXw6yr}{UserName} gives you {GoldAmountToGive} gold"
                    : "{=jOqdrS7h}{UserName} gives you {GoldAmountToGive} gold: '{Message}'")
                .Translate(
                    ("UserName", context.UserName),
                    ("GoldAmountToGive", settings.GoldAmountToGive),
                    ("Message", context.Args)
                ), 
                adoptedHero?.CharacterObject, settings.AlertSound);
            
            Hero.MainHero.ChangeHeroGold(settings.GoldAmountToGive);

            onSuccess("{=ziWoNEec}Sent {GoldAmountToGive}{GoldIcon} to {PlayerName}"
                .Translate(
                    ("GoldAmountToGive", settings.GoldAmountToGive),
                    ("GoldIcon", Naming.Gold),
                    ("PlayerName", Hero.MainHero.Name)));
        }
    }
}