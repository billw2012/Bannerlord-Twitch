using System;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
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
                ActionManager.SendReply(context, 
                    context.ArgsErrorMessage("{=8XQwqd4f}(team) (gold)".Translate()));
                return;
            }

            (int? gold, string team) ParseArgs(string[] args)
            {
                if (string.Equals(args[0], "{=hHekZwYB}all".Translate(), StringComparison.CurrentCultureIgnoreCase))
                {
                    return (BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(adoptedHero), args[1]);
                }
                else if (string.Equals(args[1], "{=hHekZwYB}all".Translate(), StringComparison.CurrentCultureIgnoreCase))
                {
                    return (BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(adoptedHero), args[0]);
                } 
                else if (int.TryParse(args[0], out int gold0))
                {
                    return (gold0, args[1]);
                }
                else if (int.TryParse(args[1], out int gold1))
                {
                    return (gold1, args[0]);
                }
                return default;
            }

            (int? gold, string team) = ParseArgs(parts);
            if(gold is null or <= 0)
            {
                ActionManager.SendReply(context, "{=GiU7feEu}Invalid gold argument".Translate());
                return;
            }

            (bool success, string failReason) 
                = BLTTournamentBetMissionBehavior.Current?.PlaceBet(adoptedHero, team, gold.Value) 
                  ?? (false, "{=3AQKsF9f}Betting not active".Translate());
            
            if (!success)
            {
                ActionManager.SendReply(context, failReason);
            }
            else
            {
                ActionManager.SendReply(context,  "{=9tlhaGyH}Bet {GoldAmount}{GoldIcon} on {Team}"
                    .Translate(("GoldAmount", gold), ("GoldIcon", Naming.Gold), ("Team", team)));
            }
        }
    }
}