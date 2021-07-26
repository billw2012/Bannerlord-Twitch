using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace BLTAdoptAHero
{
    public class TournamentHub : Hub
    {
        private static string GetContentPath(string fileName) => Path.Combine(
            Path.GetDirectoryName(typeof(TournamentHub).Assembly.Location) ?? ".",
            "Overlay", "Tournament", fileName);
        private static string GetContent(string fileName) => File.ReadAllText(GetContentPath(fileName));
        
        public static void Register()
        {
            BLTOverlay.BLTOverlay.Register("tournament", 100, 
                GetContent("Tournament.css"), 
                GetContent("Tournament.html"), 
                GetContent("Tournament.js"));
        }
        
        public override Task OnConnected()
        {
            Refresh();
            return base.OnConnected();
        }
        
        public void Refresh()
        {
            (int entrants, int tournamentSize) = BLTTournamentQueueBehavior.Current?.GetTournamentQueueSize() ?? (0, 0);
            Clients.Caller.updateEntrants(entrants, tournamentSize);
            Clients.Caller.updateBets(BLTTournamentBetMissionBehavior.Current?.GetTotalBets() ?? new List<int>());
            Clients.Caller.UpdateBettingState(BLTTournamentBetMissionBehavior.Current?.CurrentBettingState.ToString() ?? string.Empty);
        }
        
        public static void Reset()
        {
            GlobalHost.ConnectionManager.GetHubContext<TournamentHub>()
                .Clients.All.reset();
        }
        
        public static void UpdateEntrants()
        {
            (int entrants, int tournamentSize) = BLTTournamentQueueBehavior.Current?.GetTournamentQueueSize() ?? (0, 0);
            GlobalHost.ConnectionManager.GetHubContext<TournamentHub>()
                .Clients.All.updateEntrants(entrants, tournamentSize);
        }
        
        public static void UpdateBets()
        {
            GlobalHost.ConnectionManager.GetHubContext<TournamentHub>()
                .Clients.All.updateBets(BLTTournamentBetMissionBehavior.Current?.GetTotalBets() ?? new List<int>());
            GlobalHost.ConnectionManager.GetHubContext<TournamentHub>()
                .Clients.All.updateBettingState(BLTTournamentBetMissionBehavior.Current?.CurrentBettingState.ToString() ?? "none");
        }
    }
}