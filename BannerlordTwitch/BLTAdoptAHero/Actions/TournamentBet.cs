using System;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    public class TournamentBet : ICommandHandler
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

            string[] parts = context.Args?.Split(' ');
            if (parts?.Length != 2)
            {
                ActionManager.SendReply(context, $"Arguments: team gold");
                return;
            }

            int gold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(adoptedHero);
            int nameIdx = -1;
            if (parts[0].ToLower() == "all" || int.TryParse(parts[0], out gold))
            {
                nameIdx = 1;
            }
            else if(parts[1].ToLower() == "all" || int.TryParse(parts[1], out gold))
            {
                nameIdx = 0;
            }
            if(gold == 0 || nameIdx == -1)
            {
                ActionManager.SendReply(context, $"Invalid gold argument");
                return;
            }
            
            string team = parts[nameIdx].ToLower();
            (bool success, string failReason) = BLTTournamentBetMissionBehavior.Current?.PlaceBet(adoptedHero, team, gold) 
                                                ?? (false, "Betting not active");
            if (!success)
            {
                ActionManager.SendReply(context, failReason);
            }
            else
            {
                ActionManager.SendReply(context, $"Bet {gold}{Naming.Gold} on {team}");
            }
        }
    }
}