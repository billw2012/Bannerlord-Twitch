using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Actions.Util;
using BLTAdoptAHero.UI;
using HarmonyLib;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    /// <summary>
    /// Behaviour that is active for all missions
    /// </summary>
    [HarmonyPatch]
    internal class BLTAdoptAHeroCommonMissionBehavior : AutoMissionBehavior<BLTAdoptAHeroCommonMissionBehavior>
    {
        private readonly List<Hero> activeHeroes = new();

        private class HeroMissionState
        {
            public AgentState LastAgentState { get; set; } = AgentState.Active;
            public int LastTeamIndex { get; set; }
            public int WonGold { get; set; }
            public int WonXP { get; set; }
            public int Kills { get; set; }
            public int RetinueKills { get; set; }
            public int KillStreak { get; set; }
        }

        private readonly Dictionary<Hero, HeroMissionState> heroMissionState = new();
        private readonly List<Agent> adoptedHeroMounts = new();

        public float PlayerSidePower { get; private set; }
        public float EnemySidePower { get; private set; }
        public float PlayerPowerRatio => PlayerSidePower / Math.Max(1, EnemySidePower);
        public float EnemyPowerRatio => EnemySidePower / Math.Max(1, PlayerSidePower);

        public float PlayerSideRewardMultiplier
        {
            get
            {
                if (BLTAdoptAHeroModule.CommonConfig.DifficultyScalingOnPlayersSide)
                {
                    return MathF.Clamp(MathF.Pow(EnemyPowerRatio, BLTAdoptAHeroModule.CommonConfig.DifficultyScalingClamped),
                        BLTAdoptAHeroModule.CommonConfig.DifficultyScalingMinClamped, 
                        BLTAdoptAHeroModule.CommonConfig.DifficultyScalingMaxClamped);
                }
                else
                {
                    return 1;
                }
            }
        }
        
        public float EnemySideRewardMultiplier
        {
            get
            {
                if (BLTAdoptAHeroModule.CommonConfig.DifficultyScalingOnEnemySide)
                {
                    return MathF.Clamp(MathF.Pow(PlayerPowerRatio, BLTAdoptAHeroModule.CommonConfig.DifficultyScalingClamped),
                        BLTAdoptAHeroModule.CommonConfig.DifficultyScalingMinClamped, 
                        BLTAdoptAHeroModule.CommonConfig.DifficultyScalingMaxClamped);
                }
                else
                {
                    return 1;
                }
            }
        }

        public override void OnAgentCreated(Agent agent)
        {
            SafeCall(() =>
            {
                var hero = agent.GetAdoptedHero();
                if (hero == null)
                {
                    return;
                }

                BLTAdoptAHeroCampaignBehavior.SetAgentStartingHealth(hero, agent);
                activeHeroes.Add(hero);
            });
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            SafeCall(() =>
            {
                var hero = agent.GetAdoptedHero();
                if (hero != null)
                {
                    GetHeroMissionState(hero).LastAgentState = AgentState.Active;

                    if (agent.MountAgent != null)
                    {
                        adoptedHeroMounts.Add(agent.MountAgent);
                    }
                }

                if (!agent.IsMount && agent.Team?.IsValid == true && Mission.PlayerTeam?.IsValid == true)
                {
                    if (agent.Team.IsFriendOf(Mission.PlayerTeam))
                    {
                        PlayerSidePower += agent.Character.GetPower();
                    }
                    else
                    {
                        EnemySidePower += agent.Character.GetPower();
                    }
                }
            });
        }

        private float lastTickT;

        public override void OnMissionTick(float dt)
        {
            SafeCall(() =>
            {
                if (lastTickT == 0)
                {
                    lastTickT = CampaignHelpers.GetApplicationTime();
                    return;
                }

                const float TickTime = 0.25f;
                if (CampaignHelpers.GetApplicationTime() - lastTickT > TickTime)
                {
                    lastTickT = CampaignHelpers.GetApplicationTime();

                    foreach (var h in activeHeroes)
                    {
                        UpdateHeroVM(h);
                    }
                    MissionInfoHub.Update();
                }
            });
        }

        protected override void OnEndMission()
        {
            MissionInfoHub.Clear();
        }

        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(Mission), "OnAgentRemoved")]
        public static void OnAgentRemovedPrefix(Mission __instance, Agent affectedAgent, Agent affectorAgent,
            ref AgentState agentState, KillingBlow killingBlow)
        {
            #if !DEBUG
            try
            #endif
            {
                if (affectedAgent.State == AgentState.Killed)
                {
                    if (affectedAgent.IsHuman)
                    {
                        // Stop adopted hero from dying if death is disabled or death chance roll fails
                        if (affectedAgent.IsAdopted())
                        {
                            if (!BLTAdoptAHeroModule.CommonConfig.AllowDeath
                                || StaticRandom.Next() > BLTAdoptAHeroModule.CommonConfig.DeathChance)
                            {
                                agentState = affectedAgent.State = AgentState.Unconscious;
                            }
                        }
                        // Stop non-adopted hero from dying if enabled, and death chance roll fails
                        else if (affectedAgent.IsHero
                                 && BLTAdoptAHeroModule.CommonConfig.ApplyDeathChanceToAllHeroes
                                 && StaticRandom.Next() > BLTAdoptAHeroModule.CommonConfig.DeathChance)
                        {
                            agentState = affectedAgent.State = AgentState.Unconscious;
                        }
                    }
                    // Stop adopted heroes horses from dying, as they are not easily replaceable
                    else if (affectedAgent.IsMount && Current?.adoptedHeroMounts.Contains(affectedAgent) == true)
                    {
                        agentState = affectedAgent.State = AgentState.Unconscious;
                    }
                }

                // Remove agent from mount tracking (if its not a mount or isn't tracked then this line doesn't do anything)
                Current?.adoptedHeroMounts.Remove(affectedAgent);
            }
            #if !DEBUG
            catch (Exception ex)
            {
                Log.Exception($"{nameof(BLTAdoptAHeroCommonMissionBehavior)}.{nameof(OnAgentRemovedPrefix)}", ex);
            }
            #endif
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            SafeCall(() =>
            {
                var affectedHero = affectedAgent.GetAdoptedHero();
                if (affectedHero != null)
                {
                    Log.Trace($"[{nameof(BLTAdoptAHeroCommonMissionBehavior)}] {affectedHero} was made " +
                              $"{agentState} by {affectorAgent?.Name ?? "unknown"}");

                    if (!BLTAdoptAHeroModule.TournamentConfig.DisableKillRewardsInTournament ||
                        !MissionHelpers.InTournament())
                    {
                        ApplyKilledEffects(
                            affectedHero, affectorAgent, agentState,
                            BLTAdoptAHeroModule.CommonConfig.XPPerKilled,
                            Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1),
                            BLTAdoptAHeroModule.CommonConfig.RelativeLevelScaling,
                            BLTAdoptAHeroModule.CommonConfig.LevelScalingCap
                        );
                    }

                    if (!BLTAdoptAHeroModule.TournamentConfig.DisableTrackingKillsTournament ||
                        !MissionHelpers.InTournament())
                    {
                        ResetKillStreak(affectedHero);
                        BLTAdoptAHeroCampaignBehavior.Current.IncreaseHeroDeaths(affectedHero, affectorAgent);
                    }

                    GetHeroMissionState(affectedHero).LastAgentState = agentState;
                }

                var affectorHero = affectorAgent.GetAdoptedHero();
                if (affectorHero != null)
                {
                    Log.Trace($"[{nameof(BLTAdoptAHeroCommonMissionBehavior)}] {affectorHero} made " +
                              $"{affectedAgent?.Name ?? "unknown"} {agentState}");

                    if (!BLTAdoptAHeroModule.TournamentConfig.DisableKillRewardsInTournament ||
                        !MissionHelpers.InTournament())
                    {
                        float horseFactor = affectedAgent?.IsHuman == false ? 0.25f : 1;
                        ApplyKillEffects(
                            affectorHero, affectorAgent, affectedAgent, agentState,
                            (int) (BLTAdoptAHeroModule.CommonConfig.GoldPerKill * horseFactor),
                            (int) (BLTAdoptAHeroModule.CommonConfig.HealPerKill * horseFactor),
                            (int) (BLTAdoptAHeroModule.CommonConfig.XPPerKill * horseFactor),
                            Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1),
                            BLTAdoptAHeroModule.CommonConfig.RelativeLevelScaling,
                            BLTAdoptAHeroModule.CommonConfig.LevelScalingCap
                        );
                    }

                    if (affectedAgent?.IsHuman == true && agentState is AgentState.Unconscious or AgentState.Killed 
                        && (!BLTAdoptAHeroModule.TournamentConfig.DisableTrackingKillsTournament 
                            || !MissionHelpers.InTournament()))
                    {
                        GetHeroMissionState(affectorHero).Kills++;
                        AddKillStreak(affectorHero);
                        BLTAdoptAHeroCampaignBehavior.Current.IncreaseKills(affectorHero, affectedAgent);
                    }
                }

                var affectorRetinueOwner = BLTSummonBehavior.Current?.GetHeroSummonStateForRetinue(affectorAgent);
                if (affectorRetinueOwner != null)
                {
                    GetHeroMissionState(affectorRetinueOwner.Hero).RetinueKills++;
                }
            });
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
        
        private void AddKillStreak(Hero hero)
        {
            // declare variable right where it's passed
            var heroState = GetHeroMissionState(hero);
            heroState.KillStreak++;

            var currKillStreak = BLTAdoptAHeroModule.CommonConfig.KillStreaks?.FirstOrDefault(k => k.Enabled && heroState.KillStreak == k.KillsRequired);
            if (currKillStreak != null)
            {
                if (BLTAdoptAHeroModule.CommonConfig.ShowKillStreakPopup 
                    && currKillStreak.ShowNotification 
                    && !LocString.IsNullOrEmpty(currKillStreak.NotificationText))
                {
                    string message = currKillStreak.NotificationText.ToString(
                        ("{viewer}", hero.FirstName.ToString()),
                        ("{player}", hero.FirstName.ToString()),
                        ("{kills}",currKillStreak.KillsRequired.ToString()),
                        ("{name}",currKillStreak.Name));
                    Log.ShowInformation(message, hero.CharacterObject, BLTAdoptAHeroModule.CommonConfig.KillStreakPopupAlertSound);
                }
                ApplyStreakEffects(hero, currKillStreak.GoldReward, currKillStreak.XPReward,
                    Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1),
                    BLTAdoptAHeroModule.CommonConfig.RelativeLevelScaling,
                    BLTAdoptAHeroModule.CommonConfig.LevelScalingCap);
            }
        }

        private void ResetKillStreak(Hero hero)
        {
            GetHeroMissionState(hero).KillStreak = 0;
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
        
        private static bool IsHeroOnPlayerSide(Hero hero) 
            => hero.PartyBelongedTo?.MapEventSide?.MissionSide == PlayerEncounter.Current?.PlayerSide;

        private void UpdateHeroVM(Hero hero)
        {
            var heroState = GetHeroMissionState(hero);

            if (!activeHeroes.Contains(hero))
            {
                activeHeroes.Add(hero);
            }

            var summonState = BLTSummonBehavior.Current?.GetHeroSummonState(hero);
            
            var agent = summonState?.CurrentAgent ?? hero.GetAgent();

            var state = summonState?.State ?? agent?.State ?? heroState.LastAgentState;
            
            // So that heroes are cleaned up at the end of rounds in tournament 
            bool shouldRemove = agent?.State is not AgentState.Active && MissionHelpers.InTournament();

            if (shouldRemove)
            {
                MissionInfoHub.Remove(hero.FirstName.Raw());
            }
            else
            {
                if (agent?.Team != null && agent.State == AgentState.Active)
                {
                    heroState.LastTeamIndex = agent.Team.TeamIndex;
                }

                MissionInfoHub.UpdateHero(new()
                {
                    Name = hero.FirstName.Raw(),
                    IsPlayerSide = summonState?.WasPlayerSide ?? IsHeroOnPlayerSide(hero),
                    TournamentTeam = MissionHelpers.InTournament() ? heroState.LastTeamIndex : -1,
                    MaxHP = agent?.HealthLimit ?? 100,
                    HP = agent != null && state == AgentState.Active ? agent.Health : 0,
                    CooldownFractionRemaining = 1 - summonState?.CoolDownFraction ?? 0,
                    CooldownSecondsRemaining = summonState?.CooldownRemaining ?? 0,
                    ActivePowerFractionRemaining = state is AgentState.Active ? ActivePowerFractionRemaining(hero) : 0,
                    State = state.ToString().ToLower(),
                    Retinue = summonState?.ActiveRetinue ?? 0,
                    DeadRetinue = summonState?.DeadRetinue ?? 0,
                    GoldEarned = heroState.WonGold,
                    XPEarned = heroState.WonXP,
                    Kills = heroState.Kills,
                    RetinueKills = heroState.RetinueKills,
                });
            }
        }

        private static float ActivePowerFractionRemaining(Hero hero)
        {
            var classDef = BLTAdoptAHeroCampaignBehavior.Current?.GetClass(hero);
            (float duration, float remaining) = classDef?.ActivePower?.DurationRemaining(hero) ?? (1, 0);
            return duration == 0 ? 0 : remaining / duration;
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
        
        public void ApplyStreakEffects(Hero hero, int goldStreak, int xpStreak, float subBoost, float? relativeLevelScaling, float? levelScalingCap)
        {
            goldStreak = (int)(goldStreak * subBoost);
            xpStreak = (int)(xpStreak * subBoost);

            if (relativeLevelScaling.HasValue)
            {
                // More reward for killing higher level characters
                float levelBoost = RelativeLevelScaling(hero.Level, BLTAdoptAHeroModule.CommonConfig.ReferenceLevelReward, relativeLevelScaling.Value, levelScalingCap ?? 5);

                goldStreak = (int)(goldStreak * levelBoost);
                xpStreak = (int)(xpStreak * levelBoost);
            }

            if (goldStreak != 0)
            {
                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(hero, goldStreak);
                GetHeroMissionState(hero).WonGold += goldStreak;
            }

            if (xpStreak != 0)
            {
                (bool success, string _) = SkillXP.ImproveSkill(hero, xpStreak, SkillsEnum.All, auto: true);
                if (success)
                {
                    GetHeroMissionState(hero).WonXP += xpStreak;
                }
            }
        }

        public void ApplyKillEffects(Hero hero, Agent killer, Agent killed, AgentState state, int goldPerKill, int healPerKill, int xpPerKill, float subBoost, float? relativeLevelScaling, float? levelScalingCap)
        {
            goldPerKill = (int) (goldPerKill * subBoost);
            healPerKill = (int) (healPerKill * subBoost);
            xpPerKill = (int) (xpPerKill * subBoost);

            if (relativeLevelScaling.HasValue && killed?.Character != null)
            {
                // More reward for killing higher level characters
                float levelBoost = RelativeLevelScaling(hero.Level, killed.Character.Level, relativeLevelScaling.Value, levelScalingCap ?? 5);

                goldPerKill = (int) (goldPerKill * levelBoost);
                healPerKill = (int) (healPerKill * levelBoost);
                xpPerKill = (int) (xpPerKill * levelBoost);
            }

            if (goldPerKill != 0)
            {
                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(hero, goldPerKill);
                GetHeroMissionState(hero).WonGold += goldPerKill;
            }
            
            if (healPerKill != 0)
            {
                killer.Health = Math.Min(killer.HealthLimit, killer.Health + healPerKill);
            }

            if (xpPerKill != 0)
            {
                SkillXP.ImproveSkill(hero, xpPerKill, SkillsEnum.All, auto: true);
                GetHeroMissionState(hero).WonXP += xpPerKill;
            }
        }

        private void ApplyKilledEffects(Hero hero, Agent killer, AgentState state, int xpPerKilled, float subBoost, float? relativeLevelScaling, float? levelScalingCap)
        {
            xpPerKilled = (int) (xpPerKilled * subBoost);

            if (relativeLevelScaling.HasValue && killer?.Character != null)
            {
                // More reward for being killed by higher level characters
                float levelBoost = RelativeLevelScaling(hero.Level, killer.Character.Level, relativeLevelScaling.Value, levelScalingCap ?? 5);

                xpPerKilled = (int) (xpPerKilled * levelBoost);
            }

            if (xpPerKilled != 0)
            {
                SkillXP.ImproveSkill(hero, xpPerKilled, SkillsEnum.All, auto: true);
                GetHeroMissionState(hero).WonXP += xpPerKilled;
            }
        }

        public void RecordGoldGain(Hero hero, int gold)
        {
            GetHeroMissionState(hero).WonGold += gold;
        }
        
        public void RecordXPGain(Hero hero, int xp)
        {
            GetHeroMissionState(hero).WonXP += xp;
        }
    }
}