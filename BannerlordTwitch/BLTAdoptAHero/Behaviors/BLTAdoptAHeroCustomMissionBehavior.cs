﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    internal class BLTAdoptAHeroCustomMissionBehavior : AutoMissionBehavior<BLTAdoptAHeroCustomMissionBehavior>
    {
        public delegate void AgentCreatedDelegate(Agent agent);
        public delegate void MissionOverDelegate();
        public delegate void MissionModeChangeDelegate(MissionMode oldMode, MissionMode newMode, bool atStart);
        public delegate void MissionResetDelegate();
        public delegate void GotAKillDelegate(Agent killer, Agent killed, AgentState agentState);
        public delegate void GotKilledDelegate(Agent killed, Agent killer, AgentState agentState);
        public delegate void MissionTickDelegate(float dt);
            
        private class Listeners
        {
            public AgentCreatedDelegate onAgentCreated;
            public MissionOverDelegate onMissionOver;
            public MissionModeChangeDelegate onModeChange;
            public MissionResetDelegate onMissionReset;
            public GotAKillDelegate onGotAKill;
            public GotKilledDelegate onGotKilled;
            public MissionTickDelegate onMissionTick;
            public MissionTickDelegate onSlowTick;
        }

        private readonly Dictionary<Hero, Listeners> heroListeners = new();
        private readonly Dictionary<Agent, Listeners> agentListeners = new();
        private IEnumerable<Listeners> AllListeners => heroListeners.Values.Concat(agentListeners.Values);

        public bool AddListeners(Hero hero, 
            AgentCreatedDelegate onAgentCreated = null,
            MissionOverDelegate onMissionOver = null,
            MissionModeChangeDelegate onModeChange = null,
            MissionResetDelegate onMissionReset = null,
            GotAKillDelegate onGotAKill = null,
            GotKilledDelegate onGotKilled = null,
            MissionTickDelegate onMissionTick = null,
            MissionTickDelegate onSlowTick = null,
            bool replaceExisting = false
        )
        {
            if (!replaceExisting && HasListeners(hero))
                return false;
            RemoveListeners(hero);
            heroListeners.Add(hero, new Listeners
            {
                onAgentCreated = onAgentCreated, 
                onMissionOver = onMissionOver,
                onModeChange = onModeChange,
                onMissionReset = onMissionReset,
                onGotAKill = onGotAKill,
                onGotKilled = onGotKilled,
                onMissionTick = onMissionTick,
                onSlowTick = onSlowTick,
            });
            return true;
        }
        
        public bool AddListeners(Agent agent, 
            AgentCreatedDelegate onAgentCreated = null,
            MissionOverDelegate onMissionOver = null,
            MissionModeChangeDelegate onModeChange = null,
            MissionResetDelegate onMissionReset = null,
            GotAKillDelegate onGotAKill = null,
            GotKilledDelegate onGotKilled = null,
            MissionTickDelegate onMissionTick = null,
            MissionTickDelegate onSlowTick = null,
            bool replaceExisting = false
        )
        {
            if (!replaceExisting && HasListeners(agent))
                return false;
            RemoveListeners(agent);
            agentListeners.Add(agent, new Listeners
            {
                onAgentCreated = onAgentCreated, 
                onMissionOver = onMissionOver,
                onModeChange = onModeChange,
                onMissionReset = onMissionReset,
                onGotAKill = onGotAKill,
                onGotKilled = onGotKilled,
                onMissionTick = onMissionTick,
                onSlowTick = onSlowTick,
            });
            return true;
        }

        public void RemoveListeners(Hero hero) => heroListeners.Remove(hero);
        public void RemoveListeners(Agent agent) => agentListeners.Remove(agent);

        public bool HasListeners(Hero hero) => heroListeners.ContainsKey(hero);
        public bool HasListeners(Agent agent) => agentListeners.ContainsKey(agent);

        public override void OnAgentCreated(Agent agent)
        {
            ForAgent(agent, l => l.onAgentCreated?.Invoke(agent));
        }

        public override void OnAgentRemoved(Agent killedAgent, Agent killerAgent, AgentState agentState, KillingBlow blow)
        {
            ForAgent(killedAgent, l => l.onGotKilled?.Invoke(killedAgent, killerAgent, agentState));
            if (killerAgent != null)
            {
                ForAgent(killerAgent, l => l.onGotAKill?.Invoke(killerAgent, killedAgent, agentState));
            }
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            // Have to do this, as agent state become undefined after they are deleted 
            agentListeners.Remove(affectedAgent);
        }

        protected override void OnEndMission()
        {
            ForAll(listeners => listeners.onMissionOver?.Invoke());
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
            if (hero == null) return null;
            return heroListeners.ContainsKey(hero) ? hero : null;
        }

        private void ForAll(Action<Listeners> action, [CallerMemberName] string callerName = "")
        {
            foreach (var listener in AllListeners)
            {
                try
                {
                    action(listener);
                }
                catch (Exception e)
                {
                    Log.Exception($"[{nameof(BLTAdoptAHeroCustomMissionBehavior)}] ForAll", e);
                }
            }
        }

        private void ForAgent(Agent agent, Action<Listeners> action, [CallerMemberName] string callerName = "")
        {
            var hero = FindHero(agent);
            if (hero != null && heroListeners.TryGetValue(hero, out var hl))
            {
                try
                {
                    action(hl);
                }
                catch (Exception e)
                {
                    Log.Exception($"[{nameof(BLTAdoptAHeroCustomMissionBehavior)}] ForAgent", e);
                }
            }

            if (agentListeners.TryGetValue(agent, out var al))
            {
                try
                {
                    action(al);
                }
                catch (Exception e)
                {
                    Log.Exception($"[{nameof(BLTAdoptAHeroCustomMissionBehavior)}] ForAgent", e);
                }
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
        
        // public const int MaxLevel = 62;
        public const int MaxLevelInPractice = 32;
        
        // https://www.desmos.com/calculator/frzo6bkrwv
        // value returned is 0 < v < 1 if levelB < levelA, v = 1 if they are equal, and 1 < v < max if levelB > levelA
        public static float RelativeLevelScaling(int levelA, int levelB, float n, float max = float.MaxValue) 
            => Math.Min(MathF.Pow(1f - Math.Min(MaxLevelInPractice - 1, levelB - levelA) / (float)MaxLevelInPractice, -10f * MathF.Clamp(n, 0, 1)), max);
        
        public static List<string> ApplyKillEffects(Hero hero, Agent killer, Agent killed, AgentState state, int goldPerKill, int healPerKill, int xpPerKill, float subBoost, float? relativeLevelScaling, float? levelScalingCap)
        {
            var results = new List<string>();
            
            if (killed != null)
            {
                results.Add($"{KillStateVerb(state)} {killed.Name}");
            }
            
            if (subBoost != 1)
            {
                goldPerKill = (int) (goldPerKill * subBoost);
                healPerKill = (int) (healPerKill * subBoost);
                xpPerKill = (int) (xpPerKill * subBoost);
            }

            float levelBoost = 1;
            if (relativeLevelScaling.HasValue && killed?.Character != null)
            {
                // More reward for killing higher level characters
                levelBoost = RelativeLevelScaling(hero.Level, killed.Character.Level, relativeLevelScaling.Value, levelScalingCap ?? 5);

                if (levelBoost != 1)
                {
                    goldPerKill = (int) (goldPerKill * levelBoost);
                    healPerKill = (int) (healPerKill * levelBoost);
                    xpPerKill = (int) (xpPerKill * levelBoost);
                }
            }

            bool showMultiplier = false;
            if (goldPerKill != 0)
            {
                BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(hero, goldPerKill);
                results.Add($"+{goldPerKill} gold");
                showMultiplier = true;
            }
            if (healPerKill != 0)
            {
                float prevHealth = killer.Health;
                killer.Health = Math.Min(killer.HealthLimit,
                    killer.Health + healPerKill);
                float healthDiff = killer.Health - prevHealth;
                if (healthDiff > 0)
                {
                    results.Add($"+{healthDiff}hp");
                    showMultiplier = true;
                }
            }
            if (xpPerKill != 0)
            {
                (bool success, string description) = SkillXP.ImproveSkill(hero, xpPerKill, Skills.All, auto: true);
                if (success)
                {
                    results.Add(description);
                    showMultiplier = true;
                }
            }

            if (showMultiplier)
            {
                if (subBoost != 1)
                {
                    results.Add($"x{subBoost:0.0} (sub)");
                }

                if (levelBoost != 1 && killed?.Character != null)
                {
                    results.Add($"x{levelBoost:0.0} (lvl diff {killed.Character.Level - hero.Level})");
                }
            }
            return results;
        }
        
        public static List<string> ApplyKilledEffects(Hero hero, Agent killer, AgentState state, int xpPerKilled, float subBoost, float? relativeLevelScaling, float? levelScalingCap)
        {
            if (subBoost != 1)
            {
                xpPerKilled = (int) (xpPerKilled * subBoost);
            }

            float levelBoost = 1;
            if (relativeLevelScaling.HasValue && killer?.Character != null)
            {
                // More reward for being killed by higher level characters
                levelBoost = RelativeLevelScaling(hero.Level, killer.Character.Level, relativeLevelScaling.Value, levelScalingCap ?? 5);

                if (levelBoost != 1)
                {
                    xpPerKilled = (int) (xpPerKilled * levelBoost);
                }
            }

            bool showMultiplier = false;
            
            var results = new List<string>();
            if (killer != null)
            {
                results.Add($"{KillStateVerb(state)} by {killer.Name}");
            }
            if (xpPerKilled != 0)
            {
                (bool success, string description) = SkillXP.ImproveSkill(hero, xpPerKilled, Skills.All, auto: true);
                if (success)
                {
                    results.Add(description);
                    showMultiplier = true;
                }
            }
            
            if (showMultiplier)
            {
                if (subBoost != 1)
                {
                    results.Add($"x{subBoost:0.0} (sub)");
                }

                if (levelBoost != 1 && killer?.Character != null)
                {
                    results.Add($"x{levelBoost:0.0} (lvl diff {killer.Character.Level - hero.Level})");
                }
            }
            
            return results;
        }
    }
}