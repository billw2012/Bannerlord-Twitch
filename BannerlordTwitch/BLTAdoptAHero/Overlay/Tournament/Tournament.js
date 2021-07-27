<!-- Tournament -->
$(document).ready(function () {
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

    if(typeof $.connection.tournamentHub !== 'undefined') {
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
    } else {
        tournament.entrants = 20;
        tournament.tournamentSize = 16;
        tournament.bettingState = 'open';
        tournament.bets = [12312, 0, 400, 1];
    }
});