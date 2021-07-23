using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace BLTAdoptAHero
{
    public class TournamentHub : Hub
    {
        public static void Register()
        {
            BLTOverlay.BLTOverlay.Register("tournament", 100, @"
.tournament-container-inner {
    display: flex;
    flex-direction: row;
    margin-top: 1em;
}

#tournament-label {
    font-weight: bold;
    margin-right: 0.6em;
    display: flex;
    align-items: center;
    margin-bottom: 0.1em;
}

#tournament-items {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    align-items: center;
}
.tournament-range {
    display: flex;
    align-items: center;
}

.tournament-entry {
    height: 0.5em;
    width: 0.5em;
    border-radius: 50%;
    display: inline-block;
    box-sizing: border-box;
    margin: 0.1em;
}

.tournament-empty {
    background-color: transparent;
    border: 0.1em solid #ffffff;
}

.tournament-in-next {
    background-color: #ffffff;
}

.tournament-last-slot {
    background-color: #ff813f;
}

.tournament-overflow {
    background-color: #29ba7f;
}

.tournament-entry-t-enter-active {
    animation: bounce-in 0.6s;
}

/*.tournament-entry-t-leave-active {*/
/*    animation: bounce-in 0.1s reverse;*/
/*}*/

@keyframes bounce-in {
    0% {
        transform: scale(1);
        opacity: 0;
    }
    25% {
        transform: scale(4);
        opacity: 0.5;
    }
    100% {
        transform: scale(1);
        opacity: 1;
    }
}


.tournament-bets-items {
    display: flex;
    flex-direction: row;
    margin-top: 0.3em;
    flex-wrap: wrap;
    align-items: center;
}
.tournament-bets-label {
    font-weight: bold;
    margin-right: 0.6em;
    display: flex;
    align-items: center;
    margin-bottom: 0.1em;
}

.tournament-bet {
    margin: 0.2em;
    display: flex;
    border-radius: 0.5em;
    padding: 0.1em 0.5em 0.1em;
    min-width: 3em;
    justify-content: center;
    border: gold solid 0.1em;
}

.tournament-bet-text {
    font-weight: 600;
    font-size: 120%;
}

.tournament-bet-side-0 {
    background: #5e5ef5;
}
.tournament-bet-side-1 {
    background: #a13b3b;
}
.tournament-bet-side-2 {
    background: #2e901f;
}
.tournament-bet-side-3 {
    background: #939300;
}
", @"
<div id='tournament-container' class='drop-shadow-highlight'>
        <div v-if='tournamentSize > 0' class='tournament-container-inner'>
            <div id='tournament-label' class='drop-shadow'>
                Tournament
            </div>
            <div id='tournament-items' class='drop-shadow'>
                <div v-for='index in range(0, Math.max(tournamentSize, entrants))' class='tournament-range'>
                    <transition name='tournament-entry-t' tag='div' mode='out-in' appear>
                        <div v-if='index < entrants && index < tournamentSize - 1'
                             class='tournament-entry tournament-in-next' v-bind:key=""index + 'in-next'""></div>
                        <div v-else-if='index < entrants && index === tournamentSize - 1'
                             class='tournament-entry tournament-last-slot' v-bind:key=""index + 'last-slot'""></div>
                        <div v-else-if='index > tournamentSize - 1'
                             class='tournament-entry tournament-overflow' v-bind:key=""index + 'overflow'""></div>
                        <div v-else
                             class='tournament-entry tournament-empty' v-bind:key=""index + 'empty'""></div>
                    </transition>
                </div>
            </div>
        </div>
        <div v-if=""bettingState === 'open'"" class='tournament-bets-label drop-shadow'>
            Betting is&nbsp;<span style='color: green'>OPEN</span>
        </div>
        <div v-else-if=""bettingState === 'closed'"" class='tournament-bets-label drop-shadow'>
            Betting is&nbsp;<span style='color: red'>CLOSED</span>
        </div>
        <div v-else-if=""bettingState === 'disabled'"" class='tournament-bets-label drop-shadow'>
            <span style='color: gray'>Not taking bets</span>
        </div>
        <div v-if=""bettingState === 'open' || bettingState === 'closed'"" class='drop-shadow tournament-bets-items'>
            <div v-for='(bet, index) in bets' class='tournament-bet'
                 v-bind:class=""'tournament-bet-side-' + index"">
                <div class='tournament-bet-text gold-text-style'>{{bet}}⦷</div>
            </div>
        </div>
    </div>
", @"
<!-- Tournament -->
$(function () {
    const tournament = new Vue({
        el: '#tournament-container',
        data: {
            entrants: 0,
            tournamentSize: 0,
            bettingState: 'none',
            bets: []
        },
        computed: {
            anyBets: function () {
                const nonzero = (b) => b > 0;
                return this.bets.some(nonzero);
            }
        },
        methods:{
            range : function (start, end) {
                if(end <= start)
                {
                    return [];
                }
                return Array(end - start).fill(0).map((_, idx) => start + idx)
            }
        }
    });

    $.connection.hub.url = '$url_root$/signalr';
    $.connection.hub.reconnecting(function () {
        tournament.entrants = 0;
        tournament.tournamentSize = 0;
        tournament.bets = [];
        tournament.bettingState = 'none';
    });
    const tournamentHub = $.connection.tournamentHub;
    tournamentHub.client.updateEntrants = function (entrants, tournamentSize) {
        tournament.entrants = entrants;
        tournament.tournamentSize = tournamentSize;
    };
    tournamentHub.client.reset = function () {
        tournament.entrants = 0;
        tournament.tournamentSize = 0;
        tournament.bets = [];
        tournament.bettingState = 'none';
    };
    tournamentHub.client.updateBets = function (bets) {
        tournament.bets = bets;
    };
    tournamentHub.client.updateBettingState = function (bettingState) {
        tournament.bettingState = bettingState;
    };
    $.connection.hub.start().done(function () {
        console.log('BLT Tournament Hub started');
    });
});
");
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