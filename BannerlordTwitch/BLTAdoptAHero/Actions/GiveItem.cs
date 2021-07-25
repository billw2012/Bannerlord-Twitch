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
    [UsedImplicitly, Description("Allows viewers to give one of their own custom items to another viewer")]
    public class GiveItem : HeroCommandHandlerBase
    {
        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config, 
            Action<string> onSuccess, Action<string> onFailure)
        {
            var customItems = 
                BLTAdoptAHeroCampaignBehavior.Current.GetCustomItems(adoptedHero).ToList();

            if (!customItems.Any())
            {
                ActionManager.SendReply(context, $"You have no items to give");
                return;
            }

            if (string.IsNullOrWhiteSpace(context.Args))
            {
                ActionManager.SendReply(context, 
                    $"Usage: !{((Command)context.Source).Name} (recipient) (partial item name)");
                return;
            }

            var argParts = context.Args.Trim().Split(' ').ToList();
            if (argParts.Count == 1)
            {
                ActionManager.SendReply(context, 
                    $"Usage: !{((Command)context.Source).Name} (recipient) (partial item name)");
                return;
            }

            var targetHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(argParts[0]);
            if (targetHero == null)
            {
                ActionManager.SendReply(context, $"Couldn't find recipient \"{argParts[0]}\"");
                return;
            }

            string itemName = context.Args.Substring(argParts[0].Length + 1).Trim();
            var matchingItems = customItems.Where(i => i.GetModifiedItemName()
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
            var item = matchingItems.First();
            BLTAdoptAHeroCampaignBehavior.Current.TransferCustomItem(adoptedHero, targetHero, item, 0);
            
            ActionManager.SendNonReply(context, $"\"{item.GetModifiedItemName()}\" was transferred from @{adoptedHero.FirstName} to @{targetHero}");
        }
    }
}