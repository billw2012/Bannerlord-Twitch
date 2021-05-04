using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox;
using SandBox.Source.Missions.Handlers;
using StoryMode.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Spawns the adopted hero into the current active mission")]
    internal class SummonHero : ActionHandlerBase
    {
        [CategoryOrder("Allowed Missions", 0)]
        [CategoryOrder("General", 1)]
        [CategoryOrder("Effects", 2)]
        private class Settings
        {
            [Category("Allowed Missions"), Description("Can summon for normal field battles between parties"), PropertyOrder(1)]
            public bool AllowFieldBattle { get; set; }
            [Category("Allowed Missions"), Description("Can summon in village battles"), PropertyOrder(2)]
            public bool AllowVillageBattle { get; set; }
            [Category("Allowed Missions"), Description("Can summon in sieges"), PropertyOrder(3)]
            public bool AllowSiegeBattle { get; set; }
            [Category("Allowed Missions"), Description("This includes walking about village/town/dungeon/keep"), PropertyOrder(4)]
            public bool AllowFriendlyMission { get; set; }
            [Category("Allowed Missions"), Description("Can summon in the practice arena"), PropertyOrder(5)]
            public bool AllowArena { get; set; }
            [Category("Allowed Missions"), Description("NOT IMPLEMENTED YET Can summon in tournaments"), PropertyOrder(6)]
            public bool AllowTournament { get; set; }
            [Category("Allowed Missions"), Description("Can summon in the hideout missions"), PropertyOrder(7)]
            public bool AllowHideOut { get; set; }
            [Category("General"), Description("Whether the hero is on the player or enemy side"), PropertyOrder(1)]
            public bool OnPlayerSide { get; set; }
            [Category("General"), Description("Maximum number of summons that can be active at the same time (i.e. max alive adopted heroes that can be in the mission)"), PropertyOrder(2)]
            public int? MaxSimultaneousSummons { get; set; }
            [Category("General"), Description("Whether the summoned hero is allowed to die"), PropertyOrder(3)]
            public bool AllowDeath { get; set; }
            [Category("General"), Description("Whether the summoned hero will always start with full health"), PropertyOrder(3)]
            public bool StartWithFullHealth { get; set; }
            [Category("General"), Description("Gold cost to summon"), PropertyOrder(1)]
            public int GoldCost { get; set; }
            [Category("Effects"), Description("Gold won if the heroes side wins"), PropertyOrder(2)]
            public int WinGold { get; set; }
            [Category("Effects"), Description("Gold lost if the heroes side loses"), PropertyOrder(3)]
            public int LoseGold { get; set; }
            [Category("Effects"), Description("Gold the hero gets for every kill"), PropertyOrder(4)]
            public int GoldPerKill { get; set; }
            [Category("Effects"), Description("XP the hero gets for every kill. It will be distributed using the Auto behavior of the SkillXP action: randomly between the top skills from each skill group (melee, ranged, movement, support, personal)."), PropertyOrder(5)]
            public int XPPerKill { get; set; }
            [Category("Effects"), Description("HP the hero gets for every kill"), PropertyOrder(6)]
            public int HealPerKill { get; set; }
            [Category("Effects"), Description("HP the hero gets every second they are alive in the mission"), PropertyOrder(7)]
            public float HealPerSecond { get; set; }
            [Category("Effects"), Description("Multiplier applied to (positive) effects for subscribers"), PropertyOrder(8)]
            public float SubBoost { get; set; } = 1;
        }

        protected override Type ConfigType => typeof(Settings);
        
        private delegate Agent MissionAgentHandler_SpawnWanderingAgentDelegate(
            MissionAgentHandler instance,
            LocationCharacter locationCharacter,
            MatrixFrame spawnPointFrame,
            bool hasTorch,
            bool noHorses);

        private static readonly MissionAgentHandler_SpawnWanderingAgentDelegate MissionAgentHandler_SpawnWanderingAgent 
            = (MissionAgentHandler_SpawnWanderingAgentDelegate) AccessTools.Method(typeof(MissionAgentHandler),
                    "SpawnWanderingAgent", new[] {typeof(LocationCharacter), typeof(MatrixFrame), typeof(bool), typeof(bool)})
                .CreateDelegate(typeof(MissionAgentHandler_SpawnWanderingAgentDelegate));
        
        // private delegate Agent SpawnWanderingAgentAutoDelegate(
        //     MissionAgentHandler instance,
        //     LocationCharacter locationCharacter,
        //     bool hasTorch);
        //
        // private static readonly SpawnWanderingAgentAutoDelegate SpawnWanderingAgentAuto = (SpawnWanderingAgentAutoDelegate)
        //     AccessTools.Method(typeof(MissionAgentHandler),
        //             "SpawnWanderingAgent",
        //             new[] {typeof(LocationCharacter), typeof(bool)})
        //         .CreateDelegate(typeof(SpawnWanderingAgentDelegate));
        
        private delegate MatrixFrame ArenaPracticeFightMissionController_GetSpawnFrameDelegate(
            ArenaPracticeFightMissionController instance, bool considerPlayerDistance, bool isInitialSpawn);

        private static readonly ArenaPracticeFightMissionController_GetSpawnFrameDelegate ArenaPracticeFightMissionController_GetSpawnFrame = (ArenaPracticeFightMissionController_GetSpawnFrameDelegate)
            AccessTools.Method(typeof(ArenaPracticeFightMissionController), "GetSpawnFrame", new[] {typeof(bool), typeof(bool)})
                .CreateDelegate(typeof(ArenaPracticeFightMissionController_GetSpawnFrameDelegate));

        private class BLTMissionBehavior : AutoMissionBehavior<BLTMissionBehavior>
        {
            public delegate void MissionOverDelegate();
            public delegate void MissionModeChangeDelegate(MissionMode oldMode, MissionMode newMode, bool atStart);
            public delegate void MissionResetDelegate();
            public delegate void GotAKillDelegate(Agent killed, AgentState agentState);
            public delegate void GotKilledDelegate(Agent killer, AgentState agentState);
            public delegate void MissionTickDelegate(float dt);
            
            public class Listeners
            {
                public Hero hero;
                public Agent agent;
                public MissionOverDelegate onMissionOver;
                public MissionModeChangeDelegate onModeChange;
                public MissionResetDelegate onMissionReset;
                public GotAKillDelegate onGotAKill;
                public GotKilledDelegate onGotKilled;
                public MissionTickDelegate onMissionTick;
                public MissionTickDelegate onSlowTick;
            }

            private readonly List<Listeners> listeners = new();

            public void AddListeners(Hero hero, Agent agent,
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
                    agent = agent,
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
                ForAgent(killedAgent, l => l.onGotKilled?.Invoke(killerAgent, agentState));
                ForAgent(killerAgent, l => l.onGotAKill?.Invoke(killedAgent, agentState));
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

            private Hero FindHero(Agent agent) => listeners.FirstOrDefault(l => l.agent == agent)?.hero;
            
            private void ForAll(Action<Listeners> action)
            {
                foreach (var listener in listeners)
                {
                    action(listener);
                }
            }

            private void ForAgent(Agent agent, Action<Listeners> action)
            {
                foreach (var listener in listeners.Where(l => l.agent == agent))
                {
                    action(listener);
                }
            }
        }

        private class BLTRemoveAgentsBehavior : AutoMissionBehavior<BLTRemoveAgentsBehavior>
        {
            private readonly List<Hero> heroesAdded = new();
 
            public void Add(Hero hero)
            {
                heroesAdded.Add(hero);
            }

            private void RemoveHeroes()
            {
                foreach (var hero in heroesAdded)
                {
                    LocationComplex.Current?.RemoveCharacterIfExists(hero);
                    if(CampaignMission.Current?.Location?.ContainsCharacter(hero) == true)
                        CampaignMission.Current.Location.RemoveCharacter(hero);
                }
                heroesAdded.Clear();
            }
            
            public override void HandleOnCloseMission()
            {
                base.HandleOnCloseMission();
                RemoveHeroes();
            }

            protected override void OnEndMission()
            {
                base.OnEndMission();
                RemoveHeroes();
            }

            public override void OnMissionDeactivate()
            {
                base.OnMissionDeactivate();
                RemoveHeroes();
            }

            public override void OnMissionRestart()
            {
                base.OnMissionRestart();
                RemoveHeroes();
            }
        }

        protected override void ExecuteInternal(ReplyContext context, object config,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            static string KillStateVerb(AgentState state) =>
                state switch
                {
                    AgentState.Routed => "routed",
                    AgentState.Unconscious => "knocked out",
                    AgentState.Killed => "killed",
                    AgentState.Deleted => "deleted",
                    _ => "fondled"
                };

            var settings = (Settings) config;

            var adoptedHero = AdoptAHero.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            if (adoptedHero.Gold < settings.GoldCost)
            {
                onFailure($"You do not have enough gold: you need {settings.GoldCost}, and you only have {adoptedHero.Gold}!");
                return;
            }
            if (adoptedHero.IsPlayerCompanion)
            {
                onFailure($"You are a player companion, you cannot be summoned in this manner!");
                return;
            }
            
            // SpawnAgent crashes in MissionMode.Deployment, would be nice to make it work though
            if (Mission.Current == null 
                || Mission.Current.Mode is MissionMode.Barter or MissionMode.Conversation or MissionMode.Deployment or
                    MissionMode.Duel or MissionMode.Replay or MissionMode.CutScene)
            {
                onFailure($"You cannot be summoned now!");
                return;
            }

            if(InArenaPracticeMission() && (!settings.AllowArena || !settings.OnPlayerSide) 
               || InTournament() && (!settings.AllowTournament)
               || InFieldBattleMission() && !settings.AllowFieldBattle
               || InVillageEncounter() && !settings.AllowVillageBattle
               || InSiegeMission() && !settings.AllowSiegeBattle
               || InFriendlyMission() && !settings.AllowFriendlyMission
               || InHideOutMission() && !settings.AllowHideOut
               || InTrainingFieldMission()
               || InArenaPracticeVisitingArea()
            )
            {
                onFailure($"You cannot be summoned now, this mission does not allow it!");
                return;
            }

            if (!Mission.Current.IsLoadingFinished)
            {
                onFailure($"You cannot be summoned now, the mission has not started yet!");
                return;
            }
            if (Mission.Current.IsMissionEnding || Mission.Current.MissionResult?.BattleResolved == true)
            {
                onFailure($"You cannot be summoned now, the mission is ending!");
                return;
            }
            
            if (HeroIsSpawned(adoptedHero))
            {
                onFailure($"You cannot be summoned, you are already here!");
                return;
            }

            Agent agent;
            
            if (CampaignMission.Current.Location != null)
            {
                var locationCharacter = LocationCharacter.CreateBodyguardHero(adoptedHero,
                    MobileParty.MainParty,
                    SandBoxManager.Instance.AgentBehaviorManager.AddBodyguardBehaviors);
                
                var missionAgentHandler = Mission.Current.GetMissionBehaviour<MissionAgentHandler>();
                var worldFrame = missionAgentHandler.Mission.MainAgent.GetWorldFrame();
                worldFrame.Origin.SetVec2(worldFrame.Origin.AsVec2 + (worldFrame.Rotation.f * 10f + worldFrame.Rotation.s).AsVec2);
                
                CampaignMission.Current.Location.AddCharacter(locationCharacter);

                if (InArenaPracticeMission())
                {
                    var controller = Mission.Current.GetMissionBehaviour<ArenaPracticeFightMissionController>();
                    var pos = ArenaPracticeFightMissionController_GetSpawnFrame(controller, false, false);
                    agent = MissionAgentHandler_SpawnWanderingAgent(missionAgentHandler, locationCharacter, pos, false, true);
                    var _participantAgents = (List<Agent>)AccessTools.Field(typeof(ArenaPracticeFightMissionController), "_participantAgents")
                        .GetValue(controller);
                    _participantAgents.Add(agent);
                }
                else
                {
                    agent = missionAgentHandler.SpawnLocationCharacter(locationCharacter);
                }
                //agent = SpawnWanderingAgent(missionAgentHandler, locationCharacter, worldFrame.ToGroundMatrixFrame(), false, true); 

                agent.SetTeam(settings.OnPlayerSide 
                    ? missionAgentHandler.Mission.PlayerTeam
                    : missionAgentHandler.Mission.PlayerEnemyTeam, false);

                // For arena mission we add fight everyone behaviours
                if (InArenaPracticeMission())
                {
                    if (agent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
                    {
                        var behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator
                            .GetBehaviorGroup<AlarmedBehaviorGroup>();
                        behaviorGroup.DisableCalmDown = true;
                        behaviorGroup.AddBehavior<FightBehavior>();
                        behaviorGroup.SetScriptedBehavior<FightBehavior>();
                    }
                    agent.SetWatchState(AgentAIStateFlagComponent.WatchState.Alarmed);
                }
                // For other player hostile situations we setup a 1v1 fight
                else if (!settings.OnPlayerSide)
                {
                    Mission.Current.GetMissionBehaviour<MissionFightHandler>().StartCustomFight(
                        new() {Agent.Main},
                        new() {agent}, false, false, false,
                        playerWon =>
                        {
                            if (settings.WinGold != 0)
                            {
                                if (!playerWon)
                                {
                                    Hero.MainHero.ChangeHeroGold(-settings.WinGold);
                                    // User gets their gold back also
                                    adoptedHero.ChangeHeroGold(settings.WinGold + settings.GoldCost);
                                    ActionManager.SendReply(context, $@"You won {settings.WinGold} gold!");
                                }
                                else if(settings.LoseGold > 0)
                                {
                                    Hero.MainHero.ChangeHeroGold(settings.LoseGold);
                                    adoptedHero.ChangeHeroGold(-settings.LoseGold);
                                    ActionManager.SendReply(context, $@"You lost {settings.LoseGold + settings.GoldCost} gold!");
                                }
                            }
                        },
                        true, null, null, null, null);
                }
                else
                {
                    InformationManager.AddQuickInformation(new TextObject($"I'm here!"), 1000,
                        adoptedHero.CharacterObject, "event:/ui/mission/horns/move");
                }

                // Bodyguard
                if (settings.OnPlayerSide && agent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
                {
                    var behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
                    (behaviorGroup.GetBehavior<FollowAgentBehavior>() ?? behaviorGroup.AddBehavior<FollowAgentBehavior>()).SetTargetAgent(Agent.Main);
                    behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
                }
                
                BLTRemoveAgentsBehavior.Current.Add(adoptedHero);
                // missionAgentHandler.SimulateAgent(agent);
            }
            else
            {
                PartyBase party = null;
                if (settings.OnPlayerSide && Mission.Current?.PlayerTeam != null)
                {
                    party = PartyBase.MainParty;
                }
                else if(!settings.OnPlayerSide && Mission.Current?.PlayerEnemyTeam != null)
                {
                    party = Mission.Current.PlayerEnemyTeam?.TeamAgents
                        ?.Select(a => a.Origin?.BattleCombatant as PartyBase)
                        .Where(p => p != null).SelectRandom();
                }

                if (party == null)
                {
                    onFailure($"Could not find a party for you to join!");
                    return;
                }

                agent = Mission.Current.SpawnTroop(
                    new PartyAgentOrigin(party, adoptedHero.CharacterObject, alwaysWounded: !settings.AllowDeath),
                    isPlayerSide: settings.OnPlayerSide,
                    hasFormation: true,
                    spawnWithHorse: adoptedHero.CharacterObject.HasMount() && Mission.Current.Mode != MissionMode.Stealth,
                    isReinforcement: true,
                    enforceSpawningOnInitialPoint: false,
                    formationTroopCount: 1,
                    formationTroopIndex: 8,
                    isAlarmed: true,
                    wieldInitialWeapons: true);


                float actualBoost = context.IsSubscriber ? settings.SubBoost : 1;
                BLTMissionBehavior.Current.AddListeners(adoptedHero, agent, 
                    onSlowTick: dt =>
                    {
                        if (settings.HealPerSecond != 0)
                        {
                            agent.Health = Math.Min(agent.HealthLimit, agent.Health + settings.HealPerSecond * dt * actualBoost);
                        }
                    },
                    onMissionOver: () => 
                    {
                        if (Mission.Current.MissionResult != null)
                        {
                            if (settings.OnPlayerSide == Mission.Current.MissionResult.PlayerVictory)
                            {
                                // User gets their gold back also
                                adoptedHero.ChangeHeroGold((int) (settings.WinGold * actualBoost + settings.GoldCost));
                                ActionManager.SendReply(context, $@"You won {settings.WinGold} gold!");
                            }
                            else if(settings.LoseGold > 0)
                            {
                                adoptedHero.ChangeHeroGold(-settings.LoseGold);
                                ActionManager.SendReply(context, $@"You lost {settings.LoseGold + settings.GoldCost} gold!");
                            }
                        }
                    },
                    onGotAKill: (killed, state) =>
                    {
                        if (killed != null)
                        {
                            Log.LogFeedBattle($"{adoptedHero.FirstName} {KillStateVerb(state)} {killed.Name}");
                        }
                        if (settings.GoldPerKill != 0)
                        {
                            int gold = (int) (settings.GoldPerKill * actualBoost);
                            adoptedHero.ChangeHeroGold(gold);
                            Log.LogFeedBattle($"{adoptedHero.FirstName}: +{gold} gold");
                        }

                        if (settings.HealPerKill != 0)
                        {
                            float prevHealth = agent.Health;
                            agent.Health = Math.Min(agent.HealthLimit, agent.Health + settings.HealPerKill * actualBoost);
                            float healthDiff = agent.Health - prevHealth;
                            if(healthDiff > 0)
                                Log.LogFeedBattle($"{adoptedHero.FirstName}: +{healthDiff}hp");
                        }

                        if (settings.XPPerKill != 0)
                        {
                            int xp = (int) (settings.XPPerKill * actualBoost);
                            (bool success, string description) = SkillXP.ImproveSkill(adoptedHero, xp, Skills.All, random: false, auto: true);
                            if(success)
                                Log.LogFeedBattle($"{adoptedHero.FirstName}: {description}");
                        }
                    },
                    onGotKilled: (killer, state) =>
                    {
                        Log.LogFeedBattle(killer != null
                            ? $"{adoptedHero.FirstName} was {KillStateVerb(state)} by {killer.Name}"
                            : $"{adoptedHero.FirstName} was {KillStateVerb(state)}");
                    }
                );
            }
            
            if (settings.StartWithFullHealth)
            {
                agent.Health = agent.HealthLimit;
            }

            var messages = settings.OnPlayerSide
                ? new List<string>
                {
                    "Don't worry, I've got your back!",
                    "I'm here!",
                    "Which one should I stab?",
                    "Once more unto the breach!",
                    "It's nothing personal!",
                    "Freeeeeedddooooooommmm!",
                }
                : new List<string>
                {
                    "Defend yourself!",
                    "Time for you to die!",
                    "You killed my father, prepare to die!",
                    "En garde!",
                    "It's stabbing time! For you.",
                    "It's nothing personal!",
                };
            if (InSiegeMission() && settings.OnPlayerSide) messages.Add($"Don't send me up the siege tower, its confusing!");
            InformationManager.AddQuickInformation(new TextObject(!string.IsNullOrEmpty(context.Args) ? context.Args : messages.SelectRandom()), 1000,
                adoptedHero.CharacterObject, "event:/ui/mission/horns/attack");

            adoptedHero.Gold -= settings.GoldCost;
            onSuccess($"You have joined the battle!");
        }

        private static bool HeroIsSpawned(Hero hero) 
            => //CampaignMission.Current.Location?.ContainsCharacter(hero) == true || 
               Mission.Current?.Agents.Any(a => a.Character == hero.CharacterObject) == true;

        private static bool InHideOutMission() => Mission.Current?.Mode == MissionMode.Stealth;
        private static bool InFieldBattleMission() => Mission.Current?.IsFieldBattle == true;

        private static bool InSiegeMission() 
            => Mission.Current?.IsFieldBattle != true 
               && Mission.Current?.GetMissionBehaviour<CampaignSiegeStateHandler>() != null;
        private static bool InArenaPracticeMission() 
            => CampaignMission.Current?.Location?.StringId == "arena"
               && Mission.Current?.Mode == MissionMode.Battle;
        private static bool InArenaPracticeVisitingArea() 
            => CampaignMission.Current?.Location?.StringId == "arena"
               && Mission.Current?.Mode != MissionMode.Battle;

        private static bool InTournament()
            => Mission.Current?.GetMissionBehaviour<TournamentFightMissionController>() != null 
               && Mission.Current?.Mode == MissionMode.Battle;

        private static bool InFriendlyMission() 
            => Mission.Current?.IsFriendlyMission == true && !InArenaPracticeMission();
        private static bool InConversation() => Mission.Current?.Mode == MissionMode.Conversation;
        private static bool InTrainingFieldMission()
            => Mission.Current?.GetMissionBehaviour<TrainingFieldMissionController>() != null;
        private static bool InVillageEncounter()
            => PlayerEncounter.LocationEncounter?.GetType() == typeof(VillageEncouter);
    }
}