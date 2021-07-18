using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLTAdoptAHero.Annotations;
using BLTOverlay;
using Microsoft.AspNet.SignalR;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero.UI
{
    public class MissionInfoHub : Hub
    {
        // Hero state that changes quickly
        public class HeroFastTickState
        {
            public float HP;
            
            public float CooldownFractionRemaining;
            public float CooldownSecondsRemaining;
            
            public float ActivePowerFractionRemaining;
        }

        // Full hero state
        public class HeroState
        {
            public string Name;
            
            public HeroFastTickState FastTickState = new();
            
            public bool IsPlayerSide;
            
            public bool IsRouted;
            public bool IsUnconscious;
            public bool IsKilled;
            
            public float MaxHP;
            
            public int Kills;
            
            public int Retinue;
            public int RetinueKills;
            
            public int GoldEarned;
            public int XPEarned;
        }

        private static readonly List<HeroState> heroState = new();

        // private void Refresh()
        // {
        //     lock (heroState)
        //     {
        //         foreach (var hero in heroState)
        //         {
        //             Clients.All.updateHero(hero);
        //         }
        //     }
        // }

        public override Task OnConnected()
        {
            TickSlow();
            return base.OnConnected();
        }

        public static void TickSlow()
        {
            lock (heroState)
            {
                GlobalHost.ConnectionManager.GetHubContext<MissionInfoHub>()
                    .Clients.All.tickSlow(heroState);
            }
        }

        public static void TickFast()
        {
            lock (heroState)
            {
                GlobalHost.ConnectionManager.GetHubContext<MissionInfoHub>()
                    .Clients.All.tickFast(heroState);
            }
        }
        
        // public static void RemoveHero(string name)
        // {
        //     lock (heroState)
        //     {
        //         heroState.RemoveAll(h => h.Name == name);
        //     }
        // }
                
        public static void Clear()
        {
            lock (heroState)
            {
                heroState.Clear();
            }

            TickSlow();
        }
            
        public static void UpdateHero(HeroState state)
        {
            lock (heroState)
            {
                heroState.RemoveAll(h => h.Name == state.Name);
                heroState.Add(state);
            }
        }

        public static void UpdateHeroFastTickState(string name, HeroFastTickState TickState)
        {
            lock (heroState)
            {
                var state = heroState.FirstOrDefault(h => h.Name == name);
                if (state != null)
                {
                    state.FastTickState = TickState;
                }
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
.mission-heroes-t-move {
    transition: transform 0.5s;
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
.mission-hero-player-side {
    background: #202050;
}
.mission-hero-other-side {
    background: #401122;
}
.mission-hero-health {
    position: absolute;
    z-index: 0;
    height: 100%;
    margin: 0;
}
.mission-hero-player-side .mission-hero-health {
    background: #6666CC;
}
.mission-hero-other-side .mission-hero-health {
    background: #AA3277;
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
                        v-bind:class=""hero.IsPlayerSide ? 'mission-hero-player-side' : 'mission-hero-other-side'"">
                <div class='mission-hero-health' 
                     v-bind:style=""{ width: (hero.FastTickState.HP * 100 / hero.MaxHP) + '%' }""></div>
                <div v-show='hero.FastTickState.ActivePowerFractionRemaining > 0' 
                     class='mission-hero-active-power-remaining' 
                     v-bind:style=""{ height: hero.FastTickState.ActivePowerFractionRemaining * 100 + '%' }""></div>
                <div class='mission-hero-name-row'>
                    <div class='mission-hero-summon-cooldown outline'>
                        <progress-ring :radius='10' 
                                       color='yellow'
                                       :progress='hero.FastTickState.CooldownFractionRemaining * 100' 
                                       :stroke='10'></progress-ring>
                    </div>
                    <div class='mission-hero-name drop-shadow-2'>{{hero.Name}}</div>
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
    missionInfoHub.client.tickFast = function (heroes) {
        mission.heroes = heroes;
        console.log('BLT Mission Info heroes set to ' + heroes);
    };
    missionInfoHub.client.tickSlow = function (heroes) {
        mission.heroes = heroes;
        console.log('BLT Mission Info heroes set to ' + heroes);
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