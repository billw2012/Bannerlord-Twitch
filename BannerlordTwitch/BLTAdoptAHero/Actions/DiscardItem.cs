using System;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=fmftNyHh}Discard Item"),
     LocDescription("{=f3LrrLHP}Allows viewers to discard one of their own custom items"), 
     UsedImplicitly]
    public class DiscardItem : HeroCommandHandlerBase
    {
        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config, 
            Action<string> onSuccess, Action<string> onFailure)
        {
            var customItems = 
                BLTAdoptAHeroCampaignBehavior.Current.GetCustomItems(adoptedHero).ToList();

            if (!customItems.Any())
            {
                ActionManager.SendReply(context, "{=oXQ4En4P}You have no items to discard".Translate());
                return;
            }

            if (string.IsNullOrWhiteSpace(context.Args))
            {
                ActionManager.SendReply(context, 
                    context.ArgsErrorMessage("{=by80aboy}(partial item name)".Translate()));
                return;
            }

            var matchingItems = customItems.Where(i => i.GetModifiedItemName()
                    .ToString().IndexOf(context.Args, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .ToList();

            if (matchingItems.Count == 0)
            {
                ActionManager.SendReply(context,
                    "{=p0urrIvR}No items found matching '{Args}'".Translate(("Args", context.Args)));
                return;
            }
            if (matchingItems.Count > 1)
            {
                ActionManager.SendReply(context, 
                    "{=Pzo2UJrl}{Count} items found matching '{Args}', be more specific"
                        .Translate(("Count", matchingItems.Count), ("Args", context.Args)));
                return;
            }
            var item = matchingItems.First();
            BLTAdoptAHeroCampaignBehavior.Current.DiscardCustomItem(adoptedHero, item);
            
            ActionManager.SendReply(context, 
                "{=bNqd3AzN}'{ItemName}' was discarded"
                    .Translate(("ItemName", item.GetModifiedItemName())));
        }
    }
}