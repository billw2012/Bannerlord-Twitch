using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
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
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Spawns the adopted hero into the current active mission")]
    internal class SummonHero : ActionHandlerBase
    {
        [CategoryOrder("General", -1)]
        [CategoryOrder("Allowed Missions", 0)]
        [CategoryOrder("General", 1)]
        [CategoryOrder("Effects", 2)]
        [CategoryOrder("End Effects", 3)]
        [CategoryOrder("Kill Effects", 4)]
        private class Settings
        {
            [Category("General"), YamlIgnore, ReadOnly(true), PropertyOrder(-100), Editor(typeof(MultilineTextNonEditable), typeof(MultilineTextNonEditable))]
            public string Help => "This allows viewers to spawn their adopted heroes into your missions/battles. It supports a custom shout: if used as a command then anything the viewer puts after the command will be shouted in game by the player, if used as a reward then just enable the 'IsUserInputRequired' and set the 'Prompt' to something like 'My shout'.";
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
            [Category("General"), Description("Maximum number of summons that can be active at the same time (i.e. max alive adopted heroes that can be in the mission) NOT IMPLEMENTED YET"), PropertyOrder(2)]
            public int? MaxSimultaneousSummons { get; set; }
            [Category("General"), Description("Whether the summoned hero is allowed to die"), PropertyOrder(3)]
            public bool AllowDeath { get; set; }
            [Category("General"), Description("Whether the summoned hero will always start with full health"), PropertyOrder(3)]
            public bool StartWithFullHealth { get; set; }
            [Category("General"), Description("Gold cost to summon"), PropertyOrder(1)]
            public int GoldCost { get; set; }

            [Category("Effects"), Description("Multiplier applied to (positive) effects for subscribers"), PropertyOrder(1)]
            public float SubBoost { get; set; } = 1;
            [Category("Effects"), Description("HP the hero gets every second they are alive in the mission"), PropertyOrder(2)]
            public float HealPerSecond { get; set; }

            [Category("End Effects"), Description("Gold won if the heroes side wins"), PropertyOrder(1)]
            public int WinGold { get; set; }
            [Category("End Effects"), Description("XP the hero gets if the heroes side wins"), PropertyOrder(2)]
            public int WinXP { get; set; }
            [Category("End Effects"), Description("Gold lost if the heroes side loses"), PropertyOrder(3)]
            public int LoseGold { get; set; }
            [Category("End Effects"), Description("XP the hero gets if the heroes side loses"), PropertyOrder(4)]
            public int LoseXP { get; set; }

            [Category("Kill Effects"), Description("Gold the hero gets for every kill"), PropertyOrder(1)]
            public int GoldPerKill { get; set; }
            [Category("Kill Effects"), Description("XP the hero gets for every kill"), PropertyOrder(2)]
            public int XPPerKill { get; set; }
            [Category("Kill Effects"), Description("XP the hero gets for being killed"), PropertyOrder(3)]
            public int XPPerKilled { get; set; }
            [Category("Kill Effects"), Description("HP the hero gets for every kill"), PropertyOrder(4)]
            public int HealPerKill { get; set; }
            [Category("Kill Effects"), Description("How much to scale the reward by, based on relative level of the two characters. If this is 0 (or not set) then the rewards are always as specified, if this is higher than 0 then the rewards increase if the killed unit is higher level than the hero, and decrease if it is lower. At a value of 0.5 (recommended) at level difference of 20 would give about 2.5 times the normal rewards for gold, xp and health."), PropertyOrder(5)]
            public float? RelativeLevelScaling { get; set; }
            [Category("Kill Effects"), Description("Caps the maximum multiplier for the level difference, defaults to 5 if not specified"), PropertyOrder(6)]
            public float? LevelScalingCap { get; set; }
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
                    agent.SetWatchState(Agent.WatchState.Alarmed);
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
                BLTMissionBehavior.Current.AddListeners(adoptedHero,
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
                            var results = new List<string>();
                            if (settings.OnPlayerSide == Mission.Current.MissionResult.PlayerVictory)
                            {
                                int actualGold = (int) (settings.WinGold * actualBoost + settings.GoldCost);
                                if (actualGold > 0)
                                {
                                    adoptedHero.ChangeHeroGold(actualGold);
                                    results.Add($"+{actualGold} gold");
                                }
                                int xp = (int) (settings.WinXP * actualBoost);
                                if (xp > 0)
                                {
                                    (bool success, string description) = SkillXP.ImproveSkill(adoptedHero, xp, Skills.All,
                                        random: false, auto: true);
                                    if (success)
                                    {
                                        results.Add(description);
                                    }
                                }
                            }
                            else
                            {
                                if (settings.LoseGold > 0)
                                {
                                    adoptedHero.ChangeHeroGold(-settings.LoseGold);
                                    results.Add($"-{settings.LoseGold} gold");
                                }
                                int xp = (int) (settings.LoseXP * actualBoost);
                                if (xp > 0)
                                {
                                    (bool success, string description) = SkillXP.ImproveSkill(adoptedHero, xp, Skills.All,
                                        random: false, auto: true);
                                    if (success)
                                    {
                                        results.Add(description);
                                    }
                                }
                            }
                            if (results.Any())
                            {
                                ActionManager.SendReply(context, results.ToArray());
                            }
                        }
                    },
                    onGotAKill: (killer, killed, state) =>
                    {
                        var results = BLTMissionBehavior.ApplyKillEffects(
                            adoptedHero, killer, killed, state,
                            settings.GoldPerKill,
                            settings.HealPerKill, 
                            settings.XPPerKill,
                            actualBoost,
                            settings.RelativeLevelScaling,
                            settings.LevelScalingCap
                        );
                        if (results.Any())
                        {
                            ActionManager.SendReply(context, results.ToArray());
                        }
                    },
                    onGotKilled: (killed, killer, state) =>
                    {
                        var results = BLTMissionBehavior.ApplyKilledEffects(
                            adoptedHero, killer, state,
                            settings.XPPerKilled,
                            actualBoost,
                            settings.RelativeLevelScaling,
                            settings.LevelScalingCap
                        );
                        if (results.Any())
                        {
                            ActionManager.SendReply(context, results.ToArray());
                        }
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