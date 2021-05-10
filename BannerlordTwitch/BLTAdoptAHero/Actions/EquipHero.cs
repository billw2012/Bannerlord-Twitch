using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Improve adopted heroes equipment")]
    internal class EquipHero : ActionHandlerBase
    {
        private struct Settings
        {
            [Description("Improve armor"), PropertyOrder(1)]
            public bool Armor { get; set; }
            [Description("Improve melee weapons (one handled, two handed, polearm). The one the player has the highest skill in will be selected."), PropertyOrder(2)]
            public bool Melee { get; set; }
            [Description("Improve ranged weapons (bow, crossbow, throwing). The one the player has the highest skill in will be selected."), PropertyOrder(3)]
            public bool Ranged { get; set; }
            [Description("Improve the heroes horse (if they can ride a better one)."), PropertyOrder(4)]
            public bool Horse { get; set; }
            [Description("Improve the heroes civilian equipment."), PropertyOrder(5)]
            public bool Civilian { get; set; }
            [Description("Allow improvement of adopted heroes who are also companions of the player."), PropertyOrder(6)]
            public bool AllowCompanionUpgrade { get; set; }
            [Description("Tier to upgrade to (0 to 5). Anything better than this tier will be left alone, viewer will be refunded if nothing could be upgraded. Not compatible with Upgrade."), PropertyOrder(7)]
            public int? Tier { get; set; } // 0 to 5
            [Description("Upgrade to the next tier from the current one, viewer will be refunded if nothing could be upgraded. Not compatible with Tier."), PropertyOrder(8)]
            public bool Upgrade { get; set; }
            [Description("Gold cost to the adopted hero"), PropertyOrder(9)]
            public int GoldCost { get; set; }
            [Description("Whether to multiply the cost by the current tier"), PropertyOrder(10)]
            public bool MultiplyCostByCurrentTier { get; set; }
        }

        protected override Type ConfigType => typeof(Settings);
        
        public static int GetHeroEquipmentTier(Hero hero) =>
            // The Mode of the tiers of the equipment
            hero.BattleEquipment.YieldEquipmentSlots().Concat(hero.CivilianEquipment.YieldEquipmentSlots())
                .Select(s => s.element.Item)
                .Where(i => i != null)
                .Select(i => (int)i.Tier)
                .GroupBy(v => v)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

        protected override void ExecuteInternal(ReplyContext context, object config,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            var settings = (Settings)config;
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            if (!settings.AllowCompanionUpgrade && adoptedHero.IsPlayerCompanion)
            {
                onFailure($"You are a player companion, you cannot change your own equipment!");
                return;
            }
            if (Mission.Current != null)
            {
                onFailure($"You cannot upgrade equipment, as a mission is active!");
                return;
            }
            if (!settings.Tier.HasValue && !settings.Upgrade)
            {
                onFailure($"Configuration is invalid, either Tier or Upgrade must be specified");
                return;
            }
            int targetTier = settings.Upgrade 
                    ? GetHeroEquipmentTier(adoptedHero) + 1 
                    : settings.Tier.Value
                ;
            
            if (targetTier > 5)
            {
                onFailure($"You cannot upgrade any further!");
                return;
            }

            int cost = settings.MultiplyCostByCurrentTier
                ? settings.GoldCost * targetTier
                : settings.GoldCost;

            int availableGold = BLTAdoptAHeroCampaignBehavior.Get().GetHeroGold(adoptedHero);
            if (availableGold < cost)
            {
                onFailure($"You do not have enough gold: you need {cost}, and you only have {availableGold}!");
                return;
            }

            var itemsPurchased = UpgradeEquipment(adoptedHero, targetTier, settings.Melee, settings.Ranged, settings.Armor, settings.Horse, settings.Civilian);

            if (!itemsPurchased.Any())
            {
                onFailure($"Couldn't find any items to upgrade!");
                return;
            }
            BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(adoptedHero, -cost);
            //string itemsStr = string.Join(", ", itemsPurchased.Select(i => i.Name.ToString()));
            // $"You purchased these items: {itemsStr}!"
            onSuccess($"Equip Tier {targetTier + 1}");
        }

        internal static void RemoveAllEquipment(Hero adoptedHero)
        {
            foreach (var slot in adoptedHero.BattleEquipment.YieldEquipmentSlots())
            {
                adoptedHero.BattleEquipment[slot.index] = EquipmentElement.Invalid;
            }
            foreach (var slot in adoptedHero.CivilianEquipment.YieldEquipmentSlots())
            {
                adoptedHero.CivilianEquipment[slot.index] = EquipmentElement.Invalid;
            }
        }
        
        internal static List<ItemObject> UpgradeEquipment(Hero adoptedHero, int targetTier, bool upgradeMelee, bool upgradeRanged, bool upgradeArmor, bool upgradeHorse, bool upgradeCivilian)
        {
            var itemsPurchased = new List<ItemObject>();

            if (upgradeMelee)
            {
                // We want to be left with only one melee weapon of the appropriate skill, of the highest tier, then we will 
                // try and upgrade it
                var highestSkill = SkillGroup.MeleeSkills.OrderByDescending(s => adoptedHero.GetSkillValue(s)).First();

                var newWeapon = UpgradeWeapon(highestSkill, SkillGroup.MeleeSkills, SkillGroup.MeleeItems, EquipmentIndex.Weapon0, adoptedHero,
                    adoptedHero.BattleEquipment, targetTier);
                if (newWeapon != null)
                {
                    itemsPurchased.Add(newWeapon);
                }

                var shieldSlots = adoptedHero.BattleEquipment
                    .YieldWeaponSlots()
                    .Where(e => e.element.Item?.Type == ItemObject.ItemTypeEnum.Shield)
                    .ToList();

                if (highestSkill == DefaultSkills.OneHanded)
                {
                    var (element, index) =
                        !shieldSlots.Any() ? FindEmptyWeaponSlot(adoptedHero.BattleEquipment) : shieldSlots.First();
                    if (index == EquipmentIndex.None)
                        index = EquipmentIndex.Weapon1;

                    if (element.Item == null || element.Item.Tier < (ItemObject.ItemTiers) targetTier)
                    {
                        var shield = FindRandomTieredEquipment(DefaultSkills.OneHanded, targetTier, adoptedHero,
                            null, ItemObject.ItemTypeEnum.Shield);
                        if (shield != null)
                        {
                            adoptedHero.BattleEquipment[index] = new EquipmentElement(shield);
                            itemsPurchased.Add(shield);
                        }
                    }
                }
            }

            if (upgradeRanged)
            {
                // We want to be left with only one weapon of the appropriate skill, of the highest tier, then we will 
                // try and upgrade it
                var highestSkill = SkillGroup.RangedSkills.OrderByDescending(s => adoptedHero.GetSkillValue(s)).First();

                var weapon = UpgradeWeapon(highestSkill, SkillGroup.RangedSkills, SkillGroup.RangedItems, EquipmentIndex.Weapon3, adoptedHero,
                    adoptedHero.BattleEquipment, targetTier);

                if (weapon?.Type == ItemObject.ItemTypeEnum.Thrown)
                {
                    // add more to free slots
                    var (_, index) = FindEmptyWeaponSlot(adoptedHero.BattleEquipment);
                    if (index != EquipmentIndex.None)
                    {
                        adoptedHero.BattleEquipment[index] = new EquipmentElement(weapon);
                    }
                }
                else if (weapon?.Type is ItemObject.ItemTypeEnum.Bow or ItemObject.ItemTypeEnum.Crossbow)
                {
                    var ammoType = ItemObject.GetAmmoTypeForItemType(weapon.Type);
                    var arrowSlots = adoptedHero.BattleEquipment
                        .YieldWeaponSlots()
                        .Where(e => e.element.Item?.Type == ammoType)
                        .ToList();
                    var (slot, index) = !arrowSlots.Any() ? FindEmptyWeaponSlot(adoptedHero.BattleEquipment) : arrowSlots.First();
                    if (index == EquipmentIndex.None)
                        index = EquipmentIndex.Weapon3;
                    if (slot.Item == null || slot.Item.Tier < (ItemObject.ItemTiers) targetTier)
                    {
                        var ammo = FindRandomTieredEquipment(null, targetTier, adoptedHero, null, ammoType);
                        if (ammo != null)
                        {
                            adoptedHero.BattleEquipment[index] = new EquipmentElement(ammo);
                            itemsPurchased.Add(ammo);
                        }
                    }
                }
            }

            if (upgradeArmor)
            {
                foreach (var (index, itemType) in SkillGroup.ArmorIndexType)
                {
                    var newItem = UpgradeItemInSlot(index, itemType, targetTier, adoptedHero.BattleEquipment, adoptedHero);
                    if (newItem != null) itemsPurchased.Add(newItem);
                }
            }

            if (upgradeHorse)
            {
                var newHorse = UpgradeItemInSlot(EquipmentIndex.Horse, ItemObject.ItemTypeEnum.Horse, targetTier,
                    adoptedHero.BattleEquipment, adoptedHero, h => h.HorseComponent?.IsMount == true );
                if (newHorse != null) itemsPurchased.Add(newHorse);
                
                var horse = adoptedHero.BattleEquipment[EquipmentIndex.Horse];
                if (!horse.IsEmpty)
                {
                    int horseType = horse.Item.HorseComponent.Monster.FamilyType;
                    var newHarness = UpgradeItemInSlot(EquipmentIndex.HorseHarness,
                        ItemObject.ItemTypeEnum.HorseHarness,
                        targetTier, adoptedHero.BattleEquipment, adoptedHero,
                        h => horseType == h.ArmorComponent?.FamilyType);
                    if (newHarness != null) itemsPurchased.Add(newHarness);
                }
            }

            if (upgradeCivilian)
            {
                foreach (var (index, itemType) in SkillGroup.ArmorIndexType)
                {
                    var newItem = UpgradeItemInSlot(index, itemType, targetTier, adoptedHero.CivilianEquipment, adoptedHero,
                        o => o.IsCivilian);
                    if (newItem != null) itemsPurchased.Add(newItem);
                }

                var upgradeSlot = adoptedHero.CivilianEquipment.YieldWeaponSlots().FirstOrDefault(s => !s.element.IsEmpty);
                if (upgradeSlot.element.IsEmpty)
                    upgradeSlot = FindEmptyWeaponSlot(adoptedHero.CivilianEquipment);

                UpgradeItemInSlot(upgradeSlot.index, ItemObject.ItemTypeEnum.OneHandedWeapon, targetTier,
                    adoptedHero.CivilianEquipment, adoptedHero);
            }

            return itemsPurchased;
        }

        private static ItemObject UpgradeItemInSlot(EquipmentIndex equipmentIndex, ItemObject.ItemTypeEnum itemTypeEnum, int tier, Equipment equipment, Hero hero, Func<ItemObject, bool> filter = null)
        {
            var slot = equipment[equipmentIndex];
            if (slot.Item == null || slot.Item.Tier < (ItemObject.ItemTiers) tier)
            {
                var item = FindRandomTieredEquipment(null, tier, hero, filter, itemTypeEnum);
                if (item != null && (slot.Item == null || slot.Item.Tier < item.Tier))
                {
                    equipment[equipmentIndex] = new EquipmentElement(item);
                    return item;
                }
            }

            return null;
        }

        private static ItemObject UpgradeWeapon(SkillObject skill, SkillObject[] skillGroup, ItemObject.ItemTypeEnum[] itemTypeEnums, EquipmentIndex defaultEquipmentIndex, Hero hero, Equipment equipment, int tier, Func<ItemObject, bool> filter = null)
        {
            // Remove all non-skill matching weapons
            RemoveNonBestSkillItems(skillGroup, skill, equipment, itemTypeEnums);

            // Remove all but the *best* matching weapon
            RemoveNonBestMatchingWeapons(skill, equipment, itemTypeEnums);

            // Get slot of correct skill weapon we can replace  
            var weaponSlots = GetMatchingItems(skill, equipment, itemTypeEnums);

            // If there isn't one then find an empty slot
            var (element, index) = !weaponSlots.Any()
                ? FindEmptyWeaponSlot(equipment)
                : weaponSlots.First();

            if (index == EquipmentIndex.None)
            {
                // We will just replace the first weapon if we can't find any slot (shouldn't happen)
                index = defaultEquipmentIndex;
            }

            if (element.Item == null || element.Item.Tier < (ItemObject.ItemTiers) tier)
            {
                var newWeapon = FindRandomTieredEquipment(skill, tier, hero, filter, itemTypeEnums);
                if (newWeapon != null)
                {
                    equipment[index] = new EquipmentElement(newWeapon);
                    return newWeapon;
                }
            }

            return element.Item;
        }

        private static ItemObject FindRandomTieredEquipment(SkillObject skill, int tier, Hero hero, Func<ItemObject, bool> filter = null, params ItemObject.ItemTypeEnum[] itemTypeEnums)
        {
            var items = ItemObject.All
                // Usable
                .Where(item => !item.NotMerchandise && CanUseItem(item, hero) && (filter == null || filter(item)))
                // Correct type
                .Where(item => itemTypeEnums.Contains(item.Type))
                // Correct skill
                .Where(item => skill == null || item.RelevantSkill == skill)
                .ToList();

            // Correct tier
            var tieredItems = items.Where(item => (int) item.Tier == tier).ToList();

            // We might not find an item at the specified tier, so find the closest tier we can
            while (!tieredItems.Any() && tier >= 0)
            {
                tier--;
                tieredItems = items.Where(item => (int) item.Tier == tier).ToList();
            }

            return tieredItems.SelectRandom();
        }

        private static (EquipmentElement element, EquipmentIndex index) FindEmptyWeaponSlot(Equipment equipment)
        {
            var emptySlots = FindAllEmptyWeaponSlots(equipment);
            return emptySlots.Any() ? emptySlots.First() : (EquipmentElement.Invalid, EquipmentIndex.None);
        }

        private static List<(EquipmentElement element, EquipmentIndex index)> FindAllEmptyWeaponSlots(Equipment equipment)
        {
            return equipment.YieldWeaponSlots()
                .Where(e => e.element.IsEmpty)
                .ToList();
        }

        private static void RemoveNonBestMatchingWeapons(SkillObject skillObject, Equipment equipment, params ItemObject.ItemTypeEnum[] itemTypeEnums)
        {
            foreach (var x in GetMatchingItems(skillObject, equipment, itemTypeEnums)
                // Highest tier first
                .OrderByDescending(e => e.element.Item.Tier)
                .Skip(1)
                .ToList())
            {
                equipment[x.index] = EquipmentElement.Invalid;
            }
        }

        private static void RemoveNonBestSkillItems(IEnumerable<SkillObject> skills, SkillObject bestSkill, Equipment equipment, params ItemObject.ItemTypeEnum[] itemTypeEnums)
        {
            foreach (var x in equipment.YieldWeaponSlots()
                .Where(e => !e.element.IsEmpty)
                // Correct type
                .Where(e => itemTypeEnums.Contains(e.element.Item.Type))
                .Where(e => skills.Contains(e.element.Item.RelevantSkill) && e.element.Item.RelevantSkill != bestSkill)
                .ToList())
            {
                equipment[x.index] = EquipmentElement.Invalid;
            }
        }

        private static List<(EquipmentElement element, EquipmentIndex index)> GetMatchingItems(SkillObject skill, Equipment equipment, params ItemObject.ItemTypeEnum[] itemTypeEnums)
        {
            return equipment.YieldWeaponSlots()
                .Where(e => !e.element.IsEmpty)
                .Where(e => itemTypeEnums.Contains(e.element.Item.Type))
                .Where(e => e.element.Item.RelevantSkill == skill)
                .ToList();
        }

        public static bool CanUseItem(ItemObject item, Hero hero)
        {
            var relevantSkill = item.RelevantSkill;
            return (relevantSkill == null || hero.GetSkillValue(relevantSkill) >= item.Difficulty) && (!hero.IsFemale || !item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByFemale)) && (hero.IsFemale || !item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale));
        }
    }
}