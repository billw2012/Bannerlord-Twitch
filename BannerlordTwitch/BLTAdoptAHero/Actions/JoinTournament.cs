using System;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=EZPZtyXo}Join Tournament"),
     LocDescription("{=1h8OT5VD}Puts adopted heroes in queue for the next tournament"), 
     UsedImplicitly]
    internal class JoinTournament : ActionHandlerBase
    {
        private class Settings : IDocumentable
        {
            [LocDisplayName("{=7D2UBlGC}Gold Cost"),
             LocDescription("{=Upuhtmr6}Gold cost to join"), 
             PropertyOrder(4)]
            public int GoldCost { get; [UsedImplicitly] set; }

            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                if (GoldCost != 0) 
                    generator.PropertyValuePair("{=LnQoMDLT}Cost".Translate(), $"{GoldCost}{Naming.Gold}");
            }
        }
        
        protected override Type ConfigType => typeof(Settings);
        
        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Settings) config;
            
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(AdoptAHero.NoHeroMessage);
                return;
            }
            
            int availableGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(adoptedHero);
            if (availableGold < settings.GoldCost)
            {
                onFailure(Naming.NotEnoughGold(settings.GoldCost, availableGold));
                return;
            }

            (bool success, string reply) = BLTTournamentQueueBehavior.Current.AddToQueue(adoptedHero, context.IsSubscriber, settings.GoldCost);
            if (!success)
            {
                onFailure(reply);
            }
            else
            {
                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(adoptedHero, -settings.GoldCost);
                onSuccess(reply);
            }
        }

        public static void SetupGameMenus(CampaignGameStarter campaignGameSystemStarter)
        {
            campaignGameSystemStarter.AddGameMenuOption(
                "town_arena", "blt_join_tournament", 
                "{=jUcsHkqk}JOIN the viewer tournament".Translate(), 
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                    return BLTTournamentQueueBehavior.Current.TournamentAvailable;
                },
                _ =>
                {
                    BLTTournamentQueueBehavior.Current.JoinViewerTournament();
                    GameMenu.SwitchToMenu("town");
                }, 
                index: 2);
            campaignGameSystemStarter.AddGameMenuOption(
                "town_arena", "blt_watch_tournament", 
                "{=hlVvENgP}WATCH the viewer tournament".Translate(), 
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                    return BLTTournamentQueueBehavior.Current.TournamentAvailable;
                },
                _ =>
                {
                    BLTTournamentQueueBehavior.Current.WatchViewerTournament();
                    GameMenu.SwitchToMenu("town");
                }, 
                index: 3);
        }

        public static void OnGameEnd(Campaign campaign)
        {
            campaign.GetCampaignBehavior<BLTTournamentQueueBehavior>()?.Dispose();
        }
    }
}
