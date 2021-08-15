using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=kn4uxAWs}Add Gold To Hero"), 
     LocDescription("{=vkgPfcAu}Gives gold to the adopted hero"), 
     UsedImplicitly]
    internal class AddGoldToHero : IRewardHandler
    {
        private class Settings : IDocumentable
        {
            [LocDisplayName("{=DKfKt4qP}Amount"), 
             LocDescription("{=S2CeRdjR}How much gold to give the adopted hero"), 
             UsedImplicitly, Document]
            public int Amount { get; set; }

            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                generator.PropertyValuePair("Amount", $"{Amount}{Naming.Gold}");
            }
        }

        Type IRewardHandler.RewardConfigType => typeof(Settings);
        void IRewardHandler.Enqueue(ReplyContext context, object config)
        {
            var settings = (Settings)config;
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                ActionManager.NotifyCancelled(context, AdoptAHero.NoHeroMessage);
                return;
            }
            int newGold = BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(adoptedHero, settings.Amount);
            
            ActionManager.NotifyComplete(context, $"{Naming.Inc}{settings.Amount}{Naming.Gold}{Naming.To}{newGold}{Naming.Gold}");
        }

    }
}