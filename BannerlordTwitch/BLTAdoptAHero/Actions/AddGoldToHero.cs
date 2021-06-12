using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Gives gold to the adopted hero")]
    internal class AddGoldToHero : IRewardHandler
    {
        private class Settings
        {
            [Description("How much gold to give the adopted hero")]
            public int Amount { get; set; }
        }

        Type IRewardHandler.RewardConfigType => typeof(Settings);
        void IRewardHandler.Enqueue(ReplyContext context, object config)
        {
            var settings = (Settings)config;
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                ActionManager.NotifyCancelled(context, Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            int newGold = BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(adoptedHero, settings.Amount);
            
            ActionManager.NotifyComplete(context, $"{Naming.Inc}{settings.Amount}{Naming.Gold}{Naming.To}{newGold}{Naming.Gold}");
        }
    }
}