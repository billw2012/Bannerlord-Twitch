using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    public class AuctionItem : HeroCommandHandlerBase
    {
        private class Settings
        {
            [Description("How long the auction should last before the highest bidder wins"), 
             PropertyOrder(1), UsedImplicitly]
            public int AuctionDurationInSeconds { get; set; } = 60;
            
            [Description("Interval at which to output a reminder of the auction"), 
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
                ActionManager.SendReply(context, $"Another auction is already in progress");
                return;
            }
            
            var auctionItems = 
                BLTAdoptAHeroCampaignBehavior.Current.GetCustomItems(adoptedHero).ToList();

            if (!auctionItems.Any())
            {
                ActionManager.SendReply(context, $"You have no items to auction");
                return;
            }

            if (string.IsNullOrWhiteSpace(context.Args))
            {
                ActionManager.SendReply(context, 
                    $"Usage: !{((Command)context.Source).Name} (reserve price) (item name)");
                return;
            }

            var argParts = context.Args.Trim().Split(' ').ToList();
            if (argParts.Count == 1)
            {
                ActionManager.SendReply(context, 
                    $"Usage: !{((Command)context.Source).Name} (reserve price) (item name)");
                return;
            }

            if (!int.TryParse(argParts[0], out int reservePrice) || reservePrice < 0)
            {
                ActionManager.SendReply(context, $"Invalid reserve price \"{argParts[0]}\"");
                return;
            }

            string itemName = context.Args.Substring(argParts[0].Length + 1).Trim();
            var matchingItems = auctionItems.Where(i => i.GetModifiedItemName()
                    .ToString().IndexOf(itemName, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .ToList();

            if (matchingItems.Count == 0)
            {
                ActionManager.SendReply(context, $"No items found matching \"{itemName}\"");
                return;
            }
            if (matchingItems.Count > 1)
            {
                ActionManager.SendReply(context, $"{matchingItems.Count} items found matching \"{itemName}\", be more specific");
                return; 
            }
            
            BLTAdoptAHeroCampaignBehavior.Current.StartItemAuction(matchingItems.First(), adoptedHero, reservePrice,
                settings.AuctionDurationInSeconds, settings.AuctionReminderIntervalInSeconds,
                s => ActionManager.SendNonReply(context, s));
            
            ActionManager.SendNonReply(context, 
                $"Auction of \"{itemName}\" is OPEN! Reserve price is {reservePrice}{Naming.Gold}, " +
                $"bidding closes in {settings.AuctionDurationInSeconds} seconds.");
        }
    }
    
    [UsedImplicitly]
    public class BidOnItem : HeroCommandHandlerBase
    {
        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config, 
            Action<string> onSuccess, Action<string> onFailure)
        {
            if (string.IsNullOrWhiteSpace(context.Args))
            {
                ActionManager.SendReply(context, 
                    $"Usage: !{((Command)context.Source).Name} (bid amount)");
                return;
            }

            if (!int.TryParse(context.Args, out int bid) || bid < 0)
            {
                ActionManager.SendReply(context, $"Invalid bid amount");
                return;
            }

            (bool _, string description) = BLTAdoptAHeroCampaignBehavior.Current.AuctionBid(adoptedHero, bid);

            ActionManager.SendReply(context, description);
        }
    }
}