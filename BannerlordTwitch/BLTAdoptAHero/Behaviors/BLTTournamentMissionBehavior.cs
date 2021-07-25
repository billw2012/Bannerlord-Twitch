using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Actions.Util;
using BLTAdoptAHero.Util;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox;
using SandBox.TournamentMissions.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;

namespace BLTAdoptAHero
{
    [HarmonyPatch]
    public class BLTTournamentMissionBehavior : AutoMissionBehavior<BLTTournamentMissionBehavior>
    {
        private readonly List<BLTTournamentQueueBehavior.TournamentQueueEntry> activeTournament = new();

        private bool isPlayerParticipating;

        public BLTTournamentMissionBehavior(bool isPlayerParticipating, TournamentGame tournamentGame)
        {
            this.isPlayerParticipating = isPlayerParticipating;
            SetPlaceholderPrize(tournamentGame);
        }

        public List<CharacterObject> GetParticipants()
        {
            var tournamentQueue = BLTTournamentQueueBehavior.Current.TournamentQueue;
                
            var participants = new List<CharacterObject>();
            if(isPlayerParticipating)
                participants.Add(Hero.MainHero.CharacterObject);
            
            int viewersToAddCount = Math.Min(16 - participants.Count, tournamentQueue.Count);
                
            var viewersToAdd = tournamentQueue.Take(viewersToAddCount).ToList();
            participants.AddRange(viewersToAdd.Select(q => q.Hero.CharacterObject));
            activeTournament.AddRange(viewersToAdd);
            tournamentQueue.RemoveRange(0, viewersToAddCount);
            
            var basicTroops = HeroHelpers.AllCultures
                .SelectMany(c => new[] {c.BasicTroop, c.EliteBasicTroop})
                .Where(t => t != null)
                .ToList();

            while (participants.Count < 16)
            {
                participants.Add(basicTroops.SelectRandom());
            }
            
            TournamentHub.UpdateEntrants();

            return participants;
        }
            
        private static IEnumerable<(Equipment equipment, IEnumerable<EquipmentType> types)> GetAllTournamentEquipment()
        {
            return HeroHelpers.AllCultures.SelectMany(c => 
                    (c.TournamentTeamTemplatesForOneParticipant ?? Enumerable.Empty<CharacterObject>())
                        .Concat(c.TournamentTeamTemplatesForTwoParticipant ?? Enumerable.Empty<CharacterObject>())
                        .Concat(c.TournamentTeamTemplatesForFourParticipant ?? Enumerable.Empty<CharacterObject>()))
                .SelectMany(c => c.BattleEquipments ?? Enumerable.Empty<Equipment>())
                .Where(e => e != null)
                .Select(c => (
                    equipment: c,
                    types: c.YieldFilledWeaponSlots()
                        .Select(w => w.element.Item.GetEquipmentType())
                        .Where(e => e != EquipmentType.None)
                ));
        }

        private void GetTeamWeaponEquipmentListPostfixImpl(List<Equipment> equipments)
        {
            if (BLTAdoptAHeroModule.TournamentConfig.NoHorses)
            {
                foreach (var e in equipments)
                {
                    e[EquipmentIndex.Horse] = EquipmentElement.Invalid;
                    e[EquipmentIndex.HorseHarness] = EquipmentElement.Invalid;
                }
            }

            if (BLTAdoptAHeroModule.TournamentConfig.RandomizeWeaponTypes)
            {
                // Basic intention of this bit of code:
                // Each equipment set has a set of skills associated with it.
                // Each participant has a set of skills associated with their class.
                // Randomly select tournament equipment set weighted by how well it matches the participants skills.
                    
                var tournamentBehavior = MissionState.Current.CurrentMission.GetMissionBehaviour<TournamentBehavior>();
                    
                // Get all equipment sets, and their associated skills
                var availableEquipment = GetAllTournamentEquipment()
                    .Select(e => (
                        e.equipment,
                        skills: SkillGroup.GetSkills(SkillGroup.GetSkillsForEquipmentType(e.types).Distinct().ToList())))
                    // Exclude spears (defined as non-swingable polearms) if the config mandates it
                    .Where(e => !BLTAdoptAHeroModule.TournamentConfig.NoSpears 
                                || e.equipment.YieldWeaponSlots()
                                    .All(s => s.element.Item?.Type != ItemObject.ItemTypeEnum.Polearm 
                                              || s.element.Item.IsSwingable()))
                    .ToList();

                // Get the skill sets of the participating adopted heroes, by class
                var participantSkills = tournamentBehavior.CurrentMatch.Participants
                    // Get all the participating adopted heroes only
                    .Select(p => p.Character.HeroObject).Where(h => h?.IsAdopted() == true)
                    // Get the heroes associated class equipment
                    .Select(h => h.GetClass()?.WeaponSkills.ToList()).Where(s => s != null)
                    .ToList();

                // Select for each participant a random equipment set that closely matches theirs, then randomly select
                // from between those sets
                var tournamentSet = participantSkills
                    .Select(p => (
                        equipment: availableEquipment
                            .Shuffle()
                            // Ordering based on number of matching skills between the two sets, then by mismatching skills (quite rough...)
                            .OrderByDescending(e => e.skills.Intersect(p).Count() * 20 - e.skills.Except(p).Count())
                            .FirstOrDefault().equipment,
                        weight: 7f / participantSkills.Count)
                    )
                    // Add 2 random sets for some variety
                    .Concat(availableEquipment.Shuffle().Take(2).Select(e => (equipment: e.equipment, weight: 1f)))
                    // Add an unarmed set for some fun
                    .Concat((equipment: new Equipment(), weight: 0.5f).Yield())
                    // Select a random one
                    .SelectRandomWeighted(e => e.weight)
                    .equipment;

                MissionState.Current.CurrentMission
                    .GetMissionBehaviour<BLTTournamentSkillAdjustBehavior>()
                    .UnarmedRound = tournamentSet.IsEmpty();

                foreach (var e in equipments)
                {
                    foreach (var (_, index) in e.YieldWeaponSlots())
                    {
                        e[index] = tournamentSet[index];
                    }
                }
            }
            else if (BLTAdoptAHeroModule.TournamentConfig.NoSpears)
            {
                var replacementWeapon = HeroHelpers.AllItems
                    .FirstOrDefault(i => i.StringId == "empire_sword_1_t2_blunt");
                if (replacementWeapon != null)
                {
                    foreach (var e in equipments)
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
        }


        private bool AddRandomClothesPrefixImpl(CultureObject culture, TournamentParticipant participant)
        {
            if (BLTAdoptAHeroModule.TournamentConfig.NormalizeArmor)
            {
                var tier = (ItemObject.ItemTiers)Math.Max(0, Math.Min(5, BLTAdoptAHeroModule.TournamentConfig.NormalizeArmorTier - 1));
                var replacements = SkillGroup.ArmorIndexType
                    .Select(slotItemTypePair =>
                    (
                        slot: slotItemTypePair.slot, 
                        item: EquipHero.SelectRandomItemNearestTier(
                                  HeroHelpers.AllItems.Where(i 
                                      => i.Culture == culture && i.ItemType == slotItemTypePair.itemType), (int)tier)
                              ?? EquipHero.SelectRandomItemNearestTier(HeroHelpers.AllItems.Where(i => i.ItemType == slotItemTypePair.itemType), (int)tier)
                    )).ToList();
                    
                foreach (var (slot, item) in replacements)
                {
                    participant.MatchEquipment[slot] = new(item);
                }

                return true;
            }

            return false;
        }
                
        #region BLTBetMissionBehavior

        #endregion

        public void PrepareForTournamentGame()
        {
            var tournamentBehavior = MissionState.Current.CurrentMission.GetMissionBehaviour<TournamentBehavior>();
                
            tournamentBehavior.TournamentEnd += () =>
            {
                // Win results, put winner last
                foreach (var entry in activeTournament
                    .OrderBy(e => e.Hero == tournamentBehavior.Winner.Character?.HeroObject)
                )
                {
                    float actualBoost = entry.IsSub ? Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1) : 1;
                    var results = new List<string>();
                    if (entry.Hero != null && entry.Hero == tournamentBehavior.Winner.Character?.HeroObject)
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
                            // Shouldn't really happen!
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

        #region Custom Prize Generation
        private static ItemObject CreateCustomWeapon(Hero hero, HeroClassDef heroClass, EquipmentType weaponType)
        {
            if (!CustomItems.CraftableEquipmentTypes.Contains(weaponType))
            {
                // Get the highest tier we can for the weapon type
                var item = EquipHero.FindRandomTieredEquipment(5, hero, 
                    heroClass?.Mounted == true || !hero.BattleEquipment.Horse.IsEmpty, 
                    EquipHero.FindFlags.IgnoreAbility,
                    o => o.IsEquipmentType(weaponType));
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
                    var (item, modifier, _) = GeneratePrizeType(prizeType, 6, Hero.MainHero, classDef, allowDuplicates: true);
                    
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
                
            GameStateManager.Current?.UpdateInventoryUI();

            // if (GameStateManager.Current.ActiveState is InventoryState inventoryState)
            // {
            //     inventoryState.InventoryLogic?.Reset();
            // }

            return "done";
        }
#endif
            
        private static (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GeneratePrizeType(
            GlobalTournamentConfig.PrizeType prizeType, int tier, Hero hero, HeroClassDef heroClass, 
            bool allowDuplicates)
        {
            return prizeType switch
            {
                GlobalTournamentConfig.PrizeType.Weapon 
                    => GeneratePrizeTypeWeapon(tier, hero, heroClass, allowDuplicates),
                GlobalTournamentConfig.PrizeType.Armor 
                    => GeneratePrizeTypeArmor(tier, hero, heroClass, allowDuplicates),
                GlobalTournamentConfig.PrizeType.Mount 
                    => GeneratePrizeTypeMount(tier, hero, heroClass, allowDuplicates),
                _ => throw new ArgumentOutOfRangeException(nameof(prizeType), prizeType, null)
            };
        }

        private static (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GeneratePrizeTypeWeapon(
            int tier, Hero hero, HeroClassDef heroClass, bool allowDuplicateTypes)
        {
            // List of heroes custom items, so we can avoid giving duplicates (it will include what they are carrying,
            // as all custom items are registered)
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


            // Weapon classes we can generate a prize for, with some heuristics to avoid some edge cases, and getting
            // duplicates
            var weaponClasses = 
                (heroClass?.IndexedWeapons ?? replaceableHeroWeapons)
                .Where(s =>
                    // No shields, they aren't cool rewards and don't support any modifiers
                    s.type != EquipmentType.Shield
                    // Exclude bolts if hero doesn't have a crossbow already
                    && (s.type != EquipmentType.Bolts || heroWeapons.Any(i 
                        => i.element.Item.WeaponComponent?.PrimaryWeapon?.AmmoClass == WeaponClass.Bolt))
                    // Exclude arrows if hero doesn't have a bow
                    && (s.type != EquipmentType.Arrows || heroWeapons.Any(i 
                        => i.element.Item.WeaponComponent?.PrimaryWeapon?.AmmoClass == WeaponClass.Arrow))
                    // Exclude any weapons we already have enough custom versions of (if we have class then we can
                    // match the class count, otherwise we just limit it to 1), unless we are allowing duplicates
                    && (allowDuplicateTypes 
                        || heroCustomWeapons.Count(i => i.Item.IsEquipmentType(s.type)) 
                        < (heroClass?.Weapons.Count(w => w == s.type) ?? 1))
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
                        item: EquipHero.FindRandomTieredEquipment(tier, hero, 
                            heroClass?.Mounted == true || !hero.BattleEquipment.Horse.IsEmpty, 
                            EquipHero.FindFlags.IgnoreAbility | EquipHero.FindFlags.RequireExactTier, 
                            i => i.IsEquipmentType(c.type)),
                        index: c.index))
                    .FirstOrDefault(w => w.item != null);
                return item == null || hero.BattleEquipment[index].Item?.Tier >= item.Tier
                    ? default 
                    : (item, null, index);
            }
        }

        private static (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GeneratePrizeTypeArmor(int tier,
            Hero hero, HeroClassDef heroClass, bool allowDuplicateTypes)
        {
            // List of custom items the hero already has, and armor they are wearing that is as good or better than
            // the tier we want 
            var heroBetterArmor = BLTAdoptAHeroCampaignBehavior.Current
                .GetCustomItems(hero)
                .Concat(hero.BattleEquipment.YieldFilledArmorSlots()
                    .Where(e => (int)e.Item.Tier >= tier));

            // Select randomly from the various armor types we can choose between
            var (index, itemType) = SkillGroup.ArmorIndexType
                // Exclude any armors we already have an equal or better version of, unless we are allowing duplicates
                .Where(i => allowDuplicateTypes 
                            || heroBetterArmor.All(i2 => i2.Item.ItemType != i.itemType))
                .SelectRandom();

            if (index == default)
            {
                return default;
            }
                    
            // Custom "modified" item
            if (tier > 5)
            {
                var armor = EquipHero.FindRandomTieredEquipment(5, hero, 
                    heroClass?.Mounted == true || !hero.BattleEquipment.Horse.IsEmpty, 
                    EquipHero.FindFlags.IgnoreAbility,
                    o => o.ItemType == itemType);
                return armor == null ? default : (armor, GenerateItemModifier(armor, "Prize"), index);
            }
            else
            {
                var armor = EquipHero.FindRandomTieredEquipment(tier, hero, 
                    heroClass?.Mounted == true || !hero.BattleEquipment.Horse.IsEmpty, 
                    EquipHero.FindFlags.IgnoreAbility | EquipHero.FindFlags.RequireExactTier,
                    o => o.ItemType == itemType);
                // if no armor was found, or its the same tier as what we have then return null
                return armor == null || hero.BattleEquipment.YieldFilledArmorSlots()
                    .Any(i2 => i2.Item.Type == armor.Type && i2.Item.Tier >= armor.Tier) 
                    ? default 
                    : (armor, null, index);
            }
        }

        private static (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GeneratePrizeTypeMount(
            int tier, Hero hero, HeroClassDef heroClass, bool allowDuplicates)
        {
            var currentMount = hero.BattleEquipment.Horse;
            // If we are generating is non custom prize, and the hero has a non custom mount already,
            // of equal or better tier, we don't replace it
            if (tier <= 5 && !currentMount.IsEmpty && (int) currentMount.Item.Tier >= tier)
            {
                return default;
            }

            // If the hero has a custom mount already, then we don't give them another, or any non custom one,
            // unless we are allowing duplicates
            if (!allowDuplicates 
                && BLTAdoptAHeroCampaignBehavior.Current.GetCustomItems(hero)
                .Any(i => i.Item.ItemType == ItemObject.ItemTypeEnum.Horse))
            {
                return default;
            }
                    
            bool IsCorrectMountFamily(ItemObject item)
            {  
                // Must match hero class requirements
                return (heroClass == null
                        || heroClass.UseHorse && item.HorseComponent.Monster.FamilyType 
                            is (int) EquipHero.MountFamilyType.horse 
                        || heroClass.UseCamel && item.HorseComponent.Monster.FamilyType 
                            is (int) EquipHero.MountFamilyType.camel)
                       // Must also not differ from current mount family type (or saddle can get messed up)
                       && (currentMount.IsEmpty 
                           || currentMount.Item.HorseComponent.Monster.FamilyType 
                           == item.HorseComponent.Monster.FamilyType
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

            (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GenerateRandomWeightedPrize(bool allowDuplicates)
            {
                return BLTAdoptAHeroModule.TournamentConfig.PrizeTypeWeights
                    // Exclude mount when it shouldn't be used by the hero or they already have a tournament reward horse
                    .Where(p => shouldUseHorse || p.type != GlobalTournamentConfig.PrizeType.Mount)
                    // Randomize the reward type order, by random weighting
                    .OrderRandomWeighted(type => type.weight)
                    .SelectMany(type => 
                        tiers.Select(tier 
                            => GeneratePrizeType(type.type, tier.tier, hero, heroClass, allowDuplicates)))
                    .FirstOrDefault(i => i != default);
            }

            var prize = GenerateRandomWeightedPrize(allowDuplicates: false);

            // If we couldn't find a unique one that the hero can use, then generate a non-unique one, they can
            // sell it if they want
            if (prize == default)
            {
                prize = GenerateRandomWeightedPrize(allowDuplicates: true);
            }

            return prize;
        }

        #endregion

        private void EndCurrentMatchPrefixImpl(TournamentBehavior tournamentBehavior)
        {
            // If the tournament is over we need to make sure player gets the real prize. 
            // Need to do this before EndCurrentMatch, as the player gets the prize in this function.
            if (tournamentBehavior.CurrentRoundIndex == 3)
            {
                // Reset the prize if the player won
                if (originalPrize != null
                    && tournamentBehavior.CurrentMatch.IsPlayerWinner())
                {
                    SetPrize(tournamentBehavior.TournamentGame, originalPrize);
                }
            }
        }

        private void EndCurrentMatchPostfixImpl(TournamentBehavior tournamentBehavior)
        {
            BLTTournamentBetMissionBehavior.Current?.CompleteBetting(tournamentBehavior.LastMatch);

            if(tournamentBehavior.CurrentMatch != null)
            {
                BLTTournamentBetMissionBehavior.Current?.OpenBetting(tournamentBehavior);
            }
                
            int lastRoundIndex = tournamentBehavior.CurrentMatch == null ? 3 : tournamentBehavior.CurrentRoundIndex - 1;
            var rewards = BLTAdoptAHeroModule.TournamentConfig.RoundRewards[
                // Better safe than sorry, maybe some mod will add more rounds
                Math.Max(0, Math.Min(lastRoundIndex, BLTAdoptAHeroModule.TournamentConfig.RoundRewards.Length - 1))
            ];
                
            // End round effects (as there is no event handler for it :/)
            foreach (var entry in activeTournament)
            {
                float actualBoost = entry.IsSub ? Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1) : 1;
                        
                var results = new List<string>();
                if(tournamentBehavior.LastMatch.Winners.Any(w => w.Character?.HeroObject == entry.Hero))
                {
                    int actualGold = (int) (rewards.WinGold * actualBoost);
                    if (actualGold > 0)
                    {
                        BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(entry.Hero, actualGold);
                        results.Add($"{Naming.Inc}{actualGold}{Naming.Gold}");
                    }
                    int xp = (int) (rewards.WinXP * actualBoost);
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
                else if (tournamentBehavior.LastMatch.Participants.Any(w => w.Character?.HeroObject == entry.Hero))
                {
                    int xp = (int) (rewards.LoseXP * actualBoost);
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

        private ItemObject originalPrize;
            
        private void SetPlaceholderPrize(TournamentGame tournamentGame)
        {
            originalPrize = tournamentGame.Prize;
            SetPrize(tournamentGame, DefaultItems.Charcoal);
        }

        private static void SetPrize(TournamentGame tournamentGame, ItemObject prize)
        {
            AccessTools.Property(typeof(TournamentGame), nameof(TournamentGame.Prize))
                .SetValue(tournamentGame, prize);
        }
        
        #region Patches

        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentFightMissionController), "GetTeamWeaponEquipmentList")]
        public static void GetTeamWeaponEquipmentListPostfix(List<Equipment> __result)
        {
            SafeCallStatic(() => Current?.GetTeamWeaponEquipmentListPostfixImpl(__result));
        }
        
        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(TournamentFightMissionController),
             "AddRandomClothes")]
        public static bool AddRandomClothesPrefix(CultureObject culture, TournamentParticipant participant)
        {
            // Harmony Prefix should return false to skip the original function
            return SafeCallStatic(() => Current?.AddRandomClothesPrefixImpl(culture, participant) != true, true);
        }
        
        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(TournamentBehavior), "EndCurrentMatch")]
        public static void EndCurrentMatchPrefix(TournamentBehavior __instance)
        {
            SafeCallStatic(() => Current?.EndCurrentMatchPrefixImpl(__instance));
        }
        
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentBehavior), "EndCurrentMatch")]
        public static void EndCurrentMatchPostfix(TournamentBehavior __instance)
        {
            SafeCallStatic(() => Current?.EndCurrentMatchPostfixImpl(__instance));
        }
        
        #endregion
    }
}