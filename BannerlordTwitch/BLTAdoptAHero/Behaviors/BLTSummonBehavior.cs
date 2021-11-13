using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Util;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    internal class BLTSummonBehavior : AutoMissionBehavior<BLTSummonBehavior>
    {
        public class RetinueState
        {
            public CharacterObject Troop;
            public Agent Agent;
            // We must record this separately, as the Agent.State is undefined once the Agent is deleted (the internal handle gets reused by the engine)
            public AgentState State;
            public bool Died;
        }

        public class HeroSummonState
        {
            public Hero Hero;
            public bool WasPlayerSide;
            public PartyBase Party;
            public AgentState State;
            public Agent CurrentAgent;
            public float SummonTime;
            public int TimesSummoned = 0;
            public List<RetinueState> Retinue { get; set; } = new();

            public int ActiveRetinue => Retinue.Count(r => r.State == AgentState.Active);
            public int DeadRetinue => Retinue.Count(r => r.Died);

            private float CooldownTime => BLTAdoptAHeroModule.CommonConfig.CooldownEnabled
                ? BLTAdoptAHeroModule.CommonConfig.GetCooldownTime(TimesSummoned) : 0;

            public bool InCooldown => BLTAdoptAHeroModule.CommonConfig.CooldownEnabled && SummonTime + CooldownTime > CampaignHelpers.GetTotalMissionTime();
            public float CooldownRemaining => !BLTAdoptAHeroModule.CommonConfig.CooldownEnabled ? 0 : Math.Max(0, SummonTime + CooldownTime - CampaignHelpers.GetTotalMissionTime());
            public float CoolDownFraction => !BLTAdoptAHeroModule.CommonConfig.CooldownEnabled ? 1 : 1f - CooldownRemaining / CooldownTime;
        }

        private readonly List<HeroSummonState> heroSummonStates = new();
        private readonly List<Action> onTickActions = new();

        public HeroSummonState GetHeroSummonState(Hero hero)
            => heroSummonStates.FirstOrDefault(h => h.Hero == hero);

        public HeroSummonState GetHeroSummonStateForRetinue(Agent retinueAgent) 
            => heroSummonStates.FirstOrDefault(h => h.Retinue.Any(r => r.Agent == retinueAgent));

        public HeroSummonState AddHeroSummonState(Hero hero, bool playerSide, PartyBase party)
        {
            var heroSummonState = new HeroSummonState
            {
                Hero = hero,
                WasPlayerSide = playerSide,
                Party = party,
                SummonTime = CampaignHelpers.GetTotalMissionTime(), 
            };
            heroSummonStates.Add(heroSummonState);
            return heroSummonState;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            SafeCall(() =>
            {
                // We only use this for heroes in battle
                if (CampaignMission.Current.Location != null) 
                    return;
                
                var adoptedHero = agent.GetAdoptedHero();
                if (adoptedHero == null)
                    return;

                var heroSummonState = GetHeroSummonState(adoptedHero) 
                                   ?? AddHeroSummonState(adoptedHero, 
                                       Mission != null 
                                       && agent.Team != null 
                                       && agent.Team.IsFriendOf(Mission.PlayerTeam),
                                       adoptedHero.GetMapEventParty());
                
                // First spawn, so spawn retinue also
                if (heroSummonState.TimesSummoned == 0 && RetinueAllowed())
                {
                    var formationClass = agent.Formation.FormationIndex;
                    SpawnRetinue(adoptedHero, ShouldBeMounted(formationClass), formationClass, 
                        heroSummonState, heroSummonState.WasPlayerSide);
                }

                heroSummonState.CurrentAgent = agent;
                heroSummonState.State = AgentState.Active;
                heroSummonState.TimesSummoned++;
                heroSummonState.SummonTime = CampaignHelpers.GetTotalMissionTime();
                // If hero isn't registered yet then this must be a hero that is part of one of the involved parties
                // already
            });
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            SafeCall(() =>
            {
                var heroSummonState = heroSummonStates.FirstOrDefault(h => h.CurrentAgent == affectedAgent);
                if (heroSummonState != null)
                {
                    heroSummonState.State = agentState;
                }

                // Set the final retinue state
                var (retinueOwner, retinueState) = heroSummonStates
                    .Select(h 
                        => (state: h, retinue: h.Retinue.FirstOrDefault(r => r.Agent == affectedAgent)))
                    .FirstOrDefault(h => h.retinue != null);

                if (retinueOwner != null)
                {
                    if (agentState == AgentState.Killed &&
                        MBRandom.RandomFloat <= BLTAdoptAHeroModule.CommonConfig.RetinueDeathChance)
                    {
                        retinueState.Died = true;
                        BLTAdoptAHeroCampaignBehavior.Current.KillRetinue(retinueOwner.Hero, affectedAgent.Character);
                        if (retinueOwner.Hero.FirstName != null)
                        {
                            Log.LogFeedResponse(retinueOwner.Hero.FirstName.ToString(),
                                $"Your {affectedAgent.Character} was killed in battle!");
                        }
                    }
                    retinueState.State = agentState;
                }
            });
        }

        public void DoNextTick(Action action)
        {
            onTickActions.Add(action);
        }

        public override void OnMissionTick(float dt)
        {
            SafeCall(() =>
            {
                var actionsToDo = onTickActions.ToList();
                onTickActions.Clear();
                foreach (var action in actionsToDo)
                {
                    action();
                }
            });
        }

        protected override void OnEndMission()
        {
            SafeCall(() =>
            {
                // Remove still living retinue troops from their parties
                foreach (var h in heroSummonStates)
                {
                    foreach (var r in h.Retinue.Where(r => r.State != AgentState.Killed))
                    {
                        h.Party?.MemberRoster?.AddToCounts(r.Troop, -1);
                    }
                }
            });
        }
        
        private static void SpawnRetinue(Hero adoptedHero, bool ownerIsMounted, FormationClass ownerFormationClass,
            HeroSummonState existingHero, bool onPlayerSide)
        {
            var retinueTroops = BLTAdoptAHeroCampaignBehavior.Current.GetRetinue(adoptedHero).ToList();

            bool retinueMounted = Mission.Current.Mode != MissionMode.Stealth
                                  && !MissionHelpers.InSiegeMission()
                                  && (ownerIsMounted || !BLTAdoptAHeroModule.CommonConfig.RetinueUseHeroesFormation);
            var agent_name = AccessTools.Field(typeof(Agent), "_name");
            foreach (var retinueTroop in retinueTroops)
            {
                // Don't modify formation for non-player side spawn as we don't really care
                bool hasPrevFormation = Campaign.Current.PlayerFormationPreferences
                                            .TryGetValue(retinueTroop, out var prevFormation)
                                        && onPlayerSide
                                        && BLTAdoptAHeroModule.CommonConfig.RetinueUseHeroesFormation;

                if (onPlayerSide && BLTAdoptAHeroModule.CommonConfig.RetinueUseHeroesFormation)
                {
                    Campaign.Current.SetPlayerFormationPreference(retinueTroop, ownerFormationClass);
                }

                existingHero.Party.MemberRoster.AddToCounts(retinueTroop, 1);

                var retinueAgent = SpawnAgent(onPlayerSide, retinueTroop, existingHero.Party, 
                    retinueTroop.IsMounted && retinueMounted);

                existingHero.Retinue.Add(new()
                {
                    Troop = retinueTroop,
                    Agent = retinueAgent,
                    State = AgentState.Active,
                });

                agent_name.SetValue(retinueAgent, new TextObject($"{retinueAgent.Name} ({adoptedHero.FirstName})"));

                retinueAgent.BaseHealthLimit *= Math.Max(1, BLTAdoptAHeroModule.CommonConfig.StartRetinueHealthMultiplier);
                retinueAgent.HealthLimit *= Math.Max(1, BLTAdoptAHeroModule.CommonConfig.StartRetinueHealthMultiplier);
                retinueAgent.Health *= Math.Max(1, BLTAdoptAHeroModule.CommonConfig.StartRetinueHealthMultiplier);

                BLTAdoptAHeroCustomMissionBehavior.Current.AddListeners(retinueAgent,
                    onGotAKill: (killer, killed, state) =>
                    {
                        Log.Trace($"[{nameof(SummonHero)}] {retinueAgent.Name} killed {killed?.Name ?? "unknown"}");
                        BLTAdoptAHeroCommonMissionBehavior.Current.ApplyKillEffects(
                            adoptedHero, killer, killed, state,
                            BLTAdoptAHeroModule.CommonConfig.RetinueGoldPerKill,
                            BLTAdoptAHeroModule.CommonConfig.RetinueHealPerKill,
                            0, 1,
                            BLTAdoptAHeroModule.CommonConfig.RelativeLevelScaling,
                            BLTAdoptAHeroModule.CommonConfig.LevelScalingCap
                        );
                    }
                );

                if (hasPrevFormation)
                {
                    Campaign.Current.SetPlayerFormationPreference(retinueTroop, prevFormation);
                }
            }
        }

        public static Agent SpawnAgent(bool onPlayerSide, CharacterObject troop, PartyBase party, bool spawnWithHorse)
        {
            var agent = Mission.Current.SpawnTroop(
                new PartyAgentOrigin(party, troop)
                , isPlayerSide: onPlayerSide
                , hasFormation: true
                , spawnWithHorse: spawnWithHorse
                , isReinforcement: true
                , enforceSpawningOnInitialPoint: false
                , formationTroopCount: 1
                , formationTroopIndex: 0
                , isAlarmed: true
                , wieldInitialWeapons: true
#if !e159 && !e1510 && !e160 && !e161
                , forceDismounted: false
                , initialPosition: null
                , initialDirection: null
#endif
            );
            agent.MountAgent?.FadeIn();
            agent.FadeIn();
            return agent;
        }

        public static bool ShouldBeMounted(FormationClass formationClass)
        {
            return Mission.Current.Mode != MissionMode.Stealth
                   && !MissionHelpers.InSiegeMission()
                   && formationClass is
                       FormationClass.Cavalry or
                       FormationClass.LightCavalry or
                       FormationClass.HeavyCavalry or
                       FormationClass.HorseArcher;
        }

        public static bool RetinueAllowed() => MissionHelpers.InSiegeMission() || MissionHelpers.InFieldBattleMission();
    }
}