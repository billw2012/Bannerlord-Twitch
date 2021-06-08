using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Bannerlord.ButterLib.SaveSystem.Extensions;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.UI;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox.TournamentMissions.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [HarmonyPatch, Description("Puts adopted heroes in queue for the next tournament"), UsedImplicitly]
    internal class JoinTournament : ActionHandlerBase
    {
        [CategoryOrder("General", 1)]
        private class Settings
        {
            [Category("General"), Description("Gold cost to join"), PropertyOrder(4)]
            public int GoldCost { get; [UsedImplicitly] set; }
        }
        
        protected override Type ConfigType => typeof(Settings);
        
        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Settings) config;
            
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

            (bool success, string reply) = BLTTournamentQueueBehavior.Get().AddToQueue(adoptedHero, context.IsSubscriber, settings.GoldCost);
            if (!success)
            {
                onFailure(reply);
            }
            else
            {
                BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(adoptedHero, -settings.GoldCost);
                onSuccess(reply);
            }
        }

        public static void SetupGameMenus(CampaignGameStarter campaignGameSystemStarter)
        {
            campaignGameSystemStarter.AddGameMenuOption(
                "town_arena", "blt_join_tournament", "JOIN the viewer tournament", 
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                    return BLTTournamentQueueBehavior.Get().TournamentAvailable;
                },
                _ =>
                {
                    BLTTournamentQueueBehavior.Get().JoinViewerTournament();
                    GameMenu.SwitchToMenu("town");
                }, 
                index: 2);
            campaignGameSystemStarter.AddGameMenuOption(
                "town_arena", "blt_watch_tournament", "WATCH the viewer tournament", 
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                    return BLTTournamentQueueBehavior.Get().TournamentAvailable;
                },
                _ =>
                {
                    BLTTournamentQueueBehavior.Get().WatchViewerTournament();
                    GameMenu.SwitchToMenu("town");
                }, 
                index: 3);
        }

        // private static ItemObject FindRandomTieredEquipment(int tier, params ItemObject.ItemTypeEnum[] itemTypeEnums)
        // {
        //     var items = ItemObject.All
        //         // Usable
        //         .Where(item => !item.NotMerchandise)
        //         // Correct type
        //         .Where(item => itemTypeEnums.Contains(item.Type))
        //         .ToList();
        //
        //     // Correct tier
        //     var tieredItems = items.Where(item => (int) item.Tier == tier).ToList();
        //
        //     // We might not find an item at the specified tier, so find the closest tier we can
        //     while (!tieredItems.Any() && tier >= 0)
        //     {
        //         tier--;
        //         tieredItems = items.Where(item => (int) item.Tier == tier).ToList();
        //     }
        //
        //     return tieredItems.SelectRandom();
        // }

        // MissionState.Current.CurrentMission doesn't have any behaviours added during this function, so we split the initialization that requires access
        // to mission behaviours into another patch below
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentGame), nameof(TournamentGame.GetParticipantCharacters))]
        public static void GetParticipantCharactersPostfix(Settlement settlement,
            int maxParticipantCount, bool includePlayer, List<CharacterObject> __result)
        {
            BLTTournamentQueueBehavior.Get().GetParticipantCharacters(settlement, __result);
        }

        // After PrepareForTournamentGame the MissionState.Current.CurrentMission contains the behaviors
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentGame), nameof(TournamentGame.PrepareForTournamentGame))]
        public static void PrepareForTournamentGamePostfix(TournamentGame __instance, bool isPlayerParticipating)
        {
            BLTTournamentQueueBehavior.Get().PrepareForTournamentGame();
        }

        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentBehavior), "EndCurrentMatch")]
        public static void EndCurrentMatchPrefix(TournamentBehavior __instance)
        {
            BLTTournamentQueueBehavior.Get().EndCurrentMatch(__instance);
        }

        private class BLTTournamentQueueBehavior : CampaignBehaviorBase, IDisposable
        {
            public static BLTTournamentQueueBehavior Get() => GetCampaignBehavior<BLTTournamentQueueBehavior>();
            
            private TournamentQueuePanel tournamentQueuePanel;

            public BLTTournamentQueueBehavior()
            {
                Log.AddInfoPanel(construct: () =>
                {
                    tournamentQueuePanel = new TournamentQueuePanel();
                    return tournamentQueuePanel;
                });
            }

            public override void RegisterEvents()
            {
                CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, (_, _, _, _) =>
                {
                    tournamentQueue.RemoveAll(e => e.Hero == null || e.Hero.IsDead);
                });
            }

            public override void SyncData(IDataStore dataStore)
            {
                dataStore.SyncDataAsJson("Queue", ref tournamentQueue);
                tournamentQueue ??= new();
                tournamentQueue.RemoveAll(e => e.Hero == null || e.Hero.IsDead);
                UpdatePanel();
            }

            private void UpdatePanel()
            {
                int queueLength = tournamentQueue.Count;
                Log.RunInfoPanelUpdate(() =>
                {
                    tournamentQueuePanel.UpdateTournamentQueue(queueLength);
                });
            }

            private class TournamentQueueEntry
            {
                [SaveableProperty(0)]
                public Hero Hero { get; set; }
                [SaveableProperty(1)]
                public bool IsSub { get; set; }
                [SaveableProperty(2)]
                public int EntryFee { get; set; }

                public TournamentQueueEntry(Hero hero = null, bool isSub = false, int entryFee = 0)
                {
                    Hero = hero;
                    IsSub = isSub;
                    EntryFee = entryFee;
                }
            }
            
            private List<TournamentQueueEntry> tournamentQueue = new();
            private readonly List<TournamentQueueEntry> activeTournament = new();

            private enum TournamentMode
            {
                None,
                Watch,
                Join
            }
            private TournamentMode mode = TournamentMode.None;

            public bool TournamentAvailable => tournamentQueue.Any();
            
            public (bool success, string reply) AddToQueue(Hero hero, bool isSub, int entryFree)
            {
                if (tournamentQueue.Any(sh => sh.Hero == hero))
                {
                    return (false, $"You are already in the tournament queue!");
                }

                tournamentQueue.Add(new TournamentQueueEntry(hero, isSub, entryFree));
                UpdatePanel();
                return (true, $"You are position {tournamentQueue.Count} in the tournament queue!");
            }
            
            public void JoinViewerTournament()
            {
                mode = TournamentMode.Join;
                var tournamentGame = Campaign.Current.Models.TournamentModel.CreateTournament(Settlement.CurrentSettlement.Town);
                tournamentGame.PrepareForTournamentGame(true);
            }
            
            public void WatchViewerTournament()
            {
                mode = TournamentMode.Watch;
                var tournamentGame = Campaign.Current.Models.TournamentModel.CreateTournament(Settlement.CurrentSettlement.Town);
                tournamentGame.PrepareForTournamentGame(false);
            }
            
            public void GetParticipantCharacters(Settlement settlement, List<CharacterObject> __result)
            {
                activeTournament.Clear();

                if (Settlement.CurrentSettlement == settlement && mode != TournamentMode.None)
                {
                    __result.Remove(Hero.MainHero.CharacterObject);
                    
                    int viewersToAddCount = Math.Min(__result.Count, tournamentQueue.Count);
                    __result.RemoveRange(0, viewersToAddCount);
                    if(mode == TournamentMode.Join)
                        __result.Add(Hero.MainHero.CharacterObject);
                    
                    var viewersToAdd = tournamentQueue.Take(viewersToAddCount).ToList();
                    __result.AddRange(viewersToAdd.Select(q => q.Hero.CharacterObject));
                    activeTournament.AddRange(viewersToAdd);
                    tournamentQueue.RemoveRange(0, viewersToAddCount);
                    UpdatePanel();

                    mode = TournamentMode.None;
                }
            }
            
            public void PrepareForTournamentGame()
            {
                static (bool used, string failReason) UpgradeToItem(Hero hero, ItemObject item)
                {
                    if (EquipHero.CanUseItem(item, hero))
                    {
                        // Find a slot
                        var slot = hero.BattleEquipment
                            .YieldEquipmentSlots()
                            .Cast<(EquipmentElement element, EquipmentIndex index)?>()
                            .FirstOrDefault(e
                                => e.HasValue && Equipment.IsItemFitsToSlot(e.Value.index, item)
                                              && (e.Value.element.IsEmpty || e.Value.element.Item.Type == item.Type &&
                                                  e.Value.element.Item.Tierf <= item.Tierf));
                        if (slot.HasValue)
                        {
                            hero.BattleEquipment[slot.Value.index] = new EquipmentElement(item);
                            return (true, null);
                        }
                        else
                        {
                            return (false, "your existing equipment is better");
                        }
                    }
                    else
                    {
                        return (false, "you can't use this item");
                    }
                }

                if (activeTournament.Any())
                {
                    var tournamentBehaviour = MissionState.Current.CurrentMission.GetMissionBehaviour<TournamentBehavior>();

                    tournamentBehaviour.TournamentEnd += () =>
                    {
                        // Win results
                        foreach (var entry in activeTournament)
                        {
                            float actualBoost = entry.IsSub ? Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1) : 1;
                            var results = new List<string>();
                            if (entry.Hero == tournamentBehaviour.Winner.Character?.HeroObject)
                            {
                                results.Add("WINNER!");

                                // Winner gets their gold back also
                                int actualGold = (int) (BLTAdoptAHeroModule.TournamentConfig.WinGold * actualBoost + entry.EntryFee);
                                if (actualGold > 0)
                                {
                                    BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(entry.Hero, actualGold);
                                    results.Add($"+{actualGold} gold");
                                }

                                int xp = (int) (BLTAdoptAHeroModule.TournamentConfig.WinXP * actualBoost);
                                if (xp > 0)
                                {
                                    (bool success, string description) = SkillXP.ImproveSkill(entry.Hero, xp, Skills.All, auto: true);
                                    if (success)
                                    {
                                        results.Add(description);
                                    }
                                }

                                var prize = tournamentBehaviour.TournamentGame.Prize;
                                (bool upgraded, string failReason) = UpgradeToItem(entry.Hero, prize);
                                if (!upgraded)
                                {
                                    BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(entry.Hero, prize.Value);
                                    results.Add($"sold {prize.Name} for {prize.Value} gold ({failReason})");
                                }
                                else
                                {
                                    results.Add($"received {prize.Name}");
                                }
                            }
                            else
                            {
                                int xp = (int) (BLTAdoptAHeroModule.TournamentConfig.ParticipateXP * actualBoost);
                                if (xp > 0)
                                {
                                    (bool success, string description) =
                                        SkillXP.ImproveSkill(entry.Hero, xp, Skills.All, auto: true);
                                    if (success)
                                    {
                                        results.Add(description);
                                    }
                                }
                            }

                            if (results.Any())
                            {
                                Log.LogFeedResponse(entry.Hero.FirstName.ToString(), results.ToArray());
                            }
                        }

                        activeTournament.Clear(); // = false;
                    };

                    // var BLTAdoptAHeroModule.CommonConfig = BLTAdoptAHeroModule.GetGlobalConfig();
                    
                    // foreach (var (context, settings, hero) in activeTournament)
                    // {
                    //     float actualBoost = SettingsSubBoost(context, settings);
                    //
                    //     // Kill effects
                    //     BLTMissionBehavior.Current.AddListeners(hero,
                    //         onAgentCreated: agent =>
                    //         {
                    //             if (settings.StartWithFullHP)
                    //             {
                    //                 agent.Health = agent.HealthLimit;
                    //             }
                    //
                    //             if (settings.StartHPMultiplier.HasValue)
                    //             {
                    //                 agent.BaseHealthLimit *= settings.StartHPMultiplier.Value;
                    //                 agent.HealthLimit *= settings.StartHPMultiplier.Value;
                    //                 agent.Health *= settings.StartHPMultiplier.Value;
                    //             }
                    //         },
                    //         onGotAKill: (killer, killed, state) =>
                    //         {
                    //             var results = BLTMissionBehavior.ApplyKillEffects(
                    //                 hero, killer, killed, state,
                    //                 settings.GoldPerKill,
                    //                 settings.HealPerKill,
                    //                 settings.XPPerKill,
                    //                 actualBoost,
                    //                 settings.RelativeLevelScaling,
                    //                 settings.LevelScalingCap
                    //             );
                    //
                    //             if (results.Any())
                    //             {
                    //                 ActionManager.SendReply(context, results.ToArray());
                    //             }
                    //         },
                    //         onGotKilled: (_, killer, state) =>
                    //         {
                    //             var results = BLTMissionBehavior.ApplyKilledEffects(
                    //                 hero, killer, state,
                    //                 settings.XPPerKilled,
                    //                 actualBoost,
                    //                 settings.RelativeLevelScaling,
                    //                 settings.LevelScalingCap
                    //             );
                    //
                    //             if (results.Any())
                    //             {
                    //                 ActionManager.SendReply(context, results.ToArray());
                    //             }
                    //         }
                    //     );
                    // }
                }
            }
            
            public void EndCurrentMatch(TournamentBehavior tournamentBehavior)
            {
                // If the tournament is over
                if (tournamentBehavior.CurrentRoundIndex == 4 || tournamentBehavior.LastMatch == null)
                    return;

                // End round effects (as there is no event handler for it :/)
                foreach (var entry in activeTournament)
                {
                    float actualBoost = entry.IsSub ? Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1) : 1;
                    
                    var results = new List<string>();

                    if(tournamentBehavior.LastMatch.Winners.Any(w => w.Character?.HeroObject == entry.Hero))
                    {
                        int actualGold = (int) (BLTAdoptAHeroModule.TournamentConfig.WinMatchGold * actualBoost);
                        if (actualGold > 0)
                        {
                            BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(entry.Hero, actualGold);
                            results.Add($"+{actualGold} gold");
                        }
                        int xp = (int) (BLTAdoptAHeroModule.TournamentConfig.WinMatchXP * actualBoost);
                        if (xp > 0)
                        {
                            (bool success, string description) =
                                SkillXP.ImproveSkill(entry.Hero, xp, Skills.All, auto: true);
                            if (success)
                            {
                                results.Add(description);
                            }
                        }
                    }
                    else if (tournamentBehavior.LastMatch.Participants.Any(w => w.Character?.HeroObject == entry.Hero))
                    {
                        int xp = (int) (BLTAdoptAHeroModule.TournamentConfig.ParticipateMatchXP * actualBoost);
                        if (xp > 0)
                        {
                            (bool success, string description) =
                                SkillXP.ImproveSkill(entry.Hero, xp, Skills.All, auto: true);
                            if (success)
                            {
                                results.Add(description);
                            }
                        }
                    }
                    if (results.Any())
                    {
                        Log.LogFeedResponse(entry.Hero.FirstName.ToString(), results.ToArray());
                    }
                }
            }

            private void ReleaseUnmanagedResources()
            {
                Log.RemoveInfoPanel(tournamentQueuePanel);
            }

            public void Dispose()
            {
                ReleaseUnmanagedResources();
                GC.SuppressFinalize(this);
            }

            ~BLTTournamentQueueBehavior()
            {
                ReleaseUnmanagedResources();
            }
        }
        
        public static void AddBehaviors(CampaignGameStarter campaignStarter)
        {
            campaignStarter.AddBehavior(new BLTTournamentQueueBehavior());
        }

        public static void OnGameEnd(Campaign campaign)
        {
            campaign.GetCampaignBehavior<BLTTournamentQueueBehavior>()?.Dispose();
        }
    }

    #if false
    internal class BetOnTournamentMatch : ActionHandlerBase
    {
        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            JoinTournament.PlaceBet(context, config, onSuccess, onFailure);
        }
    }
    #endif
}
