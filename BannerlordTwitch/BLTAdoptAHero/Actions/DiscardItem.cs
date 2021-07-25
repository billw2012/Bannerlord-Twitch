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
    [UsedImplicitly, Description("Allows viewers to discard one of their own custom items")]
    public class DiscardItem : HeroCommandHandlerBase
    {
        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config, 
            Action<string> onSuccess, Action<string> onFailure)
        {
            var customItems = 
                BLTAdoptAHeroCampaignBehavior.Current.GetCustomItems(adoptedHero).ToList();

            if (!customItems.Any())
            {
                ActionManager.SendReply(context, $"You have no items to discard");
                return;
            }

            if (string.IsNullOrWhiteSpace(context.Args))
            {
                ActionManager.SendReply(context, 
                    $"Usage: !{((Command)context.Source).Name} (partial item name)");
                return;
            }

            var matchingItems = customItems.Where(i => i.GetModifiedItemName()
                    .ToString().IndexOf(context.Args, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .ToList();

            if (matchingItems.Count == 0)
            {
                ActionManager.SendReply(context, $"No items found matching \"{context.Args}\"");
                return;
            }
            if (matchingItems.Count > 1)
            {
                ActionManager.SendReply(context, $"{matchingItems.Count} items found matching \"{context.Args}\", be more specific");
                return; 
            }
            var item = matchingItems.First();
            BLTAdoptAHeroCampaignBehavior.Current.DiscardCustomItem(adoptedHero, item);
            
            ActionManager.SendReply(context, $"\"{item.GetModifiedItemName()}\" was discarded");
        }
    }
}