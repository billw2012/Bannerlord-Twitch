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
    [HarmonyPatch, UsedImplicitly, Description("Spawns the adopted hero into the current active mission")]
    internal class SummonHero : ActionHandlerBase
    {
        private class FormationItemSource : IItemsSource
        {
            public ItemCollection GetValues()
            {
                var col = new ItemCollection
                {
                    //"Unset",
                    "Infantry",
                    "Ranged",
                    "Cavalry",
                    "HorseArcher",
                    "Skirmisher",
                    "HeavyInfantry",
                    "LightCavalry",
                    "HeavyCavalry",
                    "Bodyguard",
                };
                return col;
            }
        }
        
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
            // [Category("General"), Description("Maximum number of summons that can be active at the same time (i.e. max alive adopted heroes that can be in the mission) NOT IMPLEMENTED YET"), PropertyOrder(2)]
            // public int? MaxSimultaneousSummons { get; set; }
            
            [Category("General"), Description("Gold cost to summon"), PropertyOrder(5)]
            public int GoldCost { get; set; }

            [Category("General"), Description("Which formation to add summoned units to"), PropertyOrder(6),
             ItemsSource(typeof(SummonHero.FormationItemSource))]
            public string PreferredFormation { get; set; }

            [Category("General"), Description("Sound to play when summoned"), PropertyOrder(7)]
            public Log.Sound AlertSound { get; set; }

            [Category("Effects"), Description("Multiplier applied to (positive) effects for subscribers"), PropertyOrder(1)]
            public float SubBoost { get; set; }
            
            [Category("Effects"), Description("HP the hero gets every second they are alive in the mission"), PropertyOrder(2)]
            public float HealPerSecond { get; set; }
        }

        protected override Type ConfigType => typeof(SummonHero.Settings);
        
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

        private static readonly List<string> FriendlySummonMessages = new List<string>
        {
            "Don't worry, I've got your back!",
            "I'm here!",
            "Which one should I stab?",
            "Once more unto the breach!",
            "Freeeeeedddooooooommmm!",
            "Remember the Alamo!",
            "Alala!",
            "Eleleu!",
            "Deus vult!",
            "Banzai!",
            "Liberty or Death!",
            "Har Har Mahadev!",
            "Desperta ferro!",
            "Alba gu bràth!",
            "Santiago!",
            "Huzzah!",
            "War... war never changes...",
            "Need a hand?",
            "May we live to see the next sunrise!",
            "For glory, charge!",
            "I'm going to just hide behind the rest of you...",
            "Why am I here!?",
            "The price has been paid. I am at your service.",
        };

        private static readonly List<string> EnemySummonMessages = new List<string>
        {
            "Defend yourself!",
            "Time for you to die!",
            "You killed my father, prepare to die!",
            "En garde!",
            "It's stabbing time! For you.",
            "It's nothing personal!",
            "Curse my sudden but inevitable betrayal!",
            "I just don't like you!",
            "I'm gonna put some dirt in your eye!",
            "I'll mount your head on a pike!",
            "Don't hate me, it's just business...",
            "Never should have come here!",
            "Your money or life!",
            "I'm sorry, but I must stop you.",
        };

        private class BLTRemoveAgentsBehavior : AutoMissionBehavior<SummonHero.BLTRemoveAgentsBehavior>
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
                    Log.Trace($"[SummonHero] Removing hero {hero}");
                    LocationComplex.Current?.RemoveCharacterIfExists(hero);
                    if(CampaignMission.Current?.Location?.ContainsCharacter(hero) == true)
                        CampaignMission.Current.Location.RemoveCharacter(hero);
                }
                heroesAdded.Clear();
            }
            
            public override void HandleOnCloseMission()
            {
                base.HandleOnCloseMission();
                Log.Trace($"[SummonHero] HandleOnCloseMission");
                RemoveHeroes();
            }

            protected override void OnEndMission()
            {
                base.OnEndMission();
                Log.Trace($"[SummonHero] OnEndMission");
                RemoveHeroes();
            }

            public override void OnMissionDeactivate()
            {
                base.OnMissionDeactivate();
                Log.Trace($"[SummonHero] OnMissionDeactivate");
                RemoveHeroes();
            }

            public override void OnMissionRestart()
            {
                base.OnMissionRestart();
                Log.Trace($"[SummonHero] OnMissionRestart");
                RemoveHeroes();
            }
        }

        internal class BLTSummonBehavior : AutoMissionBehavior<SummonHero.BLTSummonBehavior>
        {
            public class SummonedHero
            {
                public Hero Hero;
                public bool WasPlayerSide;
                public FormationClass Formation;
                public PartyBase Party;
            }
            private readonly List<SummonedHero> summonedHeroes = new();
            private readonly List<Action> onTickActions = new();

            public SummonedHero GetSummonedHero(Hero hero)
                => summonedHeroes.FirstOrDefault(h => h.Hero == hero);

            public SummonedHero AddSummonedHero(Hero hero, bool playerSide, FormationClass formationClass, PartyBase party)
            {
                var newSummonedHero = new SummonedHero
                {
                    Hero = hero,
                    WasPlayerSide = playerSide,
                    Formation = formationClass,
                    Party = party,
                };
                summonedHeroes.Add(newSummonedHero);
                return newSummonedHero;
            }

            public void DoNextTick(Action action)
            {
                onTickActions.Add(action);
            }

            public override void OnMissionTick(float dt)
            {
                base.OnMissionTick(dt);
                var actionsToDo = onTickActions.ToList();
                onTickActions.Clear();
                foreach (var action in actionsToDo)
                {
                    action();
                }
            }
        }
        
        
        protected override void ExecuteInternal(ReplyContext context, object config,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            var settings = (SummonHero.Settings) config;

            var adoptedHero = BLTAdoptAHeroCampaignBehavior.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            int availableGold = BLTAdoptAHeroCampaignBehavior.Get().GetHeroGold(adoptedHero);
            if (availableGold < settings.GoldCost)
            {
                onFailure($"You do not have enough gold: you need {settings.GoldCost}, and you only have {availableGold}!");
                return;
            }

            // if (adoptedHero.IsPlayerCompanion)
            // {
            //     onFailure($"You are a player companion, you cannot be summoned in this manner!");
            //     return;
            // }
            
            // SpawnAgent (as we call it) crashes if called in MissionMode.Deployment (would be nice to make it work though)
            if (Mission.Current == null 
                || Mission.Current.Mode is MissionMode.Barter or MissionMode.Conversation or MissionMode.Deployment or
                    MissionMode.Duel or MissionMode.Replay or MissionMode.CutScene)
            {
                onFailure($"You cannot be summoned now!");
                return;
            }

            if(MissionHelpers.InArenaPracticeMission() && (!settings.AllowArena || !settings.OnPlayerSide) 
               || MissionHelpers.InTournament() && (!settings.AllowTournament)
               || MissionHelpers.InFieldBattleMission() && !settings.AllowFieldBattle
               || MissionHelpers.InVillageEncounter() && !settings.AllowVillageBattle
               || MissionHelpers.InSiegeMission() && !settings.AllowSiegeBattle
               || MissionHelpers.InFriendlyMission() && !settings.AllowFriendlyMission
               || MissionHelpers.InHideOutMission() && !settings.AllowHideOut
               || MissionHelpers.InTrainingFieldMission()
               || MissionHelpers.InArenaPracticeVisitingArea()
            )
            {
                onFailure($"You cannot be summoned now, this mission does not allow it!");
                return;
            }

            if (!Mission.Current.IsLoadingFinished 
                || Mission.Current?.GetMissionBehaviour<TournamentFightMissionController>() != null && Mission.Current.Mode != MissionMode.Battle)
            {
                onFailure($"You cannot be summoned now, the mission has not started yet!");
                return;
            }
            if (Mission.Current.IsMissionEnding || Mission.Current.MissionResult?.BattleResolved == true)
            {
                onFailure($"You cannot be summoned now, the mission is ending!");
                return;
            }
            
            if (MissionHelpers.HeroIsSpawned(adoptedHero))
            {
                onFailure($"You cannot be summoned, you are already here!");
                return;
            }

            if (CampaignMission.Current.Location != null)
            {
                var locationCharacter = LocationCharacter.CreateBodyguardHero(adoptedHero,
                    MobileParty.MainParty,
                    SandBoxManager.Instance.AgentBehaviorManager.AddBodyguardBehaviors);
                
                var missionAgentHandler = Mission.Current.GetMissionBehaviour<MissionAgentHandler>();
                var worldFrame = missionAgentHandler.Mission.MainAgent.GetWorldFrame();
                worldFrame.Origin.SetVec2(worldFrame.Origin.AsVec2 + (worldFrame.Rotation.f * 10f + worldFrame.Rotation.s).AsVec2);
                
                CampaignMission.Current.Location.AddCharacter(locationCharacter);

                Agent agent;
                if (MissionHelpers.InArenaPracticeMission())
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
                if (MissionHelpers.InArenaPracticeMission())
                {
                    if (agent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
                    {
                        var behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator
                            .GetBehaviorGroup<AlarmedBehaviorGroup>();
                        behaviorGroup.DisableCalmDown = true;
                        behaviorGroup.AddBehavior<FightBehavior>();
                        behaviorGroup.SetScriptedBehavior<FightBehavior>();
                    }
                    #if BL_V_1_5_9
                    agent.SetWatchState(AgentAIStateFlagComponent.WatchState.Alarmed);
                    #else
                    agent.SetWatchState(Agent.WatchState.Alarmed);
                    #endif
                }
                // For other player hostile situations we setup a 1v1 fight
                else if (!settings.OnPlayerSide)
                {
                    Mission.Current.GetMissionBehaviour<MissionFightHandler>().StartCustomFight(
                        new() {Agent.Main},
                        new() {agent}, false, false, false,
                        playerWon =>
                        {
                            if (BLTAdoptAHeroModule.CommonConfig.WinGold != 0)
                            {
                                if (!playerWon)
                                {
                                    Hero.MainHero.ChangeHeroGold(-BLTAdoptAHeroModule.CommonConfig.WinGold);
                                    // User gets their gold back also
                                    BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(adoptedHero, BLTAdoptAHeroModule.CommonConfig.WinGold + settings.GoldCost);
                                    
                                    ActionManager.SendReply(context, $@"You won {BLTAdoptAHeroModule.CommonConfig.WinGold} gold!");
                                }
                                else if(BLTAdoptAHeroModule.CommonConfig.LoseGold > 0)
                                {
                                    Hero.MainHero.ChangeHeroGold(BLTAdoptAHeroModule.CommonConfig.LoseGold);
                                    BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(adoptedHero, -BLTAdoptAHeroModule.CommonConfig.LoseGold);
                                    ActionManager.SendReply(context, $@"You lost {BLTAdoptAHeroModule.CommonConfig.LoseGold + settings.GoldCost} gold!");
                                }
                            }
                        },
                        true, null, null, null, null);
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
                
                //SetAgentHealth(agent);
                
                var messages = settings.OnPlayerSide
                    ? FriendlySummonMessages
                    : EnemySummonMessages;

                Log.ShowInformation(!string.IsNullOrEmpty(context.Args) ? context.Args : messages.SelectRandom(), adoptedHero.CharacterObject, settings.AlertSound);

                BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(adoptedHero, -settings.GoldCost);

                onSuccess($"You have joined the battle!");
            }
            else
            {
                // We need to synchronize troop spawning to the MissionBehaviour OnMissionTick, or occasional null pointer  
                // crashes seem to happen in the engine...
                BLTSummonBehavior.Current.DoNextTick(() =>
                {
                    var existingHero = BLTSummonBehavior.Current.GetSummonedHero(adoptedHero);
                    if (existingHero != null && existingHero.WasPlayerSide != settings.OnPlayerSide)
                    {
                        onFailure($"You cannot switch sides, you traitor!");
                        return;
                    }

                    if (existingHero != null
                        && BLTAdoptAHeroModule.CommonConfig.AllowDeath
                        && Mission.Current?.AllAgents?.FirstOrDefault(a => a.Character == adoptedHero.CharacterObject)
                            ?.State == AgentState.Killed)
                    {
                        onFailure($"You cannot summon, you DIED!");
                        return;
                    }

                    float actualBoost = context.IsSubscriber ? Math.Max(settings.SubBoost, 1) : 1;

                    bool firstSummon = existingHero == null; 
                    if (firstSummon)
                    {
                        // If the hero exists in one of the parties already they cannot summon
                        // TODO: let them summon their retinue anyway
                        var existingParty = (PartyBase.MainParty.MapEvent?.InvolvedParties ?? PartyBase.MainParty.SiegeEvent?.Parties)?
                            .FirstOrDefault(p => p.MemberRoster.GetTroopCount(adoptedHero.CharacterObject) != 0);
                        if(existingParty != null)
                        {
                            onFailure($"You cannot be summoned, your party is already here!");
                            return;
                        }
                        PartyBase party = null;
                        if (settings.OnPlayerSide && Mission.Current?.PlayerTeam != null &&
                            Mission.Current?.PlayerTeam.ActiveAgents.Any() == true)
                        {
                            party = PartyBase.MainParty;
                        }
                        else if (!settings.OnPlayerSide && Mission.Current?.PlayerEnemyTeam != null &&
                                 Mission.Current?.PlayerEnemyTeam.ActiveAgents.Any() == true)
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

                        var originalParty = adoptedHero.PartyBelongedTo;
                        bool wasLeader = adoptedHero.PartyBelongedTo?.LeaderHero == adoptedHero;
                        if (originalParty?.Party != party)
                        {
                            originalParty?.Party?.AddMember(adoptedHero.CharacterObject, -1);
                            party.AddMember(adoptedHero.CharacterObject, 1);
                        }

                        // We don't support Unset, or General formations, and implement custom behaviour for Bodyguard
                        if (!Enum.TryParse(settings.PreferredFormation, out FormationClass formationClass)
                                 || formationClass is not (FormationClass.Ranged or FormationClass.Cavalry or FormationClass.HorseArcher
                                     or FormationClass.Skirmisher or FormationClass.HeavyInfantry or FormationClass.LightCavalry
                                     or FormationClass.HeavyCavalry or FormationClass.Bodyguard))
                        {
                            formationClass = FormationClass.Bodyguard;
                        }

                        BLTAdoptAHeroCustomMissionBehavior.Current.AddListeners(adoptedHero,
                            onSlowTick: dt =>
                            {
                                if (settings.HealPerSecond != 0)
                                {
                                    var activeAgent = Mission.Current?.Agents?.FirstOrDefault(a =>
                                        a.IsActive() && a.Character == adoptedHero.CharacterObject);
                                    if (activeAgent?.IsActive() == true)
                                    {
                                        Log.Trace($"[{nameof(SummonHero)}] healing {adoptedHero}");
                                        activeAgent.Health = Math.Min(activeAgent.HealthLimit,
                                            activeAgent.Health + settings.HealPerSecond * dt * actualBoost);
                                    }
                                }
                            },
                            onMissionOver: () =>
                            {
                                if (adoptedHero.PartyBelongedTo != originalParty)
                                {
                                    party.AddMember(adoptedHero.CharacterObject, -1);
                                    // Use insert at front to make sure we put the character back as party leader if they were previously
                                    originalParty?.Party?.MemberRoster.AddToCounts(adoptedHero.CharacterObject, 1,
                                        insertAtFront: wasLeader);
                                    Log.Trace($"[{nameof(SummonHero)}] moving {adoptedHero} from {party} back to {originalParty?.Party?.ToString() ?? "no party"}");
                                }

                                if (Mission.Current?.MissionResult != null)
                                {
                                    var results = new List<string>();
                                    if (settings.OnPlayerSide == Mission.Current.MissionResult.PlayerVictory)
                                    {
                                        int actualGold = (int) (BLTAdoptAHeroModule.CommonConfig.WinGold * actualBoost + settings.GoldCost);
                                        if (actualGold > 0)
                                        {
                                            BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(adoptedHero, actualGold);
                                            results.Add($"+{actualGold} gold");
                                        }

                                        int xp = (int) (BLTAdoptAHeroModule.CommonConfig.WinXP * actualBoost);
                                        if (xp > 0)
                                        {
                                            (bool success, string description) = SkillXP.ImproveSkill(adoptedHero, xp,
                                                Skills.All,
                                                random: false, auto: true);
                                            if (success)
                                            {
                                                results.Add(description);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (BLTAdoptAHeroModule.CommonConfig.LoseGold > 0)
                                        {
                                            BLTAdoptAHeroCampaignBehavior.Get()
                                                .ChangeHeroGold(adoptedHero, -BLTAdoptAHeroModule.CommonConfig.LoseGold);
                                            results.Add($"-{BLTAdoptAHeroModule.CommonConfig.LoseGold} gold");
                                        }

                                        int xp = (int) (BLTAdoptAHeroModule.CommonConfig.LoseXP * actualBoost);
                                        if (xp > 0)
                                        {
                                            (bool success, string description) = SkillXP.ImproveSkill(adoptedHero, xp,
                                                Skills.All,
                                                random: false, auto: true);
                                            if (success)
                                            {
                                                results.Add(description);
                                            }
                                        }
                                    }

                                    if (results.Any())
                                    {
                                        Log.LogFeedResponse(context.UserName, results.ToArray());
                                    }
                                }
                            },
                            // onGotAKill: (killer, killed, state) =>
                            // {
                            //     Log.Trace($"[{nameof(SummonHero)}] {adoptedHero} killed {killed?.ToString() ?? "unknown"}");
                            //     var results = BLTMissionBehavior.ApplyKillEffects(
                            //         adoptedHero, killer, killed, state,
                            //         settings.GoldPerKill,
                            //         settings.HealPerKill,
                            //         settings.XPPerKill,
                            //         actualBoost,
                            //         settings.RelativeLevelScaling,
                            //         settings.LevelScalingCap
                            //     );
                            //     if (results.Any())
                            //     {
                            //         Log.LogFeedResponse(context.UserName, results.ToArray());
                            //     }
                            // },
                            // onGotKilled: (killed, killer, state) =>
                            // {
                            //     Log.Trace($"[{nameof(SummonHero)}] {adoptedHero} was killed by {killer?.ToString() ?? "unknown"}");
                            //     var results = BLTMissionBehavior.ApplyKilledEffects(
                            //         adoptedHero, killer, state,
                            //         settings.XPPerKilled,
                            //         actualBoost,
                            //         settings.RelativeLevelScaling,
                            //         settings.LevelScalingCap
                            //     );
                            //     if (results.Any())
                            //     {
                            //         Log.LogFeedResponse(context.UserName, results.ToArray());
                            //     }
                            // },
                            replaceExisting: false
                        );
                        
                        existingHero = BLTSummonBehavior.Current.AddSummonedHero(adoptedHero, settings.OnPlayerSide, formationClass, party);
                    }
                    
                    // DOING:
                    // - Add general, common, settings for modules, expose on UI:
                    //   - general kill rewards, battle rewards 
                    //   -  maybe just those??
                    // - Add kill reward handlers to all missions via the campaign events

                    var troopOrigin = new PartyAgentOrigin(existingHero.Party, adoptedHero.CharacterObject,
                        alwaysWounded: !BLTAdoptAHeroModule.CommonConfig.AllowDeath);
                    bool isMounted = Mission.Current.Mode != MissionMode.Stealth 
                                     && !MissionHelpers.InSiegeMission() 
                                     && existingHero.Formation is
                                         FormationClass.Cavalry or
                                         FormationClass.LightCavalry or
                                         FormationClass.HeavyCavalry or
                                         FormationClass.HorseArcher
                                     ;

                    // The standard MissionAgentSpawnLogic.SpawnTroops does this for formations also,
                    // however it is difficult to do for us as the class required is a private subclass,
                    // and not accessible except through raw reflection. 
                    // It doesn't seem to be necessary as the required formations appear to be created dynamically
                    // anyway, so it might just be legacy code.
                    // var formation = Mission.GetAgentTeam(troopOrigin, settings.OnPlayerSide).GetFormation(existingHero.Formation);
                    // if (formation != null && !(bool)AccessTools.Field(typeof(Formation), "HasBeenPositioned").GetValue(formation))
                    // {
                    //     formation.BeginSpawn(1, isMounted);
                    //     Mission.Current.SpawnFormation(formation, 1, Mission.Current.Mode != MissionMode.Stealth 
                    //                                                  && !InSiegeMission(), isMounted, true);
                    //     var spawnLogic = Mission.Current.GetMissionBehaviour<MissionAgentSpawnLogic>();
                    //     var _spawnedFormations = (MBList<Formation>) AccessTools
                    //         .Field(typeof(MissionAgentSpawnLogic), "_spawnedFormations").GetValue(spawnLogic);
                    //     _spawnedFormations.Add(formation);
                    // }

                    bool allowRetinue = firstSummon
                                        && !MissionHelpers.InArenaPracticeMission()
                                        && !MissionHelpers.InTournament()
                                        && !MissionHelpers.InFriendlyMission();

                    var retinueTroops = allowRetinue 
                        ? BLTAdoptAHeroCampaignBehavior.Get().GetRetinue(adoptedHero).ToList() 
                        : Enumerable.Empty<CharacterObject>().ToList();

                    int formationTroopIdx = 0;

                    int totalTroopsCount = 1 + retinueTroops.Count;
                    if (settings.OnPlayerSide)
                    {
                        Campaign.Current.SetPlayerFormationPreference(adoptedHero.CharacterObject,
                            existingHero.Formation);
                    }

                    var adoptedAgent = Mission.Current.SpawnTroop(
                        troopOrigin,
                        isPlayerSide: settings.OnPlayerSide,
                        hasFormation: !settings.OnPlayerSide || existingHero.Formation != FormationClass.Bodyguard,
                        spawnWithHorse: adoptedHero.CharacterObject.IsMounted && isMounted,
                        isReinforcement: true,
                        enforceSpawningOnInitialPoint: false,
                        formationTroopCount: totalTroopsCount,
                        formationTroopIndex: formationTroopIdx++,
                        isAlarmed: true,
                        wieldInitialWeapons: true);

                    //SetAgentHealth(adoptedAgent);

                    if (settings.OnPlayerSide && existingHero.Formation == FormationClass.Bodyguard)
                    {
                        var spawnPos = Vec2.Forward * (3 + MBRandom.RandomFloat * 5);
                        spawnPos.RotateCCW(MathF.PI * 2 * MBRandom.RandomFloat);
                        adoptedAgent.SetColumnwiseFollowAgent(Agent.Main, ref spawnPos);
                        // agent.HumanAIComponent.FollowAgent(Agent.Main);
                    }

                    var agent_name = AccessTools.Field(typeof(Agent), "_name");
                    foreach (var retinueTroop in retinueTroops)
                    {
                        // Don't modify formation for non-player side spawn as we don't really care
                        bool hasPrevFormation = Campaign.Current.PlayerFormationPreferences
                                                    .TryGetValue(retinueTroop, out var prevFormation) 
                                                && settings.OnPlayerSide;
                        if (settings.OnPlayerSide)
                        {
                            Campaign.Current.SetPlayerFormationPreference(retinueTroop, existingHero.Formation);
                        }

                        existingHero.Party.MemberRoster.AddToCounts(retinueTroop, 1);
                        var retinueAgent = Mission.Current.SpawnTroop(
                            new PartyAgentOrigin(existingHero.Party, retinueTroop),
                            isPlayerSide: settings.OnPlayerSide,
                            hasFormation: settings.OnPlayerSide || existingHero.Formation != FormationClass.Bodyguard,
                            spawnWithHorse: retinueTroop.IsMounted && isMounted,
                            isReinforcement: true,
                            enforceSpawningOnInitialPoint: false,
                            formationTroopCount: totalTroopsCount,
                            formationTroopIndex: formationTroopIdx++,
                            isAlarmed: true,
                            wieldInitialWeapons: true);
                        
                        agent_name.SetValue(retinueAgent, new TextObject($"{retinueAgent.Name} ({context.UserName})"));
                        
                        retinueAgent.BaseHealthLimit *= Math.Max(1, BLTAdoptAHeroModule.CommonConfig.StartRetinueHealthMultiplier);
                        retinueAgent.HealthLimit *= Math.Max(1, BLTAdoptAHeroModule.CommonConfig.StartRetinueHealthMultiplier);
                        retinueAgent.Health *= Math.Max(1, BLTAdoptAHeroModule.CommonConfig.StartRetinueHealthMultiplier);

                        BLTAdoptAHeroCustomMissionBehavior.Current.AddListeners(retinueAgent,
                            onMissionOver: () =>
                            {
                                if (retinueAgent.State == AgentState.Unconscious)
                                {
                                    existingHero.Party.MemberRoster.AddToCounts(retinueTroop, -1, woundedCount: -1);
                                }
                                else if (retinueAgent.State != AgentState.Killed)
                                {
                                    existingHero.Party.MemberRoster.AddToCounts(retinueTroop, -1);
                                }
                            },
                            onGotAKill: (killer, killed, state) =>
                            {
                                Log.Trace($"[{nameof(SummonHero)}] {retinueAgent.Name} killed {killed?.ToString() ?? "unknown"}");
                                var results = BLTAdoptAHeroCustomMissionBehavior.ApplyKillEffects(
                                    adoptedHero, killer, killed, state,
                                    BLTAdoptAHeroModule.CommonConfig.RetinueGoldPerKill,
                                    BLTAdoptAHeroModule.CommonConfig.RetinueHealPerKill,
                                    0,
                                    actualBoost,
                                    BLTAdoptAHeroModule.CommonConfig.RelativeLevelScaling,
                                    BLTAdoptAHeroModule.CommonConfig.LevelScalingCap
                                );
                                if (results.Any())
                                {
                                    ActionManager.SendReply(context, new[]{ retinueAgent.Name }.Concat(results).ToArray());
                                }
                            }
                        );
                        if (settings.OnPlayerSide && existingHero.Formation == FormationClass.Bodyguard)
                        {
                            var spawnPos = Vec2.Forward * (3 + MBRandom.RandomFloat * 5);
                            spawnPos.RotateCCW(MathF.PI * 2 * MBRandom.RandomFloat);
                            retinueAgent.SetColumnwiseFollowAgent(Agent.Main, ref spawnPos);
                            // agent.HumanAIComponent.FollowAgent(Agent.Main);
                        }

                        if (hasPrevFormation)
                        {
                            Campaign.Current.SetPlayerFormationPreference(retinueTroop, prevFormation);
                        }
                    }

                    // All the units try to occupy the same exact spot if standard body guard is used
                    // if (existingHero.Formation == FormationClass.Bodyguard)
                    // {
                    //     if (settings.OnPlayerSide)
                    //         agent.SetGuardState(Agent.Main, isGuarding: true);
                    //     else if (Mission.Current.PlayerEnemyTeam?.GeneralAgent != null)
                    //         agent.SetGuardState(Mission.Current.PlayerEnemyTeam?.GeneralAgent, isGuarding: true);
                    // }

                    var expireFn = AccessTools.Method(typeof(TeamQuerySystem), "Expire");
                    foreach (var team in Mission.Current.Teams)
                    {
                        expireFn.Invoke(team.QuerySystem, new object[] { }); // .Expire();
                    }
                    foreach (var formation in Mission.Current.Teams.SelectMany(t => t.Formations))
                    {
                        AccessTools.Field(typeof(Formation), "GroupSpawnIndex").SetValue(formation, 0); //formation2.GroupSpawnIndex = 0;
                    }
                    
                    var messages = settings.OnPlayerSide
                        ? FriendlySummonMessages
                        : EnemySummonMessages;

                    Log.ShowInformation(!string.IsNullOrEmpty(context.Args) ? context.Args : messages.SelectRandom(), adoptedHero.CharacterObject, settings.AlertSound);

                    BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(adoptedHero, -settings.GoldCost);

                    onSuccess($"You have joined the battle!");
                });
            }
        }

        // Modified KillAgentCheat (usually Ctrl+F4 in debug mode) that can actually kill sometimes instead of only knock out.
        // For testing...
        private static void KillAgentCheat(Agent agent)
        {
            var blow = new Blow(Mission.Current.MainAgent?.Index ?? agent.Index)
            {
                DamageType = DamageTypes.Pierce,
                BoneIndex = agent.Monster.HeadLookDirectionBoneIndex,
                Position = agent.Position,
                BaseMagnitude = 2000f,
                InflictedDamage = 2000,
                SwingDirection = agent.LookDirection,
                Direction = agent.LookDirection,
                DamageCalculated = true,
                VictimBodyPart = BoneBodyPartType.Head,
            };
            blow.WeaponRecord.FillAsMeleeBlow(Mission.Current.MainAgent?.WieldedWeapon.Item, 
                Mission.Current.MainAgent?.WieldedWeapon.CurrentUsageItem, -1, -1);
            blow.Position.z += agent.GetEyeGlobalHeight();
            agent.RegisterBlow(blow);
        }
        
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(MissionAgentSpawnLogic), nameof(MissionAgentSpawnLogic.IsSideDepleted))]
        // ReSharper disable once RedundantAssignment
        public static void IsSideDepletedPostfix(MissionAgentSpawnLogic __instance, BattleSideEnum side, ref bool __result)
        {
            __result = !__instance.Mission.Teams.Where(t => t.Side == side).Any(t => t.ActiveAgents.Any());
        }
        
                
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(DefaultBattleMoraleModel), nameof(DefaultBattleMoraleModel.GetImportance))]
        public static void GetImportancePostfix(DefaultBattleMoraleModel __instance, Agent agent, ref float __result)
        {
            if (agent.IsAdopted())
            {
                __result *= BLTAdoptAHeroModule.CommonConfig.MoraleLossFactor;
            }
        }
    }
}