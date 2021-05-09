using System;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;

namespace BLTAdoptAHero
{
    // WIP
    // public class Marry : ActionHandlerBase
    // {
    //     private struct Settings
    //     {
    //         public int GoldCost { get; set; }
    //         public bool FormNewClan { get; set; }
    //         public int NewClanTier { get; set; }
    //     }
    //     
    //     protected override Type ConfigType => typeof(Settings);
    //     
    //     private List<> 
    //     protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
    //     {
    //         var settings = (Settings)config;
    //         var adoptedHero = AdoptAHero.GetAdoptedHero(context.UserName);
    //         if (adoptedHero == null)
    //         {
    //             onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
    //             return;
    //         }
    //     }
    // }
}