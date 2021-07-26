<!-- Mission Info -->
$(function () {
    const mission = new Vue({
        el: '#mission-container',
        components: {
            'progress-ring': ProgressRing
        },
        data: {
            heroes: []
        },
        computed: {
            sortedHeroes: function () {
                function compare(a, b) {
                    if(!a.IsPlayerSide && b.IsPlayerSide) return 1;
                    if(a.IsPlayerSide && !b.IsPlayerSide) return -1;
                    if(a.TournamentTeam < b.TournamentTeam) return -1;
                    if(a.TournamentTeam > !b.TournamentTeam) return 1;
                    if(a.Kills < b.Kills) return 1;
                    if(a.Kills > b.Kills) return -1;
                    if(a.RetinueKills < b.RetinueKills) return 1;
                    if(a.RetinueKills > b.RetinueKills) return -1;
                    if (a.Name < b.Name) return 1;
                    if (a.Name > b.Name) return -1;
                    return 0;
                }
                return this.heroes.sort(compare);
            }
        }
    });

    $.connection.hub.url = '$url_root$/signalr';
    $.connection.hub.reconnecting(function () {
        mission.heroes = [];
    });

    const missionInfoHub = $.connection.missionInfoHub;
    missionInfoHub.client.update = function (heroes) {
        mission.heroes = heroes;
    };

    $.connection.hub.start().done(function () {
        console.log('BLT Mission Info Hub started');
    }).fail(function () {
        console.log('BLT Mission Info Hub failed to start!');
    });
});