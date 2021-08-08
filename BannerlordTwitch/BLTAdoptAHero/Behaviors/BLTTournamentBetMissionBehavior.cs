using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using BLTAdoptAHero.Util;
using HarmonyLib;
using SandBox.TournamentMissions.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    [HarmonyPatch]
    public class BLTTournamentBetMissionBehavior : AutoMissionBehavior<BLTTournamentBetMissionBehavior>
    {
        protected override void OnEndMission()
        {
            // Ensure the betting is closed
            RefundBets();
            CurrentBettingState = BettingState.none;
            activeBets = null;
            TournamentHub.UpdateBets();
        }
            
        public List<int> GetTotalBets()
        {
            var tournamentBehavior = MissionState.Current?.CurrentMission?.GetMissionBehaviour<TournamentBehavior>();
            if (tournamentBehavior != null && activeBets != null)
            {
                int teamsCount = tournamentBehavior.CurrentMatch.Teams.Count();
                return Enumerable.Range(0, teamsCount).Select(idx =>  
                        activeBets.Values
                            .Where(b => b.team == idx)
                            .Sum(b => b.bet))
                    .ToList();
            }
            return new();
        }

        public enum BettingState
        {
            none,
            open,
            closed,
            disabled,
        }

        public BettingState CurrentBettingState { get; private set; }

        private class TeamBet
        {
            public int team;
            public int bet;
        }
            
        private Dictionary<Hero, TeamBet> activeBets;

        public void OpenBetting(TournamentBehavior tournamentBehavior)
        {
            if (BLTAdoptAHeroModule.TournamentConfig.EnableBetting 
                && tournamentBehavior.CurrentMatch != null
                && (tournamentBehavior.CurrentRoundIndex == 3 || !BLTAdoptAHeroModule.TournamentConfig.BettingOnFinalOnly))
            {
                var teams = TournamentHelpers.TeamNames.Take(tournamentBehavior.CurrentMatch.Teams.Count());
                string round = tournamentBehavior.CurrentRoundIndex < 3
                    ? $"round {tournamentBehavior.CurrentRoundIndex + 1}"
                    : "final";
                string msg = $"Betting is now OPEN for {round} match: {string.Join(" vs ", teams)}!";
                Log.LogFeedMessage(msg);
                ActionManager.SendChat(msg);
                activeBets = new();
                CurrentBettingState = BettingState.open;
            }
            else
            {
                CurrentBettingState = BettingState.disabled;
            }
            TournamentHub.UpdateBets();
        }
            
        public (bool success, string failReason) PlaceBet(Hero hero, string team, int bet)
        {
            var tournamentBehavior = Mission.Current?.GetMissionBehaviour<TournamentBehavior>();
            if (tournamentBehavior == null)
            {
                return (false, "Tournament is not active");
            }

            if (!BLTAdoptAHeroModule.TournamentConfig.EnableBetting)
            {
                return (false, "Betting is disabled");
            }
                
            if (CurrentBettingState == BettingState.closed)
            {
                return (false, "Betting is closed");
            }
                
            if (tournamentBehavior.CurrentRoundIndex != 3 && BLTAdoptAHeroModule.TournamentConfig.BettingOnFinalOnly)
            {
                return (false, "Betting is only allowed on the final");
            }

            if (CurrentBettingState != BettingState.open)
            {
                return (false, "Betting is not open");
            }

            int teamsCount = tournamentBehavior.CurrentMatch.Teams.Count();
            string[] activeTeams = TournamentHelpers.TeamNames.Take(teamsCount).ToArray();
            int teamIdx = activeTeams.IndexOf(team.ToLower());
            if (teamIdx == -1)
            {
                return (false, $"Team name must be one of {string.Join(", ", activeTeams)}");
            }
                
            if (activeBets.TryGetValue(hero, out var existingBet))
            {
                if (existingBet.team != teamIdx)
                {
                    return (false, "You can only bet on one team");
                }
            }
                
            int heroGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(hero);
            if (heroGold < bet)
            {
                return (false, Naming.NotEnoughGold(bet, heroGold));
            }

            if (existingBet != null)
            {
                existingBet.bet += bet;
            }
            else
            {
                activeBets.Add(hero, new() {team = teamIdx, bet = bet});
            }
                
            // Take the actual money
            BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(hero, -bet);

            TournamentHub.UpdateBets();
                
            return (true, null);
        }

        private void CloseBetting()
        {
            // We use this being non-null as an indicator that betting was active
            if (activeBets != null)
            {
                var groupedBets = activeBets.Values
                    .Select(b => (name: TournamentHelpers.TeamNames[b.team], b.bet))
                    .GroupBy(b => b.name)
                    .ToList();

                if (groupedBets.Count == 1)
                {
                    // refund bets if only one team was bet on
                    RefundBets();
                    activeBets = null;
                    CurrentBettingState = BettingState.disabled;
                    Log.LogFeedMessage($"Betting is now CLOSED: only one team bet on, bets refunded");
                    ActionManager.SendChat($"Betting is now CLOSED: only one team bet on, bets refunded");
                }
                else if (!groupedBets.Any())
                {
                    activeBets = null;
                    CurrentBettingState = BettingState.disabled;
                    Log.LogFeedMessage($"Betting is now CLOSED: no bets placed");
                    ActionManager.SendChat($"Betting is now CLOSED: no bets placed");
                }
                else 
                {
                    CurrentBettingState = BettingState.closed;
                    var betTotals = activeBets.Values
                            .Select(b => (name: TournamentHelpers.TeamNames[b.team], b.bet))
                            .GroupBy(b => b.name)
                            .Select(g => $"{g.Key} {g.Select(x => x.bet).Sum()}{Naming.Gold}")
                            .ToList()
                        ;
                    string msg = $"Betting is now CLOSED: {string.Join(", ", betTotals)}";
                    Log.LogFeedMessage(msg);
                    ActionManager.SendChat(msg);
                }
            }
            else
            {
                CurrentBettingState = BettingState.disabled;
            }
                
            TournamentHub.UpdateBets();
        }

        private void RefundBets()
        {
            if (activeBets != null)
            {
                foreach (var (hero, bet) in activeBets)
                {
                    Log.LogFeedResponse(hero.FirstName.ToString(), $"REFUNDED {bet.bet}{Naming.Gold} bet");
                    BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(hero, bet.bet);
                }

                activeBets.Clear();
            }
        }

        public void CompleteBetting(TournamentMatch lastMatch)
        {
            if (activeBets != null)
            {
                double totalBet = activeBets.Values.Sum(v => v.bet);

                var allWonBets = activeBets
                    .Where(kv => lastMatch.Winners.Contains(lastMatch.Teams.ElementAt(kv.Value.team).Participants.First()))
                    .Select(kv => (
                        hero: kv.Key,
                        bet: kv.Value.bet
                    ))
                    .ToList();

                double winningTotalBet = allWonBets.Sum(v => v.bet);

                foreach ((var hero, int bet) in allWonBets.OrderByDescending(b => b.bet))
                {
                    int winnings = (int) (totalBet * bet / winningTotalBet);
                    int newGold = BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(hero, winnings);
                    Log.LogFeedResponse(hero.FirstName.ToString(),
                        $"WON BET {Naming.Inc}{winnings}{Naming.Gold}{Naming.To}{newGold}{Naming.Gold}");
                }

                activeBets = null;
            }
                
            TournamentHub.UpdateBets();
        }

        private void CreateTorunamentTreePostfixImpl(TournamentBehavior tournamentBehavior)
        {
            tournamentBehavior.Rounds[0] = BLTAdoptAHeroModule.TournamentConfig.Round1Type
                .GetRandomRound(tournamentBehavior.Rounds[0], tournamentBehavior.TournamentGame.Mode); 
            tournamentBehavior.Rounds[1] = BLTAdoptAHeroModule.TournamentConfig.Round2Type
                .GetRandomRound(tournamentBehavior.Rounds[1], tournamentBehavior.TournamentGame.Mode); 
            tournamentBehavior.Rounds[2] = BLTAdoptAHeroModule.TournamentConfig.Round3Type
                .GetRandomRound(tournamentBehavior.Rounds[2], tournamentBehavior.TournamentGame.Mode);
        }
        
        #region Patches
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentBehavior), "AfterStart")]
        public static void AfterStartPostfix(TournamentBehavior __instance)
        {
            // Only called at the start of the tournament
            SafeCallStatic(() => Current?.OpenBetting(__instance));
        }
        
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentBehavior), "CreateTorunamentTree")]
        public static void CreateTorunamentTreePostfix(TournamentBehavior __instance)
        {
            SafeCallStatic(() => Current?.CreateTorunamentTreePostfixImpl(__instance));
        }

        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(TournamentBehavior), "StartMatch")]
        public static void StartMatchPrefix(TournamentBehavior __instance)
        {
            SafeCallStatic(() => Current?.CloseBetting());
        }

        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(TournamentBehavior), "SkipMatch")]
        public static void SkipMatchPrefix(TournamentBehavior __instance)
        {
            SafeCallStatic(() => Current?.CloseBetting());
        }
        #endregion
    }
}