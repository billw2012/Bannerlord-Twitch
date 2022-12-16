using System;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=0YSdK3zK}Name Item"),
     LocDescription("{=k7vnOEIN}Allow viewer to name custom items"),
     UsedImplicitly]
    public class NameItem : ICommandHandler
    {
        public Type HandlerConfigType => null;

        public void Execute(ReplyContext context, object config)
        {
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                ActionManager.SendReply(context, AdoptAHero.NoHeroMessage);
                return;
            }

            var nameableItems = BLTAdoptAHeroCampaignBehavior.Current.GetCustomItems(adoptedHero)
                .Where(e => BLTCustomItemsCampaignBehavior.Current.ItemCanBeNamed(e.ItemModifier))
                .ToList()
                ;

            if (!nameableItems.Any())
            {
                ActionManager.SendReply(context, 
                    "{=rXVOIMri}You have no nameable items".Translate());
                return;
            }

            if (string.IsNullOrEmpty(context.Args))
            {
                ActionManager.SendReply(context, 
                    "{=Xb3eM5o7}You will rename your {Item}"
                        .Translate(("Item", nameableItems.First().GetModifiedItemName())));
                return;
            }

            string previousName = nameableItems.First().GetModifiedItemName().ToString();

            BLTCustomItemsCampaignBehavior.Current.NameItem(nameableItems.First().ItemModifier, context.Args);
            ActionManager.SendReply(context, 
                "{=iqNEr6Y7}{PreviousName} renamed to {NewName}"
                    .Translate(("PreviousName", previousName), ("NewName", context.Args))
                );
        }
    }
}