using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

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
            if (string.IsNullOrWhiteSpace(context.Args))
            {
                ActionManager.SendReply(context,
                    context.ArgsErrorMessage("{=BClUoR9H}(custom item index) (recipient)".Translate()));
                return;
            }

            var argParts = context.Args.Trim().Split(' ').ToList();
            if (argParts.Count != 2)
            {
                ActionManager.SendReply(context, 
                    context.ArgsErrorMessage("{=BClUoR9H}(custom item index) (recipient)".Translate()));
                return;
            }
            
            (var element, string error) = BLTAdoptAHeroCampaignBehavior.Current.FindCustomItemByIndex(adoptedHero, argParts[0]);
            if (element.IsEqualTo(EquipmentElement.Invalid))
            {
                ActionManager.SendReply(context, error ?? "(unknown error)");
                return;
            }

            var targetHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(argParts[1]);
            if (targetHero == null)
            {
                ActionManager.SendReply(context, 
                    "{=vDsYxMKq}Couldn't find recipient '{Name}'".Translate(("Name", argParts[1])));
                return;
            }

            if (element.Equals(EquipmentElement.Invalid))
            {
                ActionManager.SendReply(context, error ?? "(unknown error)");
                return;
            }
            
            BLTAdoptAHeroCampaignBehavior.Current.TransferCustomItem(adoptedHero, targetHero, element, 0);
            
            ActionManager.SendNonReply(context,
                "{=E1o7b5ht}'{ItemName}' was transferred from @{FromHero} to @{ToHero}"
                    .Translate(
                        ("ItemName", element.GetModifiedItemName()),
                        ("FromHero", adoptedHero.FirstName), 
                        ("ToHero", targetHero.FirstName)));
        }
    }
}