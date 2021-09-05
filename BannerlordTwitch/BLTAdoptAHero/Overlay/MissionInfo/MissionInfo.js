<!-- Mission Info -->
$(document).ready(function () {
    const o = Intl.NumberFormat('en', { notation: 'compact' });
    const mission = new Vue({
        el: '#mission-container',
        components: {
            'progress-ring': ProgressRing
        },
        data: {
            heroes: [],
            keyLabels: {
                Kills: '',
                RetinueKills: '', 
                Gold: '', 
                XP: '',
            }
        },
        computed: {
            sortedHeroes: function () {
                function compare(a, b) {
                    if(a.TournamentTeam < b.TournamentTeam) return -1;
                    if(a.TournamentTeam > !b.TournamentTeam) return 1;
                    if(!a.IsPlayerSide && b.IsPlayerSide) return 1;
                    if(a.IsPlayerSide && !b.IsPlayerSide) return -1;
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
        },
        methods: {
            getUserColor: function(userName) {
                return twitch.getUserColor(userName);
            },
            formatNumber: function(number) {
                return o.format(number);
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
        missionInfoHub.client.setKeyLabels = function (keyLabels) {
            mission.keyLabels = keyLabels;
        };

        $.connection.hub.start().done(function () {
            console.log('BLT Mission Info Hub started');
        }).fail(function () {
            console.log('BLT Mission Info Hub failed to start!');
        });
    } else {
        mission.heroes = [];
        const states = ["active", "routed", "unconscious", "killed"];
        for (let i = 0; i < 20; i++) {
            const cooldownFractionRemaining = Math.random();
            mission.heroes.push({
                Name: "Viewer Name " + i,
                HP: Math.random() * 100,
                MaxHP: 100,
                CooldownFractionRemaining: cooldownFractionRemaining,
                CooldownSecondsRemaining: cooldownFractionRemaining * 60,
                ActivePowerFractionRemaining: Math.random(),
                IsPlayerSide: Math.random() > 0.5,
                TournamentTeam: -1,
                State: states[Math.floor(Math.random()*states.length)],
                Kills: Math.floor(Math.random() * 20),
                Retinue: Math.floor(Math.random() * 10),
                DeadRetinue: Math.floor(Math.random() * 10),
                RetinueKills: Math.floor(Math.random() * 100),
                GoldEarned: Math.floor(Math.random() * 1000000),
                XPEarned: Math.floor(Math.random() * 1000000),
            });
        }
        // mission.heroes = [
        //     {
        //         Name: "Viewer1",
        //         HP: 75,
        //         CooldownFractionRemaining: 0.35,
        //         CooldownSecondsRemaining: 12,
        //         ActivePowerFractionRemaining: 0.3,
        //         IsPlayerSide: true,
        //         TournamentTeam: -1,
        //         State: "active",
        //         MaxHP: 112,
        //         Kills: 3,
        //         Retinue: 50,
        //         DeadRetinue: 50,
        //         RetinueKills: 12,
        //         GoldEarned: 12318,
        //         XPEarned: 54765,
        //     },
        //     {
        //         Name: "ViewerWithALongName1",
        //         HP: 0,
        //         CooldownFractionRemaining: 0.35,
        //         CooldownSecondsRemaining: 12,
        //         ActivePowerFractionRemaining: 0.3,
        //         IsPlayerSide: false,
        //         TournamentTeam: -1,
        //         State: "routed",
        //         MaxHP: 112,
        //         Kills: 32,
        //         Retinue: 1,
        //         RetinueKills: 122,
        //         GoldEarned: 1221318,
        //         XPEarned: 1254765,
        //     },
        //     {
        //         Name: "Viewer2",
        //         HP: 0,
        //         CooldownFractionRemaining: 0.35,
        //         CooldownSecondsRemaining: 12,
        //         ActivePowerFractionRemaining: 0.3,
        //         IsPlayerSide: false,
        //         TournamentTeam: -1,
        //         State: "unconscious",
        //         MaxHP: 112,
        //         Kills: 0,
        //         Retinue: 0,
        //         RetinueKills: 0,
        //         GoldEarned: 0,
        //         XPEarned: 0,
        //     },
        //     {
        //         Name: "Viewer3",
        //         HP: 0,
        //         CooldownFractionRemaining: 0.35,
        //         CooldownSecondsRemaining: 12,
        //         ActivePowerFractionRemaining: 0.3,
        //         IsPlayerSide: true,
        //         TournamentTeam: -1,
        //         State: "killed",
        //         MaxHP: 112,
        //         Kills: 0,
        //         Retinue: 0,
        //         RetinueKills: 0,
        //         GoldEarned: 0,
        //         XPEarned: 0,
        //     },
        //     {
        //         Name: "Viewer4",
        //         HP: 0,
        //         CooldownFractionRemaining: 0.35,
        //         CooldownSecondsRemaining: 12,
        //         ActivePowerFractionRemaining: 0.3,
        //         IsPlayerSide: false,
        //         TournamentTeam: -1,
        //         State: "unconscious",
        //         MaxHP: 112,
        //         Kills: 0,
        //         Retinue: 0,
        //         RetinueKills: 0,
        //         GoldEarned: 0,
        //         XPEarned: 0,
        //     },
        //     {
        //         Name: "Viewer5",
        //         HP: 0,
        //         CooldownFractionRemaining: 0.35,
        //         CooldownSecondsRemaining: 12,
        //         ActivePowerFractionRemaining: 0.3,
        //         IsPlayerSide: false,
        //         TournamentTeam: -1,
        //         State: "unconscious",
        //         MaxHP: 112,
        //         Kills: 0,
        //         Retinue: 0,
        //         RetinueKills: 0,
        //         GoldEarned: 0,
        //         XPEarned: 0,
        //     },
        //     {
        //         Name: "Viewer6",
        //         HP: 0,
        //         CooldownFractionRemaining: 0.35,
        //         CooldownSecondsRemaining: 12,
        //         ActivePowerFractionRemaining: 0.3,
        //         IsPlayerSide: false,
        //         TournamentTeam: -1,
        //         State: "unconscious",
        //         MaxHP: 112,
        //         Kills: 0,
        //         Retinue: 0,
        //         RetinueKills: 0,
        //         GoldEarned: 0,
        //         XPEarned: 0,
        //     },
        //     {
        //         Name: "Viewer7",
        //         HP: 0,
        //         CooldownFractionRemaining: 0.35,
        //         CooldownSecondsRemaining: 12,
        //         ActivePowerFractionRemaining: 0.3,
        //         IsPlayerSide: false,
        //         TournamentTeam: -1,
        //         State: "unconscious",
        //         MaxHP: 112,
        //         Kills: 0,
        //         Retinue: 0,
        //         RetinueKills: 0,
        //         GoldEarned: 0,
        //         XPEarned: 0,
        //     },
        //     {
        //         Name: "Viewer8",
        //         HP: 0,
        //         CooldownFractionRemaining: 0.35,
        //         CooldownSecondsRemaining: 12,
        //         ActivePowerFractionRemaining: 0.3,
        //         IsPlayerSide: false,
        //         TournamentTeam: -1,
        //         State: "unconscious",
        //         MaxHP: 112,
        //         Kills: 0,
        //         Retinue: 0,
        //         RetinueKills: 0,
        //         GoldEarned: 0,
        //         XPEarned: 0,
        //     }
        // ];
    }
});