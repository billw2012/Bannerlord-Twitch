using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Behaviors;
using HarmonyLib;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    [HarmonyPatch]
    internal class BLTAdoptAHeroCommonMissionBehavior : AutoMissionBehavior<BLTAdoptAHeroCommonMissionBehavior>
    {
        private MissionInfoPanel missionInfoPanel; 
        
        private ObservableCollection<HeroViewModel> heroesViewModel { get; set; } = new();
        private List<Hero> activeHeroes = new();

        private class HeroMissionState
        {
            public int WonGold { get; set; }
            public int WonXP { get; set; }
            public int Kills { get; set; }
            public int RetinueKills { get; set; }
            public int KillStreak { get; set; }
        }

        private Dictionary<Hero, HeroMissionState> heroMissionState = new();
        private float slowTickT = 0;

        public BLTAdoptAHeroCommonMissionBehavior()
        {
            Log.AddInfoPanel(() =>
            {
                missionInfoPanel = new MissionInfoPanel {HeroList = {ItemsSource = heroesViewModel}};
                return missionInfoPanel;
            });
        }
        
        // public override void OnAgentBuild(Agent agent, Banner banner)
        // {
        //     // if (agent == Agent.Main)
        //     // {
        //     //     foreach (var h in activeHeroes)
        //     //     {
        //     //         UpdateHeroVM(h);
        //     //     }
        //     //     // foreach (var a in Mission.AllAgents)
        //     //     // {
        //     //     //     UpdateHeroVM(a);
        //     //     // }
        //     // }
        //     // else
        //     // {
        //     //     UpdateHeroVM(agent);
        //     // }
        // }

        public override void OnMissionTick(float dt)
        {
            slowTickT += dt;
            if (slowTickT > 1f)
            {
                slowTickT -= 1f;

                var sw = new Stopwatch();
                sw.Start();
                foreach (var h in activeHeroes)
                {
                    UpdateHeroVM(h);
                }
                sw.Stop();
            }
        }

        protected override void OnEndMission()
        {
            Log.RemoveInfoPanel(missionInfoPanel);
        }
        
        public override void OnAgentCreated(Agent agent)
        {
            var hero = GetAdoptedHeroFromAgent(agent);
            if (hero == null)
            {
                return;
            }
            BLTAdoptAHeroCampaignBehavior.SetAgentStartingHealth(agent);
            activeHeroes.Add(hero);
            //UpdateHeroVM(agent);
        }

        // public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, int damage, in MissionWeapon affectorWeapon)
        // {
        //     UpdateHeroVM(affectedAgent);
        // }

        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(Mission), "OnAgentRemoved")]
        public static void OnAgentRemovedPrefix(Mission __instance, Agent affectedAgent, Agent affectorAgent,
            ref AgentState agentState, KillingBlow killingBlow)
        {
            var affectedHero = GetAdoptedHeroFromAgent(affectedAgent);
            if (affectedHero != null && BLTAdoptAHeroModule.CommonConfig.AllowDeath == false && affectedAgent.State == AgentState.Killed)
            {
                agentState = affectedAgent.State = AgentState.Unconscious;
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            var affectedHero = GetAdoptedHeroFromAgent(affectedAgent);
            if (affectedHero != null)
            {
                Log.Trace($"[{nameof(BLTAdoptAHeroCommonMissionBehavior)}] {affectedHero} was killed by {affectorAgent?.ToString() ?? "unknown"}");
                ApplyKilledEffects(
                    affectedHero, affectorAgent, agentState,
                    BLTAdoptAHeroModule.CommonConfig.XPPerKilled,
                    Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1),
                    BLTAdoptAHeroModule.CommonConfig.RelativeLevelScaling,
                    BLTAdoptAHeroModule.CommonConfig.LevelScalingCap
                );
                //UpdateHeroVM(affectedAgent);
            }
            var affectorHero = GetAdoptedHeroFromAgent(affectorAgent);
            if (affectorHero != null)
            {
                float horseFactor = affectedAgent?.IsHuman == false ? 0.25f : 1;
                Log.Trace($"[{nameof(BLTAdoptAHeroCommonMissionBehavior)}] {affectorHero} killed {affectedAgent?.ToString() ?? "unknown"}");
                ApplyKillEffects(
                    affectorHero, affectorAgent, affectedAgent, agentState,
                    (int) (BLTAdoptAHeroModule.CommonConfig.GoldPerKill * horseFactor),
                    (int) (BLTAdoptAHeroModule.CommonConfig.HealPerKill * horseFactor),
                    (int) (BLTAdoptAHeroModule.CommonConfig.XPPerKill * horseFactor),
                    Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1),
                    BLTAdoptAHeroModule.CommonConfig.RelativeLevelScaling,
                    BLTAdoptAHeroModule.CommonConfig.LevelScalingCap
                );

                //UpdateHeroVM(affectorAgent);
                if (affectedAgent?.IsHuman == true && agentState is AgentState.Unconscious or AgentState.Killed)
                {
                    GetHeroMissionState(affectorHero).Kills++;
                }                
            }

            var affectorRetinueOwner = BLTSummonBehavior.Current.GetSummonedHeroForRetinue(affectedAgent);  
            if (affectorRetinueOwner != null)
            {
                GetHeroMissionState(affectorRetinueOwner.Hero).RetinueKills++;
            }
        }

        // public override void OnAgentFleeing(Agent affectedAgent)
        // {
        //     
        // }
        //
        // public override void OnAgentPanicked(Agent affectedAgent)
        // {
        //     
        // }
        
        private static Hero GetAdoptedHeroFromAgent(Agent agent)
        {
            var hero = (agent?.Character as CharacterObject)?.HeroObject;
            return hero?.IsAdopted() == true ? hero : null;
        }

        private HeroMissionState GetHeroMissionState(Hero hero)
        {
            if (!heroMissionState.TryGetValue(hero, out var state))
            {
                state = new HeroMissionState();
                heroMissionState.Add(hero, state);
            }

            return state;
        }

        // private void UpdateHeroVM(Agent agent)
        // {
        //     var hero = GetAdoptedHeroFromAgent(agent);
        //     if (hero != null)
        //     {
        //         UpdateHeroVM(hero, agent);
        //     }
        // }

        private void UpdateHeroVM(Hero hero)
        {
            var heroState = GetHeroMissionState(hero);

            if (!activeHeroes.Contains(hero))
            {
                activeHeroes.Add(hero);
            }

            var summonState = BLTSummonBehavior.Current.GetSummonedHero(hero);

            var agent = summonState?.CurrentAgent ??
                        Mission.Current.Agents.FirstOrDefault(a => a.Character == hero.CharacterObject);

            var state = summonState?.State ?? agent?.State ?? AgentState.None;
            var heroModel = new HeroViewModel
            {
                Name = hero.FirstName.Raw(),
                IsPlayerSide = summonState?.CurrentAgent?.Team == Mission.Current?.PlayerTeam || summonState?.CurrentAgent?.Team == Mission.Current?.PlayerAllyTeam,
                MaxHP = agent?.HealthLimit ?? 100,
                HP = agent?.Health ?? 0,
                IsRouted = state is AgentState.Routed,
                IsUnconscious = state is AgentState.Unconscious,
                IsKilled = state is AgentState.Killed,
                Retinue = summonState?.ActiveRetinue ?? 0,
                GoldEarned = heroState.WonGold,
                XPEarned = heroState.WonXP,
                CooldownFractionRemaining = 1 - summonState?.CoolDownFraction ?? 0,
                Kills = heroState.Kills,
                RetinueKills = heroState.RetinueKills,
            };
            
            bool shouldRemove = agent?.State is not AgentState.Active && MissionHelpers.InTournament();
            Log.RunInfoPanelUpdate(() =>
            {
                var hm = heroesViewModel.FirstOrDefault(h => h.Name == heroModel.Name);
                if (shouldRemove)
                {
                    if (hm != null)
                    {
                        heroesViewModel.Remove(hm);
                    }
                }
                else if (hm != null)
                {
                    hm.Name = heroModel.Name;
                    hm.IsPlayerSide = heroModel.IsPlayerSide;
                    hm.MaxHP = heroModel.MaxHP;
                    hm.HP = heroModel.HP;
                    hm.IsRouted = heroModel.IsRouted;
                    hm.IsUnconscious = heroModel.IsUnconscious;
                    hm.IsKilled = heroModel.IsKilled;
                    hm.Retinue = heroModel.Retinue;
                    hm.GoldEarned = heroModel.GoldEarned;
                    hm.XPEarned = heroModel.XPEarned;
                    hm.Kills = heroModel.Kills;
                    hm.RetinueKills = heroModel.RetinueKills;
                    hm.CooldownFractionRemaining = heroModel.CooldownFractionRemaining;
                }
                else
                {
                    heroesViewModel.Add(heroModel);
                }
            });
        }
        
        // public static string KillStateVerb(AgentState state) =>
        //     state switch
        //     {
        //         AgentState.Routed => "routed",
        //         AgentState.Unconscious => "knocked out",
        //         AgentState.Killed => "killed",
        //         AgentState.Deleted => "deleted",
        //         _ => "fondled"
        //     };
        
        // public const int MaxLevel = 62;
        public const int MaxLevelInPractice = 32;
        
        // https://www.desmos.com/calculator/frzo6bkrwv
        // value returned is 0 < v < 1 if levelB < levelA, v = 1 if they are equal, and 1 < v < max if levelB > levelA
        public static float RelativeLevelScaling(int levelA, int levelB, float n, float max = float.MaxValue) 
            => Math.Min(MathF.Pow(1f - Math.Min(MaxLevelInPractice - 1, levelB - levelA) / (float)MaxLevelInPractice, -10f * MathF.Clamp(n, 0, 1)), max);
        
        public void ApplyKillEffects(Hero hero, Agent killer, Agent killed, AgentState state, int goldPerKill, int healPerKill, int xpPerKill, float subBoost, float? relativeLevelScaling, float? levelScalingCap)
        {
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

            if (goldPerKill != 0)
            {
                BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(hero, goldPerKill);
                GetHeroMissionState(hero).WonGold += goldPerKill;
            }
            
            if (healPerKill != 0)
            {
                killer.Health = Math.Min(killer.HealthLimit,
                    killer.Health + healPerKill);
            }

            if (xpPerKill != 0)
            {
                SkillXP.ImproveSkill(hero, xpPerKill, Skills.All, auto: true);
                GetHeroMissionState(hero).WonXP += xpPerKill;
            }
        }
        
        public void ApplyKilledEffects(Hero hero, Agent killer, AgentState state, int xpPerKilled, float subBoost, float? relativeLevelScaling, float? levelScalingCap)
        {
            if (subBoost != 1)
            {
                xpPerKilled = (int) (xpPerKilled * subBoost);
            }

            if (relativeLevelScaling.HasValue && killer?.Character != null)
            {
                // More reward for being killed by higher level characters
                float levelBoost = RelativeLevelScaling(hero.Level, killer.Character.Level, relativeLevelScaling.Value, levelScalingCap ?? 5);

                if (levelBoost != 1)
                {
                    xpPerKilled = (int) (xpPerKilled * levelBoost);
                }
            }

            if (xpPerKilled != 0)
            {
                SkillXP.ImproveSkill(hero, xpPerKilled, Skills.All, auto: true);
                GetHeroMissionState(hero).WonXP += xpPerKilled;
            }
        }
    }
}