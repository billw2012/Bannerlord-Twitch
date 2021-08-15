<!-- Tournament -->
$(document).ready(function () {
    const o = Intl.NumberFormat('en', { notation: 'compact' });

    const tournament = new Vue({
        el: '#tournament-container',
        data: {
            entrants: 0,
            tournamentSize: 0,
            bettingState: 'none',
            bets: [],
            labels: {
                Tournament: '',
                BettingIsOpen: '',
                BettingIsClosed: '',
                NotTakingBets: '',
            }
        },
        computed: {
            anyBets: function () {
                return this.bets.some(b => b > 0);
            },
            totalBets: function () {
                return this.bets.reduce((a, b) => a + b, 0);
            },
            betRatios: function () {
                const sum = this.bets.reduce((a, b) => a + b, 0);
                if(sum === 0)
                {
                    const length = this.bets.length;
                    return this.bets.map(function(_) {
                        return { bet: 0, ratio: 100 / length };
                    });
                }
                return this.bets.map(function(b) {
                    return { bet: b, ratio: b * 100 / sum };
                });
            }
        },
        methods:{
            range : function (start, end) {
                if(end <= start)
                {
                    return [];
                }
                return Array(end - start).fill(0).map((_, idx) => start + idx)
            },
            formatBet : function(bet) {
                return o.format(bet);
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
        tournamentHub.client.setLabels = function (labels) {
            tournament.labels = labels;
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