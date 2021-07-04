using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.ButterLib.SaveSystem.Extensions;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Actions.Util;
using BLTAdoptAHero.UI;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox.TournamentMissions.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TaleWorlds.TwoDimension;

namespace BLTAdoptAHero
{
    public class BLTTournamentQueueBehavior : CampaignBehaviorBase, IDisposable
    {
        public static BLTTournamentQueueBehavior Current => GetCampaignBehavior<BLTTournamentQueueBehavior>();
            
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
                var usedHeroList = tournamentQueue.Select<TournamentQueueEntry, Hero>(t => t.Hero).ToList();
                dataStore.SyncData("UsedHeroObjectList", ref usedHeroList);
                var queue = tournamentQueue.Select<TournamentQueueEntry, TournamentQueueEntrySavable>(e => new TournamentQueueEntrySavable
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
            
        public void GetTeamWeaponEquipmentList(List<Equipment> equipments)
        {
            var replacementWeapon =
                HeroHelpers.AllItems.FirstOrDefault(i => i.StringId == "empire_sword_1_t2_blunt");
            foreach (var e in equipments)
            {
                if (BLTAdoptAHeroModule.TournamentConfig.NoHorses)
                {
                    e[EquipmentIndex.Horse] = EquipmentElement.Invalid;
                    e[EquipmentIndex.HorseHarness] = EquipmentElement.Invalid;
                }

                if (replacementWeapon != null && BLTAdoptAHeroModule.TournamentConfig.NoSpears)
                {
                    foreach (var (element, index) in e.YieldWeaponSlots())
                    {
                        if (element.Item?.Type == ItemObject.ItemTypeEnum.Polearm && !element.Item.IsSwingable())
                        {
                            e[index] = new(replacementWeapon);
                        }
                    }
                }
            }
        }
            
        public void PrepareForTournamentGame()
        {
            if (!activeTournament.Any())
            {
                return;
            }

            var savedArmor = new Dictionary<Hero, List<(EquipmentIndex slot, EquipmentElement element)>>();
            if (BLTAdoptAHeroModule.TournamentConfig.NormalizeArmor)
            {
                var tier = (ItemObject.ItemTiers)Math.Max(0, Math.Min(5, BLTAdoptAHeroModule.TournamentConfig.NormalizeArmorTier - 1));
                var culture = Settlement.CurrentSettlement.Culture;
                var replacements = SkillGroup.ArmorIndexType
                    .Select(slotItemTypePair =>
                    (
                        slot: slotItemTypePair.slot, 
                        item: HeroHelpers.AllItems.FirstOrDefault(i 
                                  => i.Culture == culture && i.Tier == tier && i.ItemType == slotItemTypePair.itemType)
                              ?? HeroHelpers.AllItems.FirstOrDefault(i 
                                  => i.Tier == tier && i.ItemType == slotItemTypePair.itemType)
                    )).ToList();

                foreach (var entry in activeTournament)
                {
                    savedArmor.Add(entry.Hero, SkillGroup.ArmorIndexType.Select(slotItemTypePair 
                        => (slotItemTypePair.slot, entry.Hero.BattleEquipment[slotItemTypePair.slot])).ToList());

                    foreach (var (slot, item) in replacements)
                    {
                        entry.Hero.BattleEquipment[slot] = new(item);
                    }
                }
            }
				
            var tournamentBehaviour = MissionState.Current.CurrentMission.GetMissionBehaviour<TournamentBehavior>();

            tournamentBehaviour.TournamentEnd += () =>
            {
                // Win results, put winner last
                foreach (var entry in activeTournament
                    .OrderBy(e => e.Hero == tournamentBehaviour.Winner.Character?.HeroObject)
                )
                {
                    if (entry.Hero != null && savedArmor.TryGetValue(entry.Hero, out var originalGear))
                    {
                        foreach (var (slot, element) in originalGear)
                        {
                            entry.Hero.BattleEquipment[slot] = element;
                        }
                    }
                        
                    float actualBoost = entry.IsSub ? Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1) : 1;
                    var results = new List<string>();
                    if (entry.Hero != null && entry.Hero == tournamentBehaviour.Winner.Character?.HeroObject)
                    {
                        results.Add("WINNER!");

                        BLTAdoptAHeroCampaignBehavior.Current.IncreaseTournamentChampionships(entry.Hero);
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

                        var (item, itemModifier, slot) = GeneratePrize(entry.Hero);
                        if (item == null)
                        {
                            results.Add($"no prize available for you!");
                        }
                        else
                        {
                            var element = new EquipmentElement(item, itemModifier);
                            bool isCustom = BLTCustomItemsCampaignBehavior.Current.IsRegistered(itemModifier);

                            // We always put our custom items into the heroes storage, even if we won't use them right now
                            if (isCustom)
                            {
                                BLTAdoptAHeroCampaignBehavior.Current.AddCustomItem(entry.Hero, element);
                            }
                                
                            if (slot != EquipmentIndex.None)
                            {
                                entry.Hero.BattleEquipment[slot] = element;
                                results.Add($"received {element.GetModifiedItemName()}");
                            }
                            else if (!isCustom)
                            {
                                // Sell non-custom items
                                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(entry.Hero, item.Value * 5);
                                results.Add($"sold {element.GetModifiedItemName()} for {item.Value}{Naming.Gold} (not needed)");
                            }
                            else
                            {
                                // should never happen really, as custom items are only created when they can be equipped 
                                results.Add($"received {element.GetModifiedItemName()} (put in storage)");
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

                    if (results.Any() && entry.Hero != null)
                    {
                        Log.LogFeedResponse(entry.Hero.FirstName.ToString(), results.ToArray());
                    }
                }

                activeTournament.Clear();
            };
        }

        private static ItemObject CreateCustomWeapon(Hero hero, HeroClassDef heroClass, EquipmentType weaponType)
        {
            if (!CustomItems.CraftableEquipmentTypes.Contains(weaponType))
            {
                // Get the highest tier we can for the weapon type
                var item = EquipHero.FindRandomTieredEquipment(5, hero, EquipHero.FindFlags.IgnoreAbility,
                    o => o.IsEquipmentType(weaponType) && EquipHero.UsableWeaponFilter(o, heroClass));
                return item;
            }
            else
            {
                return CustomItems.CreateCraftedWeapon(hero, weaponType, 5);
            }
        }
            
        private static ItemModifier GenerateItemModifier(ItemObject item, string modifierName)
        {
            string modifiedName = $"{modifierName} {{ITEMNAME}}";
            float modifierPower = BLTAdoptAHeroModule.TournamentConfig.CustomPrize.Power;
            if (item.WeaponComponent?.PrimaryWeapon?.IsMeleeWeapon == true
                || item.WeaponComponent?.PrimaryWeapon?.IsPolearm == true
                || item.WeaponComponent?.PrimaryWeapon?.IsRangedWeapon == true
            )
            {
                return BLTCustomItemsCampaignBehavior.Current.CreateWeaponModifier(
                    modifiedName,
                    (int) Mathf.Ceil(MBRandom.RandomInt(
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.WeaponDamage.Min, 
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.WeaponDamage.Max) * modifierPower),
                    (int) Mathf.Ceil(MBRandom.RandomInt(
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.WeaponSpeed.Min, 
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.WeaponSpeed.Max) * modifierPower),
                    (int) Mathf.Ceil(MBRandom.RandomInt(
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.WeaponMissileSpeed.Min, 
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.WeaponMissileSpeed.Max) * modifierPower),
                    (short) Mathf.Ceil(MBRandom.RandomInt(
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.ThrowingStack.Min, 
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.ThrowingStack.Max) * modifierPower)
                );
            }
            else if (item.WeaponComponent?.PrimaryWeapon?.IsAmmo == true)
            {
                return BLTCustomItemsCampaignBehavior.Current.CreateAmmoModifier(
                    modifiedName,
                    (int) Mathf.Ceil(MBRandom.RandomInt(
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.AmmoDamage.Min, 
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.AmmoDamage.Max) * modifierPower),
                    (short) Mathf.Ceil(MBRandom.RandomInt(
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.ArrowStack.Min, 
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.ArrowStack.Max) * modifierPower)
                );
            }
            else if (item.HasArmorComponent)
            {
                return BLTCustomItemsCampaignBehavior.Current.CreateArmorModifier(
                    modifiedName,
                    (int) Mathf.Ceil(MBRandom.RandomInt(
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.Armor.Min, 
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.Armor.Max) * modifierPower)
                );
            }
            else if (item.IsMountable)
            {
                return BLTCustomItemsCampaignBehavior.Current.CreateMountModifier(
                    modifiedName,
                    MBRandom.RandomFloatRanged(
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.MountManeuver.Min, 
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.MountManeuver.Max) * modifierPower,
                    MBRandom.RandomFloatRanged(
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.MountSpeed.Min, 
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.MountSpeed.Max) * modifierPower,
                    MBRandom.RandomFloatRanged(
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.MountChargeDamage.Min, 
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.MountChargeDamage.Max) * modifierPower,
                    MBRandom.RandomFloatRanged(
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.MountHitPoints.Min, 
                        BLTAdoptAHeroModule.TournamentConfig.CustomPrize.MountHitPoints.Max) * modifierPower
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
                    var (item, modifier, _) = GeneratePrize(Hero.MainHero);
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
                    var (item, modifier, _) = GeneratePrizeType(prizeType, 6, Hero.MainHero, classDef);
                
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
                var (item, itemModifier, slot) = GeneratePrize(h);
                if (item != null)
                {
                    var element = new EquipmentElement(item, itemModifier);
                    BLTAdoptAHeroCampaignBehavior.Current.AddCustomItem(h, element);
                    if (slot != EquipmentIndex.None)
                    {
                        h.BattleEquipment[slot] = element;
                    }
                    //(bool upgraded, string failReason) = UpgradeToItem(h, new(item, itemModifier), itemModifier != null);
                    // if (!upgraded)
                    // {
                    //     Log.Error($"Failed to upgrade {item.Name} for {h.Name}: {failReason}");
                    // }
                }
                else
                {
                    Log.Error($"Failed to generate prize for {h.Name}");
                }
            }

            if (GameStateManager.Current.ActiveState is InventoryState inventoryState)
            {
                inventoryState.InventoryLogic?.Reset();
            }

            return "done";
        }
#endif

        private static (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GeneratePrizeType(GlobalTournamentConfig.PrizeType prizeType, int tier, Hero hero, HeroClassDef heroClass)
        {
            return prizeType switch
            {
                GlobalTournamentConfig.PrizeType.Weapon => GeneratePrizeTypeWeapon(tier, hero, heroClass),
                GlobalTournamentConfig.PrizeType.Armor => GeneratePrizeTypeArmor(tier, hero),
                GlobalTournamentConfig.PrizeType.Mount => GeneratePrizeTypeMount(tier, hero, heroClass),
                _ => throw new ArgumentOutOfRangeException(nameof(prizeType), prizeType, null)
            };
        }

        private static (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GeneratePrizeTypeWeapon(
            int tier, Hero hero, HeroClassDef heroClass)
        {
            // List of heroes custom items, so we can avoid giving duplicates (it will include what they are carrying, as all custom items are registered)
            var heroCustomWeapons = BLTAdoptAHeroCampaignBehavior.Current.GetCustomItems(hero);

            // List of heroes current weapons
            var heroWeapons = hero.BattleEquipment.YieldFilledWeaponSlots().ToList();

            var replaceableHeroWeapons = heroWeapons
                .Where(w =>
                    // Must be lower than the desired tier
                    (int)w.element.Item.Tier < tier 
                    // Must not be a custom item
                    && !BLTCustomItemsCampaignBehavior.Current.IsRegistered(w.element.ItemModifier))
                .Select(w => (w.index, w.element.Item.GetEquipmentType()));


            // Weapon classes we can generate a prize for, with some heuristics to avoid some edge cases, and getting duplicates
            var weaponClasses = 
                (heroClass?.IndexedWeapons ?? replaceableHeroWeapons)
                .Where(s =>
                    // No shields, they aren't cool rewards and don't support any modifiers
                    s.type != EquipmentType.Shield
                    // Exclude bolts if hero doesn't have a crossbow already
                    && (s.type != EquipmentType.Bolts || heroWeapons.Any(i => i.element.Item.WeaponComponent?.PrimaryWeapon?.AmmoClass == WeaponClass.Bolt))
                    // Exclude arrows if hero doesn't have a bow
                    && (s.type != EquipmentType.Arrows || heroWeapons.Any(i => i.element.Item.WeaponComponent?.PrimaryWeapon?.AmmoClass == WeaponClass.Arrow))
                    // Exclude any weapons we already have enough custom versions of (if we have class then we can match the class count, otherwise we just limit it to 1)
                    && heroCustomWeapons.Count(i => i.Item.IsEquipmentType(s.type)) < (heroClass?.Weapons.Count(w => w == s.type) ?? 1)
                )
                .Shuffle()
                .ToList();

            if (!weaponClasses.Any())
            {
                return default;
            }

            // Tier > 5 indicates custom weapons with modifiers
            if (tier > 5)
            {
                // Custom "modified" item
                var (item, index) = weaponClasses
                    .Select(c => (
                        item: CreateCustomWeapon(hero, heroClass, c.type),
                        index: c.index))
                    .FirstOrDefault(w => w.item != null);
                return item == null 
                        ? default 
                        : (item, GenerateItemModifier(item, "Prize"), index)
                    ;
            }
            else
            {
                // Find a random item fitting the weapon class requirements
                var (item, index) = weaponClasses
                    .Select(c => (
                        item: EquipHero.FindRandomTieredEquipment(tier, hero, EquipHero.FindFlags.IgnoreAbility | EquipHero.FindFlags.RequireExactTier, 
                            i => i.IsEquipmentType(c.type)),
                        index: c.index))
                    .FirstOrDefault(w => w.item != null);
                return item == null || hero.BattleEquipment[index].Item?.Tier >= item.Tier
                    ? default 
                    : (item, null, index);
            }
        }

        private static (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GeneratePrizeTypeArmor(int tier, Hero hero)
        {
            // List of custom items the hero already has, and armor they are wearing that is as good or better than the tier we want 
            var heroBetterArmor = BLTAdoptAHeroCampaignBehavior.Current
                .GetCustomItems(hero)
                .Concat(hero.BattleEquipment.YieldFilledArmorSlots().Where(e => (int)e.Item.Tier >= tier));

            // Select randomly from the various armor types we can choose between
            var (index, itemType) = SkillGroup.ArmorIndexType
                // Exclude any armors we already have an equal or better version of
                .Where(i => heroBetterArmor.All(i2 => i2.Item.ItemType != i.itemType))
                .SelectRandom();

            if (index == default)
            {
                return default;
            }
                
            // Custom "modified" item
            if (tier > 5)
            {
                var armor = EquipHero.FindRandomTieredEquipment(5, hero, 
                    EquipHero.FindFlags.IgnoreAbility,
                    o => o.ItemType == itemType);
                return armor == null ? default : (armor, GenerateItemModifier(armor, "Prize"), index);
            }
            else
            {
                var armor = EquipHero.FindRandomTieredEquipment(tier, hero, 
                    EquipHero.FindFlags.IgnoreAbility | EquipHero.FindFlags.RequireExactTier,
                    o => o.ItemType == itemType);
                // if no armor was found, or its the same tier as what we have then return null
                return armor == null || hero.BattleEquipment.YieldFilledArmorSlots().Any(i2 => i2.Item.Type == armor.Type && i2.Item.Tier >= armor.Tier) 
                    ? default 
                    : (armor, null, index);
            }
        }

        private static (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GeneratePrizeTypeMount(
            int tier, Hero hero, HeroClassDef heroClass)
        {
            var currentMount = hero.BattleEquipment.Horse;
            // If we are generating is non custom prize, and the hero has a non custom mount already,
            // of equal or better tier, we don't replace it
            if (tier <= 5 && !currentMount.IsEmpty && (int) currentMount.Item.Tier >= tier)
            {
                return default;
            }

            // If the hero has a custom mount already, then we don't give them another, or any non custom one
            if (BLTAdoptAHeroCampaignBehavior.Current.GetCustomItems(hero).Any(i => i.Item.ItemType == ItemObject.ItemTypeEnum.Horse))
            {
                return default;
            }
                
            bool IsCorrectMountFamily(ItemObject item)
            {  
                // Must match hero class requirements
                return (heroClass == null
                        || heroClass.UseHorse && item.HorseComponent.Monster.FamilyType is (int) EquipHero.MountFamilyType.horse 
                        || heroClass.UseCamel && item.HorseComponent.Monster.FamilyType is (int) EquipHero.MountFamilyType.camel)
                       // Must also not differ from current mount family type (or saddle can get messed up)
                       && (currentMount.IsEmpty 
                           || currentMount.Item.HorseComponent.Monster.FamilyType == item.HorseComponent.Monster.FamilyType
                       );
            }
                
            // Find mounts of the correct family type and tier
            var mount = HeroHelpers.AllItems
                .Where(item =>
                    item.IsMountable
                    // If we are making a custom mount then use any mount over Tier 2, otherwise match the tier exactly 
                    && (tier > 5 && (int)item.Tier >= 2 || (int)item.Tier == tier)  
                    && IsCorrectMountFamily(item)
                )
                .SelectRandom();

            if (mount == null)
            {
                return default;
            }

            var modifier = tier > 5 
                ? GenerateItemModifier(mount, "Prize") 
                : null;
            return (mount, modifier, EquipmentIndex.Horse);
        }

        private static (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GeneratePrize(Hero hero)
        {
            var heroClass = BLTAdoptAHeroCampaignBehavior.Current.GetClass(hero);

            // Randomize the reward tier order, by random weighting
            var tiers = BLTAdoptAHeroModule.TournamentConfig.PrizeTierWeights
                .OrderRandomWeighted(tier => tier.weight).ToList();
            //int tier = BLTAdoptAHeroModule.TournamentConfig.PrizeTierWeights.SelectRandomWeighted(t => t.weight).tier;
            bool shouldUseHorse = EquipHero.HeroShouldUseHorse(hero, heroClass);
            return BLTAdoptAHeroModule.TournamentConfig.PrizeTypeWeights
                    // Exclude mount when it shouldn't be used by the hero or they already have a tournament reward horse
                    .Where(p => shouldUseHorse || p.type != GlobalTournamentConfig.PrizeType.Mount)
                    // Randomize the reward type order, by random weighting
                    .OrderRandomWeighted(type => type.weight)
                    .SelectMany(type => 
                        tiers.Select(tier => GeneratePrizeType(type.type, tier.tier, hero, heroClass)))
                    .FirstOrDefault(i => i != default)
                ;
        }

        private bool bettingOpen;
        private Dictionary<Hero, (int team, int bet)> activeBets;

        private static readonly string[] TeamNames = { "blue", "red", "green", "yellow" };
        public void OpenBetting(TournamentBehavior tournamentBehavior)
        {
            if (BLTAdoptAHeroModule.TournamentConfig.EnableBetting 
                && tournamentBehavior.CurrentMatch != null
                && (tournamentBehavior.CurrentRoundIndex == 3 || !BLTAdoptAHeroModule.TournamentConfig.BettingOnFinalOnly))
            {
                var teams = TeamNames.Take(tournamentBehavior.CurrentMatch.Teams.Count());
                string round = tournamentBehavior.CurrentRoundIndex < 3
                    ? $"round {tournamentBehavior.CurrentRoundIndex + 1}"
                    : "final";
                ActionManager.SendChat($"Betting is now OPEN for {round} match: {string.Join(" vs ", teams)}!");
                activeBets = new();
            }
            bettingOpen = true;
        }
        
        public (bool success, string failReason) PlaceBet(Hero hero, string team, int bet)
        {
            var tournamentBehavior = Mission.Current?.GetMissionBehaviour<TournamentBehavior>();
            if (tournamentBehavior == null)
            {
                return (false, "Tournament is not active");
            }

            if (!BLTAdoptAHeroModule.TournamentConfig.EnableBetting)
            {
                return (false, "Betting is disabled");
            }
            
            if (!bettingOpen)
            {
                return (false, "Betting is closed");
            }
            
            if (tournamentBehavior.CurrentRoundIndex != 3 && BLTAdoptAHeroModule.TournamentConfig.BettingOnFinalOnly)
            {
                return (false, "Betting is only allowed on the final");
            }

            if (activeBets == null)
            {
                return (false, "Betting is disabled");
            }

            if (activeBets.ContainsKey(hero))
            {
                return (false, "You already placed a bet");
            }

            int teamsCount = tournamentBehavior.CurrentMatch.Teams.Count();
            var activeTeams = TeamNames.Take(teamsCount).ToArray();
            int teamIdx = activeTeams.IndexOf(team.ToLower());
            if (teamIdx == -1)
            {
                return (false, $"Team name must be one of {string.Join(", ", activeTeams)}");
            }
            
            int heroGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(hero);
            if (heroGold < bet)
            {
                return (false, Naming.NotEnoughGold(bet, heroGold));
            }
            
            activeBets.Add(hero, (teamIdx, bet));
            
            // Take the actual money
            BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(hero, -bet);

            return (true, null);
        }

        public void CloseBetting(TournamentBehavior tournamentBehavior)
        {
            // We use this being non-null as an indicator that betting was active
            if (activeBets != null)
            {
                var betTotals = activeBets.Values
                    .Select(b => (name: TeamNames[b.team], b.bet))
                    .GroupBy(b => b.name)
                    .Select(g => $"{g.Key} {g.Select(x => x.bet).Sum()}{Naming.Gold}")
                    .ToList()
                    ;
                ActionManager.SendChat(betTotals.Any()
                    ? $"Betting is now CLOSED: {string.Join(", ", betTotals)}"
                    : $"Betting is now CLOSED: no bets placed"
                );
            }
            
            bettingOpen = false;
        }

        public void EndCurrentMatch(TournamentBehavior tournamentBehavior)
        {
            var lastMatch = tournamentBehavior.LastMatch;
            CompleteBetting(lastMatch);

            // If the tournament is over
            if (tournamentBehavior.CurrentRoundIndex == 4 || lastMatch == null)
                return;

            if(tournamentBehavior.CurrentMatch != null)
            {
                OpenBetting(tournamentBehavior);
            }   
            
            // End round effects (as there is no event handler for it :/)
            foreach (var entry in activeTournament)
            {
                float actualBoost = entry.IsSub ? Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1) : 1;
                    
                var results = new List<string>();

                if(lastMatch.Winners.Any(w => w.Character?.HeroObject == entry.Hero))
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
                    BLTAdoptAHeroCampaignBehavior.Current.IncreaseTournamentWins(entry.Hero);
                }
                else if (lastMatch.Participants.Any(w => w.Character?.HeroObject == entry.Hero))
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
                    BLTAdoptAHeroCampaignBehavior.Current.IncreaseTournamentLosses(entry.Hero);
                }
                if (results.Any())
                {
                    Log.LogFeedResponse(entry.Hero.FirstName.ToString(), results.ToArray());
                }
            }
        }

        private void CompleteBetting(TournamentMatch lastMatch)
        {
            if (activeBets != null)
            {
                double totalBet = activeBets.Values.Sum(v => v.bet);
                var allBets = activeBets
                    .Select(kv => (
                        hero: kv.Key,
                        bet: kv.Value.bet,
                        won: lastMatch.Winners.Contains(lastMatch.Teams.ElementAt(kv.Value.team).Participants.First())
                    ))
                    .ToList();
                double winningTotalBet = allBets.Where(w => w.won).Sum(v => v.bet);
                foreach ((var hero, int bet, bool won) in allBets.OrderByDescending(b => b.bet))
                {
                    if (won)
                    {
                        int winnings = (int) (totalBet * bet / winningTotalBet);
                        int newGold = BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(hero, winnings);

                        Log.LogFeedResponse(hero.FirstName.ToString(),
                            $"WON BET {Naming.Inc}{winnings}{Naming.Gold}{Naming.To}{newGold}{Naming.Gold}");
                    }
                    else
                    {
                        BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(hero, -bet);
                    }
                }

                activeBets = null;
            }
        }

        private static void SetPlaceholderPrize(TournamentGame tournamentGame)
        {
            AccessTools.Property(typeof(TournamentGame), nameof(TournamentGame.Prize))
                .SetValue(tournamentGame, DefaultItems.Charcoal);
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
}