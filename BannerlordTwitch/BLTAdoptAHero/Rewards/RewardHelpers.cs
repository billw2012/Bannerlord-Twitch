using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Actions.Util;
using BLTAdoptAHero.Annotations;
using BLTAdoptAHero.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BLTAdoptAHero
{
    public static class RewardHelpers
    {
        public enum RewardType
        {
            Weapon,
            Armor,
            Mount
        }
            
        public static (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GenerateRewardType(
            RewardType rewardType, int tier, Hero hero, HeroClassDef heroClass, 
            bool allowDuplicates, RandomItemModifierDef modifierDef, string customItemName, float customItemPower)
        {
            return rewardType switch
            {
                RewardType.Weapon => GenerateRewardTypeWeapon(tier, hero, heroClass, allowDuplicates, modifierDef, customItemName, customItemPower),
                RewardType.Armor => GenerateRewardTypeArmor(tier, hero, heroClass, allowDuplicates, modifierDef, customItemName, customItemPower),
                RewardType.Mount => GenerateRewardTypeMount(tier, hero, heroClass, allowDuplicates, modifierDef, customItemName, customItemPower),
                _ => throw new ArgumentOutOfRangeException(nameof(rewardType), rewardType, null)
            };
        }

        public static string AssignCustomReward(Hero hero, ItemObject item, ItemModifier itemModifier, EquipmentIndex slot)
        {
            var element = new EquipmentElement(item, itemModifier);
            bool isCustom = BLTCustomItemsCampaignBehavior.Current.IsRegistered(itemModifier);

            // We always put our custom items into the heroes storage, even if we won't use them right now
            if (isCustom)
            {
                BLTAdoptAHeroCampaignBehavior.Current.AddCustomItem(hero, element);
            }

            if (slot != EquipmentIndex.None)
            {
                hero.BattleEquipment[slot] = element;
                return $"received {element.GetModifiedItemName()}";
            }
            else if (!isCustom)
            {
                // Sell non-custom items
                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(hero, item.Value * 5);
                return $"sold {element.GetModifiedItemName()} for {item.Value}{Naming.Gold} (not needed)";
            }
            else
            {
                return $"received {element.GetModifiedItemName()} (put in storage)";
            }
        }

        private static (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GenerateRewardTypeWeapon(
            int tier, Hero hero, HeroClassDef heroClass, bool allowDuplicateTypes, RandomItemModifierDef modifierDef,
            string customItemName, float customItemPower)
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


            // Weapon classes we can generate a reward for, with some heuristics to avoid some edge cases, and getting
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
                        : (item, modifierDef.Generate(item, customItemName, customItemPower), index)
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

        private static (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GenerateRewardTypeArmor(int tier,
            Hero hero, HeroClassDef heroClass, bool allowDuplicateTypes, RandomItemModifierDef modifierDef, 
            string customItemName, float customItemPower)
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
                return armor == null ? default : (armor, modifierDef.Generate(armor, customItemName, customItemPower), index);
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

        private static (ItemObject item, ItemModifier modifier, EquipmentIndex slot) GenerateRewardTypeMount(
            int tier, Hero hero, HeroClassDef heroClass, bool allowDuplicates, RandomItemModifierDef modifierDef, 
            string customItemName, float customItemPower)
        {
            var currentMount = hero.BattleEquipment.Horse;
            // If we are generating is non custom reward, and the hero has a non custom mount already,
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
                ? modifierDef.Generate(mount, customItemName, customItemPower) 
                : null;
            return (mount, modifier, EquipmentIndex.Horse);
        }
        
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
        
    }
}