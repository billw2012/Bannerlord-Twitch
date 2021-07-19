using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BLTAdoptAHero.Annotations;
using Microsoft.AspNet.SignalR;

namespace BLTAdoptAHero.UI
{
    [UsedImplicitly]
    public class MissionInfoHub : Hub
    {
        [UsedImplicitly]
        public class HeroState
        {
            public string Name;
            
            [UsedImplicitly] public float HP;
            
            [UsedImplicitly] public float CooldownFractionRemaining;
            [UsedImplicitly] public float CooldownSecondsRemaining;
            
            [UsedImplicitly] public float ActivePowerFractionRemaining;
            
            [UsedImplicitly] public bool IsPlayerSide;
            
            [UsedImplicitly] public int TournamentTeam;
            
            [UsedImplicitly] public string State;
            
            [UsedImplicitly] public float MaxHP;
            
            [UsedImplicitly] public int Kills;
            
            [UsedImplicitly] public int Retinue;
            [UsedImplicitly] public int RetinueKills;
            
            [UsedImplicitly] public int GoldEarned;
            [UsedImplicitly] public int XPEarned;
        }

        private static readonly List<HeroState> heroState = new();

        public override Task OnConnected()
        {
            Update();
            return base.OnConnected();
        }

        public static void Update()
        {
            lock (heroState)
            {
                GlobalHost.ConnectionManager.GetHubContext<MissionInfoHub>()
                    .Clients.All.update(heroState);
            }
        }
        
        public static void Remove(string name)
        {
            lock (heroState)
            {
                heroState.RemoveAll(h 
                    => string.Equals(h.Name, name, StringComparison.CurrentCultureIgnoreCase));
            }
        }
                
        public static void Clear()
        {
            lock (heroState)
            {
                heroState.Clear();
            }

            // Update immediately as Clear probably means the mission is over
            Update();
        }
            
        public static void UpdateHero(HeroState state)
        {
            lock (heroState)
            {
                heroState.RemoveAll(h 
                    => string.Equals(h.Name, state.Name, StringComparison.CurrentCultureIgnoreCase));
                heroState.Add(state);
            }
        }
        public static void Register()
        {
            BLTOverlay.BLTOverlay.Register("mission", 200, @"
#mission-container {
    display: flex;
    flex-direction: row;
    margin-top: 1em;
}
#mission-heroes {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    align-items: stretch;
}
.mission-hero {
    min-width: 6em;
    max-width: 10em;
    margin: 0.3em;
    flex-grow: 1;
}
.mission-hero-inner {
    display: flex;
    flex-direction: column;
    position: relative;
    border-radius: 0.3em;
    overflow: hidden;
    height: 100%;
}
.mission-hero div {
    z-index: 1;
}

.mission-hero-health {
    position: absolute;
    z-index: 0;
    height: 100%;
    margin: 0;
}

.mission-hero-player-side {
    background: #202050;
}
.mission-hero-other-side {
    background: #401122;
}
.mission-hero-player-side .mission-hero-health {
    background: #6666CC;
}
.mission-hero-other-side .mission-hero-health {
    background: #AA3277;
}

.mission-hero-tournament-side-0 {
    background: #181858;
}
.mission-hero-tournament-side-0 .mission-hero-health {
    background: #5e5ef5;
}
.mission-hero-tournament-side-1 {
    background: #3e1010;
}
.mission-hero-tournament-side-1 .mission-hero-health {
    background: #a13b3b;
}
.mission-hero-tournament-side-2 {
    background: #10390c;
}
.mission-hero-tournament-side-2 .mission-hero-health {
    background: #2e901f;
}
.mission-hero-tournament-side-3 {
    background: #454512;
}
.mission-hero-tournament-side-3 .mission-hero-health {
    background: #939300;
}

.mission-hero-active-power-remaining {
    position: absolute;
    bottom: 0;
    right: 0;
    width: 0.5em;
    background: orange;
    clip: auto;
}

.mission-hero-name-row {
    display: flex;
    flex-direction: row;
    margin: 0 0.2em 0.3em 0.2em;
}

.mission-hero-summon-cooldown {
    width: 1.25em;
    height: 1.25em;
    flex-shrink: 0;
    flex-basis: 1.25em;
    margin: -0.1em -0.3em 0 0.1em;
    align-self: center;
}
.mission-hero-name {
    text-align: center;
    text-overflow: ellipsis;
    overflow: hidden;
    white-space: nowrap;
    margin-left: 0.2em;
    margin-right: 0.2em;
    align-self: baseline;
    color: white;
}

.mission-hero-state-routed {
    color: #ffc100;
}

.mission-hero-state-unconscious {
    color: #ee8300;
}

.mission-hero-state-killed {
    color: #ff0000;
}

.mission-hero-score-row {
    display: flex;
    flex-direction: row;
    margin-top: -0.1em;
    margin-left: 0.3em;
    margin-right: 0.3em;
    margin-bottom: 0;
}

.mission-hero-kills {
    font-size: 115%;
    margin-top: -0.1em;
    transition: 0.3s;
}

.mission-hero-kills-t-active {
    opacity: 0;
    transform: scale(3);
}

.mission-hero-retinue-kills {
    align-self: stretch;
    color: cyan;
}

.mission-hero-gold-xp {
    display: flex;
    flex-direction: row;
    margin-left: auto;
}

.mission-hero-gold {
    color: gold;
    margin-left: 0.5em;
    margin-right: 0.3em;
}

.mission-hero-xp {
    color: #FF7FB0;
}

.hero-retinue-list {
    display: flex;
    flex-direction: row;
    max-height: 0;
    position: relative;
    justify-content: center;
}

.hero-retinue-list-item {
    height: 0.4em;
    width: 0.4em;
    border-radius: 50%;
    display: inline-block;
    box-sizing: border-box;
    background: cyan;
    margin: -0.15em 0.2em 0.2em 0.2em;
}
", @"
<div id='mission-container' class='drop-shadow'>
    <transition-group name='mission-heroes-t' tag='div' id='mission-heroes'>
        <div class='mission-hero' v-for='hero in sortedHeroes'
             v-bind:key='hero.Name'>
            <div class='mission-hero-inner' 
                v-bind:class=""[hero.IsPlayerSide ? 'mission-hero-player-side' : 'mission-hero-other-side', 'mission-hero-tournament-side-' + hero.TournamentTeam]"">
                <div class='mission-hero-health' 
                     v-bind:style=""{ width: (hero.HP * 100 / hero.MaxHP) + '%' }""></div>
                <div v-show='hero.ActivePowerFractionRemaining > 0' 
                     class='mission-hero-active-power-remaining' 
                     v-bind:style=""{ height: hero.ActivePowerFractionRemaining * 100 + '%' }""></div>
                <div class='mission-hero-name-row'>
                    <div class='mission-hero-summon-cooldown outline'>
                        <progress-ring :radius='10' 
                                       color='yellow'
                                       :progress='hero.CooldownFractionRemaining * 100' 
                                       :stroke='10'></progress-ring>
                    </div>
                    <div class='mission-hero-name drop-shadow-2'
                         v-bind:class=""'mission-hero-state-' + hero.State"">{{hero.Name}}</div>
                </div>
                <div class='mission-hero-score-row drop-shadow-2'>
                    <div v-show='hero.Kills > 0' class='mission-hero-kills'>
                        <transition name='mission-hero-kills-t'>
                            <div :key='hero.Kills'>
                                {{hero.Kills}}
                            </div>
                        </transition>
                    </div>
                    <div v-show='hero.RetinueKills > 0' class='mission-hero-retinue-kills'>
                        +{{hero.RetinueKills}}</div>
                    <div class='mission-hero-gold-xp'>
                        <div v-show='hero.GoldEarned > 0' class='mission-hero-gold'>
                            {{Math.round(hero.GoldEarned / 1000)}}k</div>
                        <div v-show='hero.XPEarned > 0' class='mission-hero-xp'>
                            {{Math.round(hero.XPEarned / 1000)}}k</div>
                    </div>
                </div>
            </div>
            <div class='hero-retinue-list drop-shadow-2'>
                <div v-for='index in Math.min(hero.Retinue, 5)' class='hero-retinue-list-item'></div>
            </div>
        </div>
    </transition-group>
</div>
", @"
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
    $.connection.hub.logging = true;

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
");
        }
    }
}