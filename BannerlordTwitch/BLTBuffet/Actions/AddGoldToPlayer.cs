using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTBuffet
{
    [Description("Allows viewers to give the player gold, either for channel points and/or from their own hero"), UsedImplicitly]
    public class AddGoldToPlayer : ActionHandlerBase
    {
        protected override Type ConfigType => typeof(Config);

        internal class Config
        {
            [Description("How much gold to give the player"), PropertyOrder(1)]
            public int GoldAmountToGive { get; set; } = 10000;
            
            [Description("Whether to take the gold from the viewers adopted hero"), PropertyOrder(2)]
            public bool FromAdoptedHero { get; set; }

            [Description("Alert sound to play for the message"), PropertyOrder(3)]
            public Log.Sound AlertSound { get; set; } = Log.Sound.Notification1;
        }

        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Config) config;
            
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (settings.FromAdoptedHero)
            {
                if (adoptedHero == null)
                {
                    onFailure(AdoptAHero.NoHeroMessage);
                    return;
                }

                int availableGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(adoptedHero);
                if (availableGold < settings.GoldAmountToGive)
                {
                    onFailure(Naming.NotEnoughGold(settings.GoldAmountToGive, availableGold));
                    return;
                }
                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(adoptedHero, -settings.GoldAmountToGive);
            }
            Log.ShowInformation($"{context.UserName} gives you {settings.GoldAmountToGive} gold" +
                                (string.IsNullOrEmpty(context.Args) ? "" : $": \"{context.Args}\""), adoptedHero?.CharacterObject, settings.AlertSound);
            
            Hero.MainHero.ChangeHeroGold(settings.GoldAmountToGive);

            onSuccess($"Sent {settings.GoldAmountToGive}{Naming.Gold} to {Hero.MainHero.Name}");
        }
    }
}