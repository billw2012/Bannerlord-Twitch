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
    [LocDisplayName("{=nAPjfF4C}Give Item"),
     LocDescription("{=u9Jx6QUr}Allows viewers to give one of their own custom items to another viewer"), 
     UsedImplicitly]
    public class GiveItem : HeroCommandHandlerBase
    {
        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config, 
            Action<string> onSuccess, Action<string> onFailure)
        {
            var customItems = 
                BLTAdoptAHeroCampaignBehavior.Current.GetCustomItems(adoptedHero).ToList();

            if (!customItems.Any())
            {
                ActionManager.SendReply(context, "{=Tfj1U5BB}You have no items to give".Translate());
                return;
            }

            if (string.IsNullOrWhiteSpace(context.Args))
            {
                ActionManager.SendReply(context,
                    context.ArgsErrorMessage("{=BClUoR9H}(recipient) (partial item name)".Translate()));
                return;
            }

            var argParts = context.Args.Trim().Split(' ').ToList();
            if (argParts.Count == 1)
            {
                ActionManager.SendReply(context, 
                    context.ArgsErrorMessage("{=BClUoR9H}(recipient) (partial item name)".Translate()));
                return;
            }

            var targetHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(argParts[0]);
            if (targetHero == null)
            {
                ActionManager.SendReply(context, 
                    "{=vDsYxMKq}Couldn't find recipient '{Name}'".Translate(("Name",argParts[0])));
                return;
            }

            string itemName = context.Args.Substring(argParts[0].Length + 1).Trim();
            var matchingItems = customItems.Where(i => i.GetModifiedItemName()
                    .ToString().IndexOf(itemName, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .ToList();

            if (matchingItems.Count == 0)
            {
                ActionManager.SendReply(context,
                    "{=p0urrIvR}No items found matching '{Args}'".Translate(("Args", itemName)));
                return;
            }
            if (matchingItems.Count > 1)
            {
                ActionManager.SendReply(context, 
                    "{=Pzo2UJrl}{Count} items found matching '{Args}', be more specific"
                        .Translate(("Count", matchingItems.Count), ("Args", itemName)));
                return; 
            }
            var item = matchingItems.First();
            BLTAdoptAHeroCampaignBehavior.Current.TransferCustomItem(adoptedHero, targetHero, item, 0);
            
            ActionManager.SendNonReply(context,
                "{=E1o7b5ht}'{ItemName}' was transferred from @{FromHero} to @{ToHero}"
                    .Translate(
                        ("ItemName", item.GetModifiedItemName()),
                        ("FromHero", adoptedHero.FirstName), 
                        ("ToHero", targetHero)));
        }
    }
}