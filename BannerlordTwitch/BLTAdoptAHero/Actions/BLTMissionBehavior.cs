using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    internal class BLTMissionBehavior : AutoMissionBehavior<BLTMissionBehavior>
    {
        public delegate void MissionOverDelegate();
        public delegate void MissionModeChangeDelegate(MissionMode oldMode, MissionMode newMode, bool atStart);
        public delegate void MissionResetDelegate();
        public delegate void GotAKillDelegate(Agent killer, Agent killed, AgentState agentState);
        public delegate void GotKilledDelegate(Agent killed, Agent killer, AgentState agentState);
        public delegate void MissionTickDelegate(float dt);
            
        public class Listeners
        {
            public Hero hero;
            public MissionOverDelegate onMissionOver;
            public MissionModeChangeDelegate onModeChange;
            public MissionResetDelegate onMissionReset;
            public GotAKillDelegate onGotAKill;
            public GotKilledDelegate onGotKilled;
            public MissionTickDelegate onMissionTick;
            public MissionTickDelegate onSlowTick;
        }

        private readonly List<Listeners> listeners = new();

        public void AddListeners(Hero hero, 
            MissionOverDelegate onMissionOver = null,
            MissionModeChangeDelegate onModeChange = null,
            MissionResetDelegate onMissionReset = null,
            GotAKillDelegate onGotAKill = null,
            GotKilledDelegate onGotKilled = null,
            MissionTickDelegate onMissionTick = null,
            MissionTickDelegate onSlowTick = null
        )
        {
            RemoveListeners(hero);
            listeners.Add(new Listeners
            {
                hero = hero,
                onMissionOver = onMissionOver,
                onModeChange = onModeChange,
                onMissionReset = onMissionReset,
                onGotAKill = onGotAKill,
                onGotKilled = onGotKilled,
                onMissionTick = onMissionTick,
                onSlowTick = onSlowTick,
            });
        }

        public void RemoveListeners(Hero hero)
        {
            listeners.RemoveAll(l => l.hero == hero);
        }

        public override void OnAgentRemoved(Agent killedAgent, Agent killerAgent, AgentState agentState, KillingBlow blow)
        {
            ForAgent(killedAgent, l => l.onGotKilled?.Invoke(killedAgent, killerAgent, agentState));
            if (killerAgent != null)
            {
                ForAgent(killerAgent, l => l.onGotAKill?.Invoke(killerAgent, killedAgent, agentState));
            }

            base.OnAgentRemoved(killedAgent, killerAgent, agentState, blow);
        }

        protected override void OnEndMission()
        {
            ForAll(listeners => listeners.onMissionOver?.Invoke());
            base.OnEndMission();
        }

        private const float SlowTickDuration = 2;
        private float slowTick = 0;
            
        public override void OnMissionTick(float dt)
        {
            slowTick += dt;
            if (slowTick > 2)
            {
                slowTick -= 2;
                ForAll(listeners => listeners.onSlowTick?.Invoke(2));
            }
            ForAll(listeners => listeners.onMissionTick?.Invoke(dt));
            base.OnMissionTick(dt);
        }


        // public override void OnMissionActivate()
        // {
        //     base.OnMissionActivate();
        // }
        //
        // public override void OnMissionDeactivate()
        // {
        //     base.OnMissionDeactivate();
        // }
        //
        // public override void OnMissionRestart()
        // {
        //     base.OnMissionRestart();
        // }

        public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            ForAll(l => l.onModeChange?.Invoke(oldMissionMode, Mission.Current.Mode, atStart));
            base.OnMissionModeChange(oldMissionMode, atStart);
        }

        public static Hero GetHeroFromAgent(Agent agent) => (agent?.Character as CharacterObject)?.HeroObject;
        
        private Hero FindHero(Agent agent)
        {
            var hero = GetHeroFromAgent(agent);
            return hero == null ? null : listeners.FirstOrDefault(l => l.hero == hero)?.hero;
        }

        private void ForAll(Action<Listeners> action)
        {
            foreach (var listener in listeners)
            {
                action(listener);
            }
        }

        private void ForAgent(Agent agent, Action<Listeners> action)
        {
            var hero = FindHero(agent);
            if (hero == null) return;
            foreach (var listener in listeners.Where(l => l.hero == hero))
            {
                action(listener);
            }
        }

        public static string KillStateVerb(AgentState state) =>
            state switch
            {
                AgentState.Routed => "routed",
                AgentState.Unconscious => "knocked out",
                AgentState.Killed => "killed",
                AgentState.Deleted => "deleted",
                _ => "fondled"
            };
    }
}