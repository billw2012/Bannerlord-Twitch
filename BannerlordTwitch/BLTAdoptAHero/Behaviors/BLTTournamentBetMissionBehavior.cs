using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using BLTAdoptAHero.Util;
using HarmonyLib;
//using SandBox.TournamentMissions.Missions;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
//using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.CampaignSystem.TournamentGames;
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
            var tournamentBehavior = MissionState.Current?.CurrentMission?.GetMissionBehavior<TournamentBehavior>();
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
                string msg = tournamentBehavior.CurrentRoundIndex < 3
                    ? "{=wgGkXkyh}Betting is now OPEN for round {RoundIndex} match: {Teams}!"
                        .Translate(
                            ("RoundIndex", tournamentBehavior.CurrentRoundIndex + 1),
                            ("Teams", string.Join(" " + "{=ixTCiaiv}vs".Translate() + " ", teams)))
                    : "{=9BjHfivM}Betting is now OPEN for final match: {Teams}!"
                        .Translate(
                            ("Teams", string.Join(" " + "{=ixTCiaiv}vs".Translate() + " ", teams)))
                        ;
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
            var tournamentBehavior = Mission.Current?.GetMissionBehavior<TournamentBehavior>();
            if (tournamentBehavior == null)
            {
                return (false, "{=FEZ1Opj1}Tournament is not active".Translate());
            }

            if (!BLTAdoptAHeroModule.TournamentConfig.EnableBetting)
            {
                return (false, "{=uNUyZhDk}Betting is disabled".Translate());
            }
                
            if (CurrentBettingState == BettingState.closed)
            {
                return (false, "{=APTqGlHh}Betting is closed".Translate());
            }
                
            if (tournamentBehavior.CurrentRoundIndex != 3 && BLTAdoptAHeroModule.TournamentConfig.BettingOnFinalOnly)
            {
                return (false, "{=7yFW99H8}Betting is only allowed on the final".Translate());
            }

            if (CurrentBettingState != BettingState.open)
            {
                return (false, "{=ATdvJKFI}Betting is not open".Translate());
            }

            int teamsCount = tournamentBehavior.CurrentMatch.Teams.Count();
            string[] activeTeams = TournamentHelpers.TeamNames.Take(teamsCount).ToArray();
            int teamIdx = activeTeams.IndexOf(team.ToLower());
            if (teamIdx == -1)
            {
                return (false, "{=1rUYWox8}Team name must be one of {Teams}"
                    .Translate(("Teams", string.Join(", ", activeTeams))));
            }
                
            if (activeBets.TryGetValue(hero, out var existingBet))
            {
                if (existingBet.team != teamIdx)
                {
                    return (false, "{=7iveTGQp}You can only bet on one team".Translate());
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
                    Log.LogFeedMessage(
                        "{=Nm96SVXt}Betting is now CLOSED: only one team bet on, bets refunded".Translate());
                    ActionManager.SendChat(
                        "{=Nm96SVXt}Betting is now CLOSED: only one team bet on, bets refunded".Translate());
                }
                else if (!groupedBets.Any())
                {
                    activeBets = null;
                    CurrentBettingState = BettingState.disabled;
                    Log.LogFeedMessage("{=GrI9bFCf}Betting is now CLOSED: no bets placed".Translate());
                    ActionManager.SendChat("{=GrI9bFCf}Betting is now CLOSED: no bets placed".Translate());
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
                    string msg = "{=Hj3oYnE0}Betting is now CLOSED: {TotalBets}"
                        .Translate(("TotalBets", string.Join(", ", betTotals)));
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
                    Log.LogFeedResponse(hero.FirstName.ToString(), 
                        "{=u4mPf1p0}REFUNDED {Bet}{GoldIcon} bet"
                            .Translate(("Bet", bet.bet), ("GoldIcon", Naming.Gold)));
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
                        "{=majAmqDm}WON BET".Translate() +
                        $" {Naming.Inc}{winnings}{Naming.Gold}{Naming.To}{newGold}{Naming.Gold}");
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