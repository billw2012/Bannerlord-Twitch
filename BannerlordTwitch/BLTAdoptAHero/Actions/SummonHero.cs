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
    internal class SummonHero : HeroActionHandlerBase
    {
        public class FormationItemSource : IItemsSource
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
                    // "Bodyguard",
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
            [Category("General"), YamlIgnore, ReadOnly(true), PropertyOrder(-100),
             Editor(typeof(MultilineTextNonEditable), typeof(MultilineTextNonEditable)), UsedImplicitly]
            public string Help => "This allows viewers to spawn their adopted heroes into your missions/battles. It supports a custom shout: if used as a command then anything the viewer puts after the command will be shouted in game by the player, if used as a reward then just enable the 'IsUserInputRequired' and set the 'Prompt' to something like 'My shout'.";
            [Category("Allowed Missions"), Description("Can summon for normal field battles between parties"), PropertyOrder(1)]
            public bool AllowFieldBattle { get; [UsedImplicitly] set; }
            [Category("Allowed Missions"), Description("Can summon in village battles"), PropertyOrder(2)]
            public bool AllowVillageBattle { get; [UsedImplicitly] set; }
            [Category("Allowed Missions"), Description("Can summon in sieges"), PropertyOrder(3)]
            public bool AllowSiegeBattle { get; [UsedImplicitly] set; }
            [Category("Allowed Missions"), Description("This includes walking about village/town/dungeon/keep"), PropertyOrder(4)]
            public bool AllowFriendlyMission { get; [UsedImplicitly] set; }
            [Category("Allowed Missions"), Description("Can summon in the hideout missions"), PropertyOrder(7)]
            public bool AllowHideOut { get; [UsedImplicitly] set; }
            [Category("General"), Description("Whether the hero is on the player or enemy side"), PropertyOrder(1)]
            public bool OnPlayerSide { get; [UsedImplicitly] set; }
            // [Category("General"), Description("Maximum number of summons that can be active at the same time (i.e. max alive adopted heroes that can be in the mission) NOT IMPLEMENTED YET"), PropertyOrder(2)]
            // public int? MaxSimultaneousSummons { get; set; }
            
            [Category("General"), Description("Gold cost to summon"), PropertyOrder(5)]
            public int GoldCost { get; [UsedImplicitly] set; }

            [Category("General"), Description("Which formation to add summoned heroes to (only applies to ones " +
                                              "without a specified class)"), 
             PropertyOrder(6), ItemsSource(typeof(FormationItemSource))]
            public string PreferredFormation { get; [UsedImplicitly] set; }

            [Category("General")]
            public bool RetinueUseHeroesFormation { get; [UsedImplicitly] set; }

            [Category("General"), Description("Sound to play when summoned"), PropertyOrder(7)]
            public Log.Sound AlertSound { get; [UsedImplicitly] set; }

            [Category("Effects"), Description("Multiplier applied to (positive) effects for subscribers"), PropertyOrder(1)]
            public float SubBoost { get; [UsedImplicitly] set; }
            
            [Category("Effects"), Description("HP the hero gets every second they are alive in the mission"), PropertyOrder(2)]
            public float HealPerSecond { get; [UsedImplicitly] set; }
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

        public class Shout
        {
            [PropertyOrder(1)]
            public string Text { get; [UsedImplicitly] set; } 
            [PropertyOrder(2), Description("Higher weight means more chance this shout is used")]
            public float Weight { get; [UsedImplicitly] set; } = 1f;
            [PropertyOrder(3), Description("Can be used when summoning on player side")]
            public bool PlayerSide { get; [UsedImplicitly] set; } = true;
            [PropertyOrder(4), Description("Can be used when summoning on enemy side")]
            public bool EnemySide { get; [UsedImplicitly] set; } = true;
            [PropertyOrder(5), Description("Can be used when in a field battle")]
            public bool FieldBattle { get; [UsedImplicitly] set; } = true;
            [PropertyOrder(6), Description("Can be used when on siege defender side")]
            public bool SiegeDefend { get; [UsedImplicitly] set; } = true;
            [PropertyOrder(7), Description("Can be used when on siege attacker side")]
            public bool SiegeAttack { get; [UsedImplicitly] set; } = true;

            public Shout() { }
            public Shout(string text)
            {
                Text = text;
            }

            public override string ToString() => Text;
        }
        
        private static readonly List<Shout> DefaultShouts = new()
        {
            new Shout("Don't worry, I've got your back!") { EnemySide = false },
            new Shout("I'm here!") { EnemySide = false },
            new Shout("Which one should I stab?") { EnemySide = false },
            new Shout("Once more unto the breach!") { EnemySide = false },
            new Shout("Freeeeeedddooooooommmm!") { EnemySide = false },
            new Shout("Remember the Alamo!") { EnemySide = false },
            new Shout("Alala!") { EnemySide = false },
            new Shout("Eleleu!") { EnemySide = false },
            new Shout("Deus vult!") { EnemySide = false },
            new Shout("Banzai!") { EnemySide = false },
            new Shout("Liberty or Death!") { EnemySide = false },
            new Shout("Har Har Mahadev!") { EnemySide = false },
            new Shout("Desperta ferro!") { EnemySide = false },
            new Shout("Alba gu bràth!") { EnemySide = false },
            new Shout("Santiago!") { EnemySide = false },
            new Shout("Huzzah!") { EnemySide = false },
            new Shout("War... war never changes...") { EnemySide = false },
            new Shout("Need a hand?") { EnemySide = false },
            new Shout("May we live to see the next sunrise!") { EnemySide = false },
            new Shout("For glory, charge!") { EnemySide = false },
            new Shout("The price has been paid. I am at your service.") { EnemySide = false },
            new Shout("Give them nothing, but take from them everything!") { EnemySide = false },
            new Shout("Those are brave men knocking at our door, let's go kill them!") { EnemySide = false, SiegeAttack = false, FieldBattle = false },
            new Shout("Lets take this city!") { EnemySide = false, SiegeDefend = false, FieldBattle = false },
            new Shout("Now for wrath, now for ruin and a red nightfall!") { EnemySide = false, Weight = 0.05f },
            new Shout("Fell deeds awake, fire and slaughter!") { EnemySide = false },
            new Shout("Spooooooooooooooooooon!") { EnemySide = false, Weight = 0.05f },
            new Shout("Leeeeeeeerooooy Jeeeeenkins") { EnemySide = false, Weight = 0.05f },

            new Shout("Defend yourself!") { PlayerSide = false },
            new Shout("Time for you to die!") { PlayerSide = false },
            new Shout("You killed my father, prepare to die!") { PlayerSide = false },
            new Shout("En garde!") { PlayerSide = false },
            new Shout("It's stabbing time! For you.") { PlayerSide = false },
            new Shout("It's nothing personal!") { PlayerSide = false },
            new Shout("Curse my sudden but inevitable betrayal!") { PlayerSide = false },
            new Shout("I just don't like you!") { PlayerSide = false },
            new Shout("I'm gonna put some dirt in your eye!") { PlayerSide = false },
            new Shout("I'll mount your head on a pike!") { PlayerSide = false },
            new Shout("Don't hate me, it's just business...") { PlayerSide = false },
            new Shout("Never should have come here!") { PlayerSide = false },
            new Shout("Your money or life!") { PlayerSide = false },
            new Shout("I'm sorry, but I must stop you.") { PlayerSide = false },
            new Shout("I have the high ground!") { PlayerSide = false, Weight = 0.05f },
        };

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

        internal class BLTSummonBehavior : AutoMissionBehavior<BLTSummonBehavior>
        {
            public class RetinueState
            {
                public CharacterObject Troop;
                public Agent Agent;
                // We must record this separately, as the Agent.State is undefined once the Agent is deleted (the internal handle gets reused by the engine)
                public AgentState State;
            }
            
            public class SummonedHero
            {
                public Hero Hero;
                public bool WasPlayerSide;
                public FormationClass Formation;
                public PartyBase Party;
                public AgentState State;
                public Agent CurrentAgent;
                public List<RetinueState> Retinue = new();
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
            
            public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
            {
                var hero = summonedHeroes.FirstOrDefault(h => h.CurrentAgent == affectedAgent);
                if (hero != null)
                {
                    hero.State = agentState;
                }

                // Set the final retinue state
                var retinue = summonedHeroes.SelectMany(h => h.Retinue).FirstOrDefault(r => r.Agent == affectedAgent);
                if (retinue != null)
                {
                    retinue.State = agentState;
                }
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

            protected override void OnEndMission()
            {
                // Remove still living retinue troops from their parties
                foreach(var h in summonedHeroes)
                {
                    foreach (var r in h.Retinue.Where(r => r.State != AgentState.Killed))
                    {
                        h.Party.MemberRoster.AddToCounts(r.Troop, -1);
                    }
                }
            }
        }
        
        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            var settings = (Settings) config;
            int availableGold = BLTAdoptAHeroCampaignBehavior.Get().GetHeroGold(adoptedHero);
            if (availableGold < settings.GoldCost)
            {
                onFailure($"You do not have enough gold: you need {settings.GoldCost}, and you only have {availableGold}!");
                return;
            }
            
            // SpawnAgent (as called by this function) crashes if called in MissionMode.Deployment (would be nice to make it work though)
            if (Mission.Current == null 
                || Mission.Current.Mode is MissionMode.Barter or MissionMode.Conversation or MissionMode.Deployment or
                    MissionMode.Duel or MissionMode.Replay or MissionMode.CutScene)
            {
                onFailure($"You cannot be summoned now!");
                return;
            }

            if(MissionHelpers.InArenaPracticeMission() 
               || MissionHelpers.InTournament()
               || MissionHelpers.InFieldBattleMission() && !settings.AllowFieldBattle
               || MissionHelpers.InVillageEncounter() && !settings.AllowVillageBattle
               || MissionHelpers.InSiegeMission() && !settings.AllowSiegeBattle
               || MissionHelpers.InFriendlyMission() && !settings.AllowFriendlyMission
               || MissionHelpers.InHideOutMission() && (!settings.AllowHideOut || !settings.OnPlayerSide)
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

            bool onAttackingSide = settings.OnPlayerSide
                ? Mission.Current.AttackerTeam.IsFriendOf(Mission.Current.PlayerTeam)
                : !Mission.Current.AttackerTeam.IsFriendOf(Mission.Current.PlayerTeam)
                ;
            bool doingSiegeAttack = MissionHelpers.InSiegeMission() && onAttackingSide;
            bool doingSiegeDefend = MissionHelpers.InSiegeMission() && !onAttackingSide;
            var messages = (BLTAdoptAHeroModule.CommonConfig.IncludeDefaultShouts
                    ? DefaultShouts
                    : Enumerable.Empty<Shout>())
                .Concat(BLTAdoptAHeroModule.CommonConfig.Shouts)
                .Where(s =>
                    (s.EnemySide && !settings.OnPlayerSide || s.PlayerSide && settings.OnPlayerSide)
                    && (s.FieldBattle || !MissionHelpers.InFieldBattleMission())
                    && (s.SiegeAttack || !doingSiegeAttack)
                    && (s.SiegeDefend || !doingSiegeDefend)
                );
                
            // settings.OnPlayerSide
            //     ? FriendlySummonMessages
            //     : EnemySummonMessages;

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
                
                // For player hostile situations we setup a 1v1 fight
                if (!settings.OnPlayerSide)
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
                        });
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

                Log.ShowInformation(!string.IsNullOrEmpty(context.Args) 
                        ? context.Args 
                        : messages.SelectWeighted(MBRandom.RandomFloat, shout => shout.Weight)?.Text ?? "...",
                    adoptedHero.CharacterObject, settings.AlertSound);

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
                        && existingHero.State == AgentState.Killed)
                    {
                        onFailure($"You cannot be summoned, you DIED!");
                        return;
                    }
                    
                    // Check again, as tick is delayed...
                    if (existingHero is {State: AgentState.Active})
                    {
                        onFailure($"You cannot be summoned, you are already here!");
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

                        var party = settings.OnPlayerSide switch
                        {
                            true when Mission.Current?.PlayerTeam != null &&
                                      Mission.Current?.PlayerTeam?.ActiveAgents.Any() == true => PartyBase.MainParty,
                            false when Mission.Current?.PlayerEnemyTeam != null &&
                                       Mission.Current?.PlayerEnemyTeam.ActiveAgents.Any() == true => Mission.Current
                                .PlayerEnemyTeam?.TeamAgents?.Select(a => a.Origin?.BattleCombatant as PartyBase)
                                .Where(p => p != null)
                                .SelectRandom(),
                            _ => null
                        };

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

                        var heroClass = BLTAdoptAHeroCampaignBehavior.Get().GetClass(adoptedHero);

                        // We don't support Unset, or General formations, and implement custom behaviour for Bodyguard
                        if (!Enum.TryParse(heroClass?.Formation ?? settings.PreferredFormation, out FormationClass formationClass)
                                 || formationClass >= FormationClass.NumberOfRegularFormations)
                        {
                            formationClass = FormationClass.Infantry;
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
                                                Skills.All, auto: true);
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
                                                Skills.All, auto: true);
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
                            replaceExisting: false
                        );
                        
                        existingHero = BLTSummonBehavior.Current.AddSummonedHero(adoptedHero, settings.OnPlayerSide, formationClass, party);
                    }
                    
                    // DOING:
                    // - Add general, common, settings for modules, expose on UI:
                    //   - general kill rewards, battle rewards 
                    //   -  maybe just those??
                    // - Add kill reward handlers to all missions via the campaign events

                    var troopOrigin = new PartyAgentOrigin(existingHero.Party, adoptedHero.CharacterObject);
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

                    existingHero.CurrentAgent = Mission.Current.SpawnTroop(
                        troopOrigin,
                        isPlayerSide: settings.OnPlayerSide,
                        hasFormation: true,//!settings.OnPlayerSide || existingHero.Formation != FormationClass.Bodyguard,
                        spawnWithHorse: adoptedHero.CharacterObject.IsMounted && isMounted,
                        isReinforcement: true,
                        enforceSpawningOnInitialPoint: false,
                        formationTroopCount: totalTroopsCount,
                        formationTroopIndex: formationTroopIdx++,
                        isAlarmed: true,
                        wieldInitialWeapons: true);

                    existingHero.State = AgentState.Active;

                    // if (settings.OnPlayerSide && existingHero.Formation == FormationClass.Bodyguard)
                    // {
                    //     var spawnPos = Vec2.Forward * (3 + MBRandom.RandomFloat * 5);
                    //     spawnPos.RotateCCW(MathF.PI * 2 * MBRandom.RandomFloat);
                    //     existingHero.CurrentAgent.SetColumnwiseFollowAgent(Agent.Main, ref spawnPos);
                    //     // agent.HumanAIComponent.FollowAgent(Agent.Main);
                    // }

                    if (allowRetinue)
                    {
                        var agent_name = AccessTools.Field(typeof(Agent), "_name");
                        foreach (var retinueTroop in retinueTroops)
                        {
                            // Don't modify formation for non-player side spawn as we don't really care
                            bool hasPrevFormation = Campaign.Current.PlayerFormationPreferences
                                                        .TryGetValue(retinueTroop, out var prevFormation)
                                                    && settings.OnPlayerSide
                                                    && settings.RetinueUseHeroesFormation;

                            if (settings.OnPlayerSide && settings.RetinueUseHeroesFormation)
                            {
                                Campaign.Current.SetPlayerFormationPreference(retinueTroop, existingHero.Formation);
                            }

                            existingHero.Party.MemberRoster.AddToCounts(retinueTroop, 1);
                            var retinueAgent = Mission.Current.SpawnTroop(
                                new PartyAgentOrigin(existingHero.Party, retinueTroop),
                                isPlayerSide: settings.OnPlayerSide,
                                hasFormation: true, //!settings.OnPlayerSide ||
                                              //existingHero.Formation != FormationClass.Bodyguard,
                                spawnWithHorse: retinueTroop.IsMounted && isMounted,
                                isReinforcement: true,
                                enforceSpawningOnInitialPoint: false,
                                formationTroopCount: totalTroopsCount,
                                formationTroopIndex: formationTroopIdx++,
                                isAlarmed: true,
                                wieldInitialWeapons: true);

                            existingHero.Retinue.Add(new BLTSummonBehavior.RetinueState
                            {
                                Troop = retinueTroop,
                                Agent = retinueAgent,
                                State = AgentState.Active,
                            });

                            agent_name.SetValue(retinueAgent,
                                new TextObject($"{retinueAgent.Name} ({context.UserName})"));

                            retinueAgent.BaseHealthLimit *= Math.Max(1,
                                BLTAdoptAHeroModule.CommonConfig.StartRetinueHealthMultiplier);
                            retinueAgent.HealthLimit *= Math.Max(1,
                                BLTAdoptAHeroModule.CommonConfig.StartRetinueHealthMultiplier);
                            retinueAgent.Health *= Math.Max(1,
                                BLTAdoptAHeroModule.CommonConfig.StartRetinueHealthMultiplier);

                            BLTAdoptAHeroCustomMissionBehavior.Current.AddListeners(retinueAgent,
                                onGotAKill: (killer, killed, state) =>
                                {
                                    Log.Trace(
                                        $"[{nameof(SummonHero)}] {retinueAgent.Name} killed {killed?.ToString() ?? "unknown"}");
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
                                        ActionManager.SendReply(context,
                                            new[] {retinueAgent.Name}.Concat(results).ToArray());
                                    }
                                }
                            );
                            // if (settings.OnPlayerSide && existingHero.Formation == FormationClass.Bodyguard)
                            // {
                            //     var spawnPos = Vec2.Forward * (3 + MBRandom.RandomFloat * 5);
                            //     spawnPos.RotateCCW(MathF.PI * 2 * MBRandom.RandomFloat);
                            //     retinueAgent.SetColumnwiseFollowAgent(Agent.Main, ref spawnPos);
                            //     // agent.HumanAIComponent.FollowAgent(Agent.Main);
                            // }

                            if (hasPrevFormation)
                            {
                                Campaign.Current.SetPlayerFormationPreference(retinueTroop, prevFormation);
                            }
                        }

                        BLTAdoptAHeroCommonMissionBehavior.Current.RegisterRetinue(adoptedHero, existingHero.Retinue.Select(r => r.Agent).ToList());
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
                    
                    Log.ShowInformation(!string.IsNullOrEmpty(context.Args) 
                        ? context.Args 
                        : (messages.SelectWeighted(MBRandom.RandomFloat, shout => shout.Weight)?.Text ?? "..."),
                        adoptedHero.CharacterObject, settings.AlertSound);

                    BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(adoptedHero, -settings.GoldCost);

                    onSuccess($"You have joined the battle!");
                });
            }
        }

        // Modified KillAgentCheat (usually Ctrl+F4 in debug mode) that can actually kill sometimes instead of only knock out.
        // For testing...
        // ReSharper disable once UnusedMember.Local
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