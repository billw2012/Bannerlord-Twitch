using System;
using System.Linq;
using System.Windows.Input;
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

            string team = parts[0].ToLower();
            if (team == "")
            {
                ActionManager.SendReply(context, $"Invalid team name");
                return;
            }

            if (!int.TryParse(parts[1], out int gold) || gold < 1)
            {
                ActionManager.SendReply(context, $"Invalid gold argument");
                return;
            }

            (bool success, string failReason) = BLTTournamentQueueBehavior.Current.PlaceBet(adoptedHero, team, gold);
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