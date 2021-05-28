using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Behaviors;
using HarmonyLib;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    [HarmonyPatch]
    internal class BLTAdoptAHeroCommonMissionBehavior : AutoMissionBehavior<BLTAdoptAHeroCommonMissionBehavior>
    {
        private MissionInfoPanel missionInfoPanel; 

        public BLTAdoptAHeroCommonMissionBehavior()
        {
            Log.AddInfoPanel(construct: () =>
            {
                missionInfoPanel = new MissionInfoPanel {HeroList = {ItemsSource = heroesViewModel}};
                return missionInfoPanel;
            });
        }
        
        protected override void OnEndMission()
        {
            Log.RemoveInfoPanel(missionInfoPanel);
        }
        
        private static Hero GetAdoptedHeroFromAgent(Agent agent)
        {
            var hero = (agent?.Character as CharacterObject)?.HeroObject;
            return hero?.IsAdopted() == true ? hero : null;
        }

        private class HeroViewModel : IComparable<HeroViewModel>, IComparable
        {
            public int CompareTo(HeroViewModel other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;
                int isPlayerSideComparison = -IsPlayerSide.CompareTo(other.IsPlayerSide);
                if (isPlayerSideComparison != 0) return isPlayerSideComparison;
                int killsComparison = Kills.CompareTo(other.Kills);
                if (killsComparison != 0) return killsComparison;
                return string.Compare(Name, other.Name, StringComparison.Ordinal);
            }

            public int CompareTo(object obj)
            {
                if (ReferenceEquals(null, obj)) return 1;
                if (ReferenceEquals(this, obj)) return 0;
                return obj is HeroViewModel other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(HeroViewModel)}");
            }

            public string Name { get; set; }
            public bool IsPlayerSide { get; set; }
            public bool IsRouted { get; set; }
            public bool IsUnconscious { get; set; }
            public bool IsKilled { get; set; }
            public float MaxHP { get; set; }
            public float HP { get; set; }
            public int Kills { get; set; }

            public string KillsText => Kills == 0 ? string.Empty : Kills.ToString();
            // public string KillsString => Kills.ToString();

            public Visibility KillsVisibility => Kills > 0 ? Visibility.Visible : Visibility.Hidden;

            public Brush TextColor => IsRouted
                ? Brushes.Yellow
                : IsKilled
                    ? Brushes.Crimson
                    : IsUnconscious
                        ? Brushes.Orange
                        : Brushes.Azure;

            public Brush ProgressBarForeground => IsPlayerSide 
                ? new SolidColorBrush(Color.FromArgb(0xFF, 0x66, 0x66, 0xCC))
                : new SolidColorBrush(Color.FromArgb(0xFF, 0xAA, 0x32, 0x77))
                ;
            public Brush ProgressBarBackground => IsPlayerSide 
                ? new SolidColorBrush(Color.FromArgb(0xFF, 0x20, 0x20, 0x50))
                : new SolidColorBrush(Color.FromArgb(0xFF, 0x40, 0x11, 0x22))
                ;
        }

        private void UpdateHeroVM(Agent agent)
        {
            var hero = GetAdoptedHeroFromAgent(agent);
            if (hero == null)
            {
                return;
            }
            var heroModel = new HeroViewModel
            {
                Name = hero.FirstName.ToString(),
                IsPlayerSide = hero.PartyBelongedTo?.LeaderHero == Hero.MainHero,
                MaxHP = agent.HealthLimit,
                HP = agent.Health,
                Kills = agent.KillCount,
                IsRouted = agent.State == AgentState.Routed,
                IsUnconscious = agent.State == AgentState.Unconscious,
                IsKilled = agent.State == AgentState.Killed,
            };
            bool shouldRemove = agent.State is not AgentState.Active && MissionHelpers.InTournament();
            Log.RunInfoPanelUpdate(() =>
            {
                if (shouldRemove)
                {
                    heroesViewModel.RemoveAll(h => h.Name == heroModel.Name);
                }
                else
                {
                    var hm = heroesViewModel.FirstOrDefault(h => h.Name == heroModel.Name);
                    if (hm != null)
                    {
                        hm.Name = heroModel.Name;
                        hm.IsPlayerSide = heroModel.IsPlayerSide;
                        hm.MaxHP = heroModel.MaxHP;
                        hm.HP = heroModel.HP;
                        hm.Kills = heroModel.Kills;
                        hm.IsRouted = heroModel.IsRouted;
                        hm.IsUnconscious = heroModel.IsUnconscious;
                        hm.IsKilled = heroModel.IsKilled;
                    }
                    else
                    {
                        heroesViewModel.Add(heroModel);
                    }
                }

                heroesViewModel.Sort();
                missionInfoPanel.HeroList.Items.Refresh();
            });
        }
        
        private List<HeroViewModel> heroesViewModel { get; set; } = new List<HeroViewModel>();
        public override void OnAgentCreated(Agent agent)
        {
            var hero = GetAdoptedHeroFromAgent(agent);
            if (hero == null)
            {
                return;
            }
            BLTAdoptAHeroCampaignBehavior.SetAgentStartingHealth(agent);

            UpdateHeroVM(agent);
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, int damage, in MissionWeapon affectorWeapon)
        {
            UpdateHeroVM(affectedAgent);
        }

        // public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        // {
        //     var affectedHero = GetAdoptedHeroFromAgent(affectedAgent);
        //     if (affectedHero != null)
        //     {
        //         affectedAgent.State = AgentState.Killed;
        //         // if (!globalSettings.AllowDeath && affectedAgent.State == AgentState.Killed)
        //         // {
        //         //     affectedAgent.State = AgentState.Unconscious;
        //         // }
        //     }
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
                var results = BLTAdoptAHeroCustomMissionBehavior.ApplyKilledEffects(
                    affectedHero, affectorAgent, agentState,
                    BLTAdoptAHeroModule.CommonConfig.XPPerKilled,
                    Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1),
                    BLTAdoptAHeroModule.CommonConfig.RelativeLevelScaling,
                    BLTAdoptAHeroModule.CommonConfig.LevelScalingCap
                );
                if (results.Any())
                {
                    Log.LogFeedResponse(affectedHero.FirstName.ToString(), results.ToArray());
                }

                UpdateHeroVM(affectedAgent);
            }
            var affectorHero = GetAdoptedHeroFromAgent(affectorAgent);
            if (affectorHero != null)
            {
                float horseFactor = !affectedAgent.IsHuman ? 0.25f : 1;
                Log.Trace($"[{nameof(BLTAdoptAHeroCommonMissionBehavior)}] {affectorHero} killed {affectedAgent?.ToString() ?? "unknown"}");
                var results = BLTAdoptAHeroCustomMissionBehavior.ApplyKillEffects(
                    affectorHero, affectorAgent, affectedAgent, agentState,
                    (int) (BLTAdoptAHeroModule.CommonConfig.GoldPerKill * horseFactor),
                    (int) (BLTAdoptAHeroModule.CommonConfig.HealPerKill * horseFactor),
                    (int) (BLTAdoptAHeroModule.CommonConfig.XPPerKill * horseFactor),
                    Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1),
                    BLTAdoptAHeroModule.CommonConfig.RelativeLevelScaling,
                    BLTAdoptAHeroModule.CommonConfig.LevelScalingCap
                );
                if (results.Any())
                {
                    Log.LogFeedResponse(affectorHero.FirstName.ToString(), results.ToArray());
                }
                UpdateHeroVM(affectorAgent);
            }
        }

        // public override void OnAgentDeleted(Agent affectedAgent)
        // {
        //     
        // }
        //
        // public override void OnAgentFleeing(Agent affectedAgent)
        // {
        //     
        // }
        //
        // public override void OnAgentPanicked(Agent affectedAgent)
        // {
        //     
        // }
    }
}