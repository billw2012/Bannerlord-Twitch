using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BannerlordTwitch.Util;
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
            [UsedImplicitly] public int DeadRetinue;
            [UsedImplicitly] public int RetinueKills;
            
            [UsedImplicitly] public int GoldEarned;
            [UsedImplicitly] public int XPEarned;
        }

        private static readonly List<HeroState> heroState = new();

        public override Task OnConnected()
        {
            Clients.Caller.setKeyLabels(new
            {
                Kills = "{=AM2zlkem}Kills".Translate(),
                RetinueKills = "{=79JXI4JL}+Retinue Kills".Translate(),
                Gold = "{=o0Q8Y1Qg}Gold".Translate(),
                XP = "{=VtEJiMWy}XP".Translate(),
            });
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
        
        private static string GetContentPath(string fileName) => Path.Combine(
            Path.GetDirectoryName(typeof(MissionInfoHub).Assembly.Location) ?? ".",
            "Overlay", "MissionInfo", fileName);
        private static string GetContent(string fileName) => File.ReadAllText(GetContentPath(fileName));
        
        public static void Register()
        {
            BLTOverlay.BLTOverlay.Register("mission", 200, 
                GetContent("MissionInfo.css"), 
                GetContent("MissionInfo.html"), 
                GetContent("MissionInfo.js"));
        }
    }
}