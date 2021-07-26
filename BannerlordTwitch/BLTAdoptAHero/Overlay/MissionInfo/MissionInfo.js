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

    if(typeof $.connection.missionInfoHub !== 'undefined') {
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
    } else {
        mission.heroes = [
            {
                Name: "Viewer1",
                HP: 75,
                CooldownFractionRemaining: 0.35,
                CooldownSecondsRemaining: 12,
                ActivePowerFractionRemaining: 0.3,
                IsPlayerSide: true,
                TournamentTeam: -1,
                State: "active",
                MaxHP: 112,
                Kills: 3,
                Retinue: 50,
                RetinueKills: 12,
                GoldEarned: 12318,
                XPEarned: 54765,
            },
            {
                Name: "ViewerWithALongName1",
                HP: 0,
                CooldownFractionRemaining: 0.35,
                CooldownSecondsRemaining: 12,
                ActivePowerFractionRemaining: 0.3,
                IsPlayerSide: false,
                TournamentTeam: -1,
                State: "routed",
                MaxHP: 112,
                Kills: 32,
                Retinue: 1,
                RetinueKills: 122,
                GoldEarned: 1221318,
                XPEarned: 1254765,
            },
            {
                Name: "Viewer2",
                HP: 0,
                CooldownFractionRemaining: 0.35,
                CooldownSecondsRemaining: 12,
                ActivePowerFractionRemaining: 0.3,
                IsPlayerSide: false,
                TournamentTeam: -1,
                State: "unconscious",
                MaxHP: 112,
                Kills: 0,
                Retinue: 0,
                RetinueKills: 0,
                GoldEarned: 0,
                XPEarned: 0,
            },
            {
                Name: "Viewer3",
                HP: 0,
                CooldownFractionRemaining: 0.35,
                CooldownSecondsRemaining: 12,
                ActivePowerFractionRemaining: 0.3,
                IsPlayerSide: true,
                TournamentTeam: -1,
                State: "killed",
                MaxHP: 112,
                Kills: 0,
                Retinue: 0,
                RetinueKills: 0,
                GoldEarned: 0,
                XPEarned: 0,
            }
        ];
    }
});