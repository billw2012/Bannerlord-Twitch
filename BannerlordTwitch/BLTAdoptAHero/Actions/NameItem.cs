using System;
using System.Linq;
using System.Windows.Input;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
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

            var nameableItems = adoptedHero.BattleEquipment
                .YieldFilledEquipmentSlots()
                .Where(e => BLTCustomItemsCampaignBehavior.Current.ItemCanBeNamed(e.ItemModifier))
                .ToList()
                ;

            if (!nameableItems.Any())
            {
                ActionManager.SendReply(context, $"You have no nameable items");
                return;
            }

            if (string.IsNullOrEmpty(context.Args))
            {
                ActionManager.SendReply(context, $"You will rename your {nameableItems.First().GetModifiedItemName()}");
                return;
            }

            string previousName = nameableItems.First().GetModifiedItemName().ToString();

            BLTCustomItemsCampaignBehavior.Current.NameItem(nameableItems.First().ItemModifier, context.Args);
            ActionManager.SendReply(context, $"{previousName} renamed to {context.Args}");
        }
    }
}