using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Bannerlord.ButterLib.SaveSystem.Extensions;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Actions.Util;
using BLTAdoptAHero.UI;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox.TournamentMissions.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TaleWorlds.TwoDimension;
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
            
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(AdoptAHero.NoHeroMessage);
                return;
            }
            
            int availableGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(adoptedHero);
            if (availableGold < settings.GoldCost)
            {
                onFailure(Naming.NotEnoughGold(settings.GoldCost, availableGold));
                return;
            }

            (bool success, string reply) = BLTTournamentQueueBehavior.Get().AddToQueue(adoptedHero, context.IsSubscriber, settings.GoldCost);
            if (!success)
            {
                onFailure(reply);
            }
            else
            {
                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(adoptedHero, -settings.GoldCost);
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
                if (dataStore.IsSaving)
                {
                    var usedHeroList = tournamentQueue.Select(t => t.Hero).ToList();
                    dataStore.SyncData("UsedHeroObjectList", ref usedHeroList);
                    var queue = tournamentQueue.Select(e => new TournamentQueueEntrySavable
                    {
                        HeroIndex = usedHeroList.IndexOf(e.Hero),
                        IsSub = e.IsSub,
                        EntryFee = e.EntryFee,
                    }).ToList();
                    dataStore.SyncDataAsJson("Queue2", ref queue);
                }
                else
                {
                    List<Hero> usedHeroList = null;
                    dataStore.SyncData("UsedHeroObjectList", ref usedHeroList);
                    List<TournamentQueueEntrySavable> queue = null;
                    dataStore.SyncDataAsJson("Queue2", ref queue);
                    if (usedHeroList != null && queue != null)
                    {
                        tournamentQueue = queue.Select(e => new TournamentQueueEntry
                        {
                            Hero = usedHeroList[e.HeroIndex],
                            IsSub = e.IsSub,
                            EntryFee = e.EntryFee,
                        }).ToList();
                    }
                }
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
                public Hero Hero { get; set; }
                public bool IsSub { get; set; }
                public int EntryFee { get; set; }

                public TournamentQueueEntry(Hero hero = null, bool isSub = false, int entryFee = 0)
                {
                    Hero = hero;
                    IsSub = isSub;
                    EntryFee = entryFee;
                }
            }

            private class TournamentQueueEntrySavable
            {
                [SaveableProperty(0)]
                public int HeroIndex { get; set; }
                [SaveableProperty(1)]
                public bool IsSub { get; set; }
                [SaveableProperty(2)]
                public int EntryFee { get; set; }
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
                SetPlaceholderPrize(tournamentGame);
                tournamentGame.PrepareForTournamentGame(true);
            }

            public void WatchViewerTournament()
            {
                mode = TournamentMode.Watch;
                var tournamentGame = Campaign.Current.Models.TournamentModel.CreateTournament(Settlement.CurrentSettlement.Town);
                SetPlaceholderPrize(tournamentGame);
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
                                    BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(entry.Hero, actualGold);
                                    results.Add($"{Naming.Inc}{actualGold}{Naming.Gold}");
                                }

                                int xp = (int) (BLTAdoptAHeroModule.TournamentConfig.WinXP * actualBoost);
                                if (xp > 0)
                                {
                                    (bool success, string description) = SkillXP.ImproveSkill(entry.Hero, xp, SkillsEnum.All, auto: true);
                                    if (success)
                                    {
                                        results.Add(description);
                                    }
                                }

                                var (item, itemModifier) = BLTAdoptAHeroModule.TournamentConfig.UseCustomPrizes 
                                    ? GeneratePrize(entry.Hero)
                                    : (tournamentBehaviour.TournamentGame.Prize, null);
                                if (item == null)
                                {
                                    results.Add($"no prize available for you!");
                                }
                                else
                                {
                                    var element = new EquipmentElement(item, itemModifier);
                                    (bool upgraded, string failReason) = UpgradeToItem(entry.Hero, element, itemModifier != null);
                                    if (!upgraded)
                                    {
                                        BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(entry.Hero,
                                            item.Value * 3);
                                        results.Add($"sold {element.GetModifiedItemName()} for {item.Value}{Naming.Gold} ({failReason})");
                                    }
                                    else
                                    {
                                        results.Add($"received {element.GetModifiedItemName()}");
                                    }
                                }
                            }
                            else
                            {
                                int xp = (int) (BLTAdoptAHeroModule.TournamentConfig.ParticipateXP * actualBoost);
                                if (xp > 0)
                                {
                                    (bool success, string description) =
                                        SkillXP.ImproveSkill(entry.Hero, xp, SkillsEnum.All, auto: true);
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

            private static (bool used, string failReason) UpgradeToItem(Hero hero, EquipmentElement element,  bool force)
            {
                if (EquipHero.CanUseItem(element.Item, hero, force))
                {
                    // Find a slot
                    var slot = hero.BattleEquipment.YieldEquipmentSlots()
                        .Cast<(EquipmentElement element, EquipmentIndex index)?>()
                        .FirstOrDefault(e => 
                            e.HasValue 
                            && Equipment.IsItemFitsToSlot(e.Value.index, element.Item) 
                            && (e.Value.element.IsEmpty || e.Value.element.Item.Type == element.Item.Type 
                                && (force || e.Value.element.Item.Tierf <= element.Item.Tierf)
                                )
                            );
                    if (slot.HasValue)
                    {
                        hero.BattleEquipment[slot.Value.index] = element;
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

            private static ItemObject CreateCustomWeapon(Hero hero, HeroClassDef heroClass, IEnumerable<WeaponClass> weaponClasses)
            {
                // Randomly choose where to make a craftable weapon or choose a pre-existing one, weighted by the number of 
                // each weapon class in those categories
                var craftingGroup = weaponClasses
                    .GroupBy(c => CustomItems.CraftableWeaponClasses.Contains(c))
                    .SelectRandomWeighted(g => g.Count());

                if (!craftingGroup.Key)
                {
                    // Get the highest tier we can for the weapon type
                    var itemType = craftingGroup.SelectRandom(); 
                    return EquipHero.FindRandomTieredEquipment(null, 5, hero, EquipHero.FindFlags.IgnoreAbility,
                        o => o.WeaponComponent?.PrimaryWeapon?.WeaponClass == itemType
                        && EquipHero.UsableWeaponFilter(o, heroClass));
                }
                else
                {
                    return CustomItems.CreateCraftedWeapon(hero, craftingGroup.ToList(), 5);
                }
            }
            
            private static ItemModifier GenerateItemModifier(ItemObject item, string modifierName)
            {
                string modifiedName = $"{modifierName} {{ITEMNAME}}";
                float modifierPower = BLTAdoptAHeroModule.TournamentConfig.CustomPrizePower;
                if (item.WeaponComponent?.PrimaryWeapon?.IsMeleeWeapon == true
                    || item.WeaponComponent?.PrimaryWeapon?.IsPolearm == true
                    || item.WeaponComponent?.PrimaryWeapon?.IsRangedWeapon == true
                    )
                {
                    return BLTCustomItemsCampaignBehavior.Current.CreateWeaponModifier(
                        modifiedName,
                        (int) Mathf.Ceil(MBRandom.RandomInt(
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeWeaponDamageMin, 
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeWeaponDamageMax) * modifierPower),
                        (int) Mathf.Ceil(MBRandom.RandomInt(
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeWeaponSpeedMin, 
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeWeaponSpeedMax) * modifierPower),
                        (int) Mathf.Ceil(MBRandom.RandomInt(
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeWeaponMissileSpeedMin, 
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeWeaponMissileSpeedMax) * modifierPower),
                        (short) Mathf.Ceil(MBRandom.RandomInt(
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeThrowingStackMin, 
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeThrowingStackMax) * modifierPower)
                    );
                }
                else if (item.WeaponComponent?.PrimaryWeapon?.IsAmmo == true)
                {
                    return BLTCustomItemsCampaignBehavior.Current.CreateAmmoModifier(
                        modifiedName,
                        (int) Mathf.Ceil(MBRandom.RandomInt(
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeAmmoDamageMin, 
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeAmmoDamageMax) * modifierPower),
                        (short) Mathf.Ceil(MBRandom.RandomInt(
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeArrowStackMin, 
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeArrowStackMax) * modifierPower)
                    );
                }
                else if (item.HasArmorComponent)
                {
                    return BLTCustomItemsCampaignBehavior.Current.CreateArmorModifier(
                        modifiedName,
                        (int) Mathf.Ceil(MBRandom.RandomInt(
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeArmorMin, 
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeArmorMax) * modifierPower)
                    );
                }
                else if (item.IsMountable)
                {
                    return BLTCustomItemsCampaignBehavior.Current.CreateMountModifier(
                        modifiedName,
                        MBRandom.RandomFloatRanged(
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeMountManeuverMin, 
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeMountManeuverMax) * modifierPower,
                        MBRandom.RandomFloatRanged(
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeMountSpeedMin, 
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeMountSpeedMax) * modifierPower,
                        MBRandom.RandomFloatRanged(
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeMountChargeDamageMin, 
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeMountChargeDamageMax) * modifierPower,
                        MBRandom.RandomFloatRanged(
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeMountHitPointsMin, 
                            BLTAdoptAHeroModule.TournamentConfig.CustomPrizeMountHitPointsMax) * modifierPower
                    );
                }
                else
                {
                    Log.Error($"Cannot generate modifier for {item.Name}: its modifier requirements could not be determined");
                    return null;
                }
            }
            
#if DEBUG
            [CommandLineFunctionality.CommandLineArgumentFunction("testprize", "blt")]
            [UsedImplicitly]
            public static string TestTournamentCustomPrize(List<string> strings)
            {
                if (strings.Count == 1)
                {
                    int count = int.Parse(strings[0]);
                    for (int i = 0; i < count; i++)
                    {
                        var (item, modifier) = GeneratePrize(Hero.MainHero);
                        if (item == null)
                        {
                            return $"Couldn't generate a matching item";
                        }
                        var equipment = new EquipmentElement(item, modifier);
                        Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(equipment, 1);
                    }
                    return $"Added {count} items to {Hero.MainHero.Name}";
                }
                else if (strings.Count == 3)
                {
                    int count = int.Parse(strings[2]);
                    var prizeType = (GlobalTournamentConfig.PrizeType) Enum.Parse(typeof(GlobalTournamentConfig.PrizeType), strings[0]);
                    var classDef = BLTAdoptAHeroModule.HeroClassConfig.FindClass(strings[1]);

                    for (int i = 0; i < count; i++)
                    {
                        var (item, modifier) = GeneratePrizeType(prizeType, 6, Hero.MainHero, classDef);
                
                        if (item == null)
                        {
                            return $"Couldn't generate a matching item";
                        }

                        var equipment = new EquipmentElement(item, modifier);
                
                        Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(equipment, 1);
                    }

                    return $"Added {count} items to {Hero.MainHero.Name}";
                }
                else
                {
                    return "Expected 1 or 3 arguments: blt.testprize <number to make> OR blt.testprize Weapon/Armor/Mount <class name> <number to make>";
                }
            }

            [CommandLineFunctionality.CommandLineArgumentFunction("testprize2", "blt")]
            [UsedImplicitly]
            public static string TestTournamentCustomPrize2(List<string> strings)
            {
                foreach (var h in BLTAdoptAHeroCampaignBehavior.GetAllAdoptedHeroes())
                {
                    var (item, itemModifier) = GeneratePrize(h);
                    if (item != null)
                    {
                        (bool upgraded, string failReason) = UpgradeToItem(h, new(item, itemModifier), itemModifier != null);
                        if (!upgraded)
                        {
                            Log.Error($"Failed to upgrade {item.Name} for {h.Name}: {failReason}");
                        }
                    }
                    else
                    {
                        Log.Error($"Failed to generate prize for {h.Name}");
                    }
                }

                return "done";
            }
#endif  

            private static (ItemObject item, ItemModifier modifier) GeneratePrizeType(GlobalTournamentConfig.PrizeType prizeType, int tier, Hero hero, HeroClassDef heroClass)
            {
                switch (prizeType)
                {
                    case GlobalTournamentConfig.PrizeType.Weapon:
                    {
                        var weaponSkills = heroClass != null
                                ? SkillGroup.SkillItemPairs.Where(s => heroClass.Weapons.Any(sk => sk == s.itemType))
                                // Without class we just take the top skill only (if we take multiple skills then we end up carrying a weapon of every skill type)
                                : SkillGroup.SkillItemPairs.OrderByDescending(s => hero.GetSkillValue(s.skill)).Take(1)
                            ;
                        // List of heroes current weapons
                        var heroWeapons = hero.BattleEquipment.YieldFilledWeaponSlots().ToList();
                        // List of heroes custom weapons, so we can avoid giving duplicates
                        var heroCustomWeapons = heroWeapons.Where(w => BLTCustomItemsCampaignBehavior.Current.IsRegistered(w.ItemModifier)).ToList();
                        // Custom "modified" item
                        if (tier > 5)
                        {
                            // Find appropriate weapon classes for the weapon skills the hero prefers
                            var weaponClasses = SkillGroup.SkillWeaponClassPairs
                                .Where(s => weaponSkills.Any(s2 => s2.skill == s.skill))
                                .SelectMany(s => s.weaponClasses.Select(s2 => (skill: s.skill, weapoonClass: s2)))
                                // don't want items we can't use, e.g. bolts if we don't have crossbow, and vice versa
                                .Where(s => 
                                    // Exclude bolts if hero doesn't have a crossbow already
                                    (s.weapoonClass != WeaponClass.Bolt || heroWeapons.Any(i => i.Item.WeaponComponent?.PrimaryWeapon?.AmmoClass == WeaponClass.Bolt))
                                    // Exclude arrows if hero doesn't have a bow
                                    && (s.weapoonClass != WeaponClass.Arrow || heroWeapons.Any(i => i.Item.WeaponComponent?.PrimaryWeapon?.AmmoClass == WeaponClass.Arrow))
                                    // Exclude any weapons we already have a custom version of, by skill (this isn't perfect as we will only ever get one custom ammo, but its okay)
                                    && heroCustomWeapons.All(i => s.skill != i.Item.RelevantSkill))
                                .Shuffle()
                                .ToList();
                            if (!weaponClasses.Any())
                            {
                                return default;
                            }
                            var weapon = CreateCustomWeapon(hero,  heroClass, weaponClasses.Select(w => w.weapoonClass));
                            return weapon == null ? default : (weapon, GenerateItemModifier(weapon, "Prize"));
                        }
                        else
                        {
                            var weapon = weaponSkills
                                .Shuffle()
                                .Select(sk => EquipHero.FindRandomTieredEquipment(null, 5, hero, EquipHero.FindFlags.IgnoreAbility,
                                    // Exclude any weapons we already have a custom version of (this isn't perfect as we will only ever get one custom ammo, but its okay)
                                    i => heroCustomWeapons.All(i2 => i2.Item.ItemType != i.ItemType), 
                                    sk.itemType))
                                .FirstOrDefault(w => w != null);
                            return (weapon, null);
                        }
                    }
                    case GlobalTournamentConfig.PrizeType.Armor:
                    {
                        // List of custom armors the hero already has, so we can ensure we don't get duplicates 
                        var heroCustomArmors = hero.BattleEquipment
                            .YieldFilledArmorSlots()
                            .Where(w => BLTCustomItemsCampaignBehavior.Current.IsRegistered(w.ItemModifier))
                            .ToList();
                        
                        // Select randomly from the various armor types we can choose between, notice that ChestArmor is NOT here, its not currently used by the game, 
                        // and including it will result in a failure to find any matching items
                        var armorPart = new[] {
                                ItemObject.ItemTypeEnum.BodyArmor,
                                ItemObject.ItemTypeEnum.Cape,
                                ItemObject.ItemTypeEnum.HandArmor,
                                ItemObject.ItemTypeEnum.HeadArmor,
                                ItemObject.ItemTypeEnum.LegArmor,
                            }
                            // Exclude any armors we already have a custom version of
                            .Where(i => heroCustomArmors.All(i2 => i2.Item.ItemType != i))
                            .SelectRandom();

                        // Custom "modified" item
                        if (tier > 5)
                        {
                            var armor = EquipHero.FindRandomTieredEquipment(null, 5, hero, EquipHero.FindFlags.IgnoreAbility, null, armorPart);
                            return armor == null ? default : (armor, GenerateItemModifier(armor, "Prize"));
                        }
                        else
                        {
                            return (EquipHero.FindRandomTieredEquipment(null, tier, hero, EquipHero.FindFlags.IgnoreAbility, null, armorPart), null);
                        }
                    }
                    case GlobalTournamentConfig.PrizeType.Mount:
                    {
                        // If they already have a prize horse then don't get another one
                        var horseSlot = hero.BattleEquipment[EquipmentIndex.Horse];
                        if (BLTCustomItemsCampaignBehavior.Current.IsRegistered(horseSlot.ItemModifier))
                        {
                            return default;
                        }

                        // DOING: test consecutive tournament prize generation blt.testprize2. confirm it avoid duplicates, doesn't crash, etc.
                        // if an item can't be found then just the winner a bunch of gold? or nothing? make a setting for it.
                        // investigate viewing the new item
                        // add command for naming the new item (record the items in the HeroData, so they can be named)
                        
                        var mount = HeroHelpers.AllItems
                            .Where(item => item.IsMountable && item.Tier >= ItemObject.ItemTiers.Tier3)
                            .SelectRandom();
                        if (mount == null)
                        {
                            return default;
                        }
                        var modifier = GenerateItemModifier(mount, "Prize");
                        return (mount, modifier);
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(prizeType), prizeType, null);
                }
            }

            private static (ItemObject item, ItemModifier modifier) GeneratePrize(Hero hero)
            {
                var heroClass = BLTAdoptAHeroCampaignBehavior.Current.GetClass(hero);

                int tier = BLTAdoptAHeroModule.TournamentConfig.PrizeTierWeights.SelectRandomWeighted(t => t.weight).tier;
                bool shouldUseHorse = EquipHero.HeroShouldUseHorse(hero, heroClass);
                return BLTAdoptAHeroModule.TournamentConfig.PrizeTypeWeights
                        // Exclude mount when it shouldn't be used by the hero or they already have a tournament reward horse
                        .Where(p => shouldUseHorse || p.type != GlobalTournamentConfig.PrizeType.Mount)
                        .OrderRandomWeighted(t => t.weight)
                        .Select(t => GeneratePrizeType(t.type, tier, hero, heroClass))
                        .FirstOrDefault(i => i != default)
                    ;
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
                            BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(entry.Hero, actualGold);
                            results.Add($"{Naming.Inc}{actualGold}{Naming.Gold}");
                        }
                        int xp = (int) (BLTAdoptAHeroModule.TournamentConfig.WinMatchXP * actualBoost);
                        if (xp > 0)
                        {
                            (bool success, string description) =
                                SkillXP.ImproveSkill(entry.Hero, xp, SkillsEnum.All, auto: true);
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
                                SkillXP.ImproveSkill(entry.Hero, xp, SkillsEnum.All, auto: true);
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

            private static void SetPlaceholderPrize(TournamentGame tournamentGame)
            {
                if (BLTAdoptAHeroModule.TournamentConfig.UseCustomPrizes)
                {
                    AccessTools.Property(typeof(TournamentGame), nameof(TournamentGame.Prize))
                        .SetValue(tournamentGame, DefaultItems.Charcoal);
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
