using System;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=Q1QZbwR3}Auction Item"),
     LocDescription("{=024hOo3G}Allows viewers to auction custom items, for other viewers to bid on (make sure to add a bid command also)"),
     UsedImplicitly]
    public class AuctionItem : HeroCommandHandlerBase
    {
        private class Settings
        {
            [LocDisplayName("{=34GjlaWu}Auction Duration In Seconds"),
             LocDescription("{=zsvhQABf}How long the auction should last before the highest bidder wins"), 
             PropertyOrder(1), UsedImplicitly]
            public int AuctionDurationInSeconds { get; set; } = 60;
            
            [LocDisplayName("{=ssmJ9c5L}Auction Reminder Interval In Seconds"),
             LocDescription("{=ijkjWj5q}Interval at which to output a reminder of the auction"), 
             PropertyOrder(2), UsedImplicitly]
            public int AuctionReminderIntervalInSeconds { get; set; } = 15;
        }

        public override Type HandlerConfigType => typeof(Settings);

        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config, 
            Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Settings)config;

            if (BLTAdoptAHeroCampaignBehavior.Current.AuctionInProgress)
            {
                ActionManager.SendReply(context, 
                    "{=T2R35HHV}Another auction is already in progress".Translate());
                return;
            }

            if (string.IsNullOrWhiteSpace(context.Args))
            {
                ActionManager.SendReply(context, 
                    context.ArgsErrorMessage("{=}(custom item index) (reserve price)".Translate()));
                return;
            }

            var argParts = context.Args.Trim().Split(' ').ToList();
            if (argParts.Count != 2)
            {
                ActionManager.SendReply(context, "{=}(custom item index) (reserve price)".Translate());
                return;
            }

            (var element, string error) = BLTAdoptAHeroCampaignBehavior.Current.FindCustomItemByIndex(adoptedHero, argParts[0]);
            if (element.IsEqualTo(EquipmentElement.Invalid))
            {
                ActionManager.SendReply(context, error ?? "(unknown error)");
                return;
            }
            
            if (!int.TryParse(argParts[1], out int reservePrice) || reservePrice < 0)
            {
                ActionManager.SendReply(context, "{=mm1ay4I7}Invalid reserve price '{Arg}'".Translate(("Arg", argParts[1])));
                return;
            }

            BLTAdoptAHeroCampaignBehavior.Current.StartItemAuction(element, adoptedHero, reservePrice,
                settings.AuctionDurationInSeconds, settings.AuctionReminderIntervalInSeconds,
                s => ActionManager.SendNonReply(context, s));
            
            ActionManager.SendNonReply(context,
                "{=BH5rnHNq}Auction of '{ItemName}' is OPEN! Reserve price is {ReservePrice}{GoldIcon}, bidding closes in {AuctionDurationInSeconds} seconds."
                    .Translate(
                        ("ItemName", RewardHelpers.GetItemNameAndModifiers(element)),
                        ("ReservePrice", reservePrice),
                        ("GoldIcon", Naming.Gold),
                        ("AuctionDurationInSeconds", settings.AuctionDurationInSeconds)
                    ));
        }
    }
    
    [LocDisplayName("{=rBAvqAh7}Bid On Item"),
     LocDescription("{=XuvGyCwD}Allows viewers bid on an active custom item auction (make sure to add an auction command also)"), 
     UsedImplicitly]
    public class BidOnItem : HeroCommandHandlerBase
    {
        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config, 
            Action<string> onSuccess, Action<string> onFailure)
        {
            if (string.IsNullOrWhiteSpace(context.Args))
            {
                ActionManager.SendReply(context, 
                    context.ArgsErrorMessage("{=ewjqhPqj}(bid amount)".Translate()));
                return;
            }

            if (!int.TryParse(context.Args, out int bid) || bid < 0)
            {
                ActionManager.SendReply(context, "{=dgG5WPrC}Invalid bid amount".Translate());
                return;
            }

            (bool _, string description) = BLTAdoptAHeroCampaignBehavior.Current.AuctionBid(adoptedHero, bid);

            ActionManager.SendReply(context, description);
        }
    }
}