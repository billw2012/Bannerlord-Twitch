using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Actions.Util;
using Helpers;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Improve adopted heroes equipment")]
    internal class EquipHero : ActionHandlerBase
    {
        private class Settings
        {
            [Description("Allow improvement of adopted heroes who are also companions of the player."), PropertyOrder(6)]
            public bool AllowCompanionUpgrade { get; set; } = true;
            [Description("Gold cost for Tier 1 equipment"), PropertyOrder(1)]
            public int CostTier1 { get; set; } = 25000;

            [Description("Gold cost for Tier 2 equipment"), PropertyOrder(2)]
            public int CostTier2 { get; set; } = 50000;

            [Description("Gold cost for Tier 3 equipment"), PropertyOrder(3)]
            public int CostTier3 { get; set; } = 100000;

            [Description("Gold cost for Tier 4 equipment"), PropertyOrder(4)]
            public int CostTier4 { get; set; } = 175000;

            [Description("Gold cost for Tier 5 equipment"), PropertyOrder(5)]
            public int CostTier5 { get; set; } = 275000;

            [Description("Gold cost for Tier 6 equipment"), PropertyOrder(6)]
            public int CostTier6 { get; set; } = 400000;
            
            // etc..
            
            public int GetTierCost(int tier)
            {
                return tier switch
                {
                    0 => CostTier1,
                    1 => CostTier2,
                    2 => CostTier3,
                    3 => CostTier4,
                    4 => CostTier5,
                    5 => CostTier6,
                    _ => 0
                };
            }

            [Description("Whether to multiply the cost by the current tier"), PropertyOrder(10)]
            public bool MultiplyCostByCurrentTier { get; set; } = true;
            
            [Description("Whether to re-equip the equipment INSTEAD of upgrading it (you should make TWO commands, one to upgrade and one to reequip)"), PropertyOrder(11)]
            public bool ReequipInsteadOfUpgrade { get; set; } = false;
        }

        protected override Type ConfigType => typeof(Settings);

        protected override void ExecuteInternal(ReplyContext context, object config,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            var settings = (Settings)config;
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(AdoptAHero.NoHeroMessage);
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

            int targetTier = Math.Max(0, BLTAdoptAHeroCampaignBehavior.Current.GetEquipmentTier(adoptedHero) +
                             (settings.ReequipInsteadOfUpgrade ? 0 : 1));
            
            if (targetTier > 5)
            {
                onFailure($"You cannot upgrade any further!");
                return;
            }
            
            int cost = settings.GetTierCost(targetTier);

            int availableGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(adoptedHero);
            if (availableGold < cost)
            {
                onFailure(Naming.NotEnoughGold(cost, availableGold));
                return;
            }

            var charClass = BLTAdoptAHeroCampaignBehavior.Current.GetClass(adoptedHero);

            UpgradeEquipment(adoptedHero, targetTier, charClass, !settings.ReequipInsteadOfUpgrade);

            BLTAdoptAHeroCampaignBehavior.Current.SetEquipmentTier(adoptedHero, targetTier);
            BLTAdoptAHeroCampaignBehavior.Current.SetEquipmentClass(adoptedHero, charClass);
            BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(adoptedHero, -cost, isSpending: true);

            onSuccess(settings.ReequipInsteadOfUpgrade
                ? $"Re-equipped Tier {targetTier + 1} ({charClass?.Name ?? "No Class"})"
                : $"Equipped Tier {targetTier + 1} ({charClass?.Name ?? "No Class"})");
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
        
        public static int CalculateHeroEquipmentTier(Hero hero) =>
            // The Mode of the tiers of the equipment
            hero.BattleEquipment.YieldEquipmentSlots()
                .Where(s => s.index is >= EquipmentIndex.ArmorItemBeginSlot and < EquipmentIndex.ArmorItemEndSlot || s.element.Item != null)
                .Select(s => s.element.Item)
                .Select(i => i == null ? -1 : (int)i.Tier)
                .GroupBy(v => v)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?
                .Key ?? -1;
        
        // internal static List<ItemObject> UpgradeEquipment(Hero adoptedHero, int targetTier, bool upgradeMelee, bool upgradeRanged, bool upgradeArmor, bool upgradeHorse, bool upgradeCivilian)
        // {
        //     var itemsPurchased = new List<ItemObject>();
        //
        //     if (upgradeMelee)
        //     {
        //         itemsPurchased.AddRange(UpgradeMelee(adoptedHero, targetTier));
        //     }
        //
        //     if (upgradeRanged)
        //     {
        //         itemsPurchased.AddRange(UpgradeRanged(adoptedHero, targetTier));
        //     }
        //
        //     if (upgradeArmor)
        //     {
        //         itemsPurchased.AddRange(UpgradeArmor(adoptedHero, targetTier));
        //     }
        //
        //     if (upgradeHorse)
        //     {
        //         itemsPurchased.AddRange(UpgradeHorse(adoptedHero, targetTier));
        //     }
        //
        //     if (upgradeCivilian)
        //     {
        //         itemsPurchased.AddRange(UpgradeCivilian(adoptedHero, targetTier));
        //     }
        //
        //     return itemsPurchased;
        // }
        
        public static bool PrimaryWeaponFilter(ItemObject o, HeroClassDef classDef) =>
            o.Type is ItemObject.ItemTypeEnum.OneHandedWeapon or ItemObject.ItemTypeEnum.TwoHandedWeapon or ItemObject.ItemTypeEnum.Polearm or
                ItemObject.ItemTypeEnum.Bow or ItemObject.ItemTypeEnum.Crossbow or ItemObject.ItemTypeEnum.Thrown 
            && (classDef?.Mounted != true || o.PrimaryWeapon == null || !MBItem.GetItemUsageSetFlags(o.PrimaryWeapon.ItemUsage).HasFlag(ItemObject.ItemUsageSetFlags.RequiresNoMount))
        ;
        
        public static void UpgradeEquipment(Hero adoptedHero, int targetTier, HeroClassDef classDef, bool keepBetter)
        {
            var oldItems = adoptedHero.BattleEquipment.YieldEquipmentSlots()
                .Select(e => e.element.Item).Where(i => i != null).ToList();

            foreach (var x in adoptedHero.BattleEquipment.YieldWeaponSlots())
            {
                adoptedHero.BattleEquipment[x.index] = EquipmentElement.Invalid;
            }

            var currSlot = EquipmentIndex.WeaponItemBeginSlot;

            var addedWeapons = new List<ItemObject>();

            ItemObject FindNewEquipmentBySkill(SkillObject skill, Func<ItemObject, bool> filter = null) =>
                oldItems.FirstOrDefault(i => keepBetter && i.RelevantSkill == skill && i.Tier >= (ItemObject.ItemTiers) targetTier &&
                                             (filter == null || filter(i)) )
                ?? FindRandomTieredEquipment(skill, targetTier, adoptedHero, FindFlags.None, filter);

            ItemObject FindNewEquipmentByType(ItemObject.ItemTypeEnum itemType, Func<ItemObject, bool> filter = null) =>
                oldItems.FirstOrDefault(i => keepBetter && i.Type == itemType && i.Tier >= (ItemObject.ItemTiers) targetTier &&
                                             (filter == null || filter(i)))
                ?? FindRandomTieredEquipment(null, targetTier, adoptedHero, FindFlags.None, filter, itemType);
            
            ItemObject primaryAmmo = null;
            //var combatSkills = new[] { Skills.Polearm, Skills.TwoHanded, Skills.OneHanded, Skills.Bow, Skills.Crossbow, Skills.Throwing };
            var weaponSkills = classDef != null
                ? SkillGroup.SkillItemPairs.Where(s => classDef.Weapons.Any(sk => sk == s.itemType)).ToList()
                // Without class we just take the top skill only
                : SkillGroup.SkillItemPairs.OrderByDescending(s => adoptedHero.GetSkillValue(s.skill)).Take(1).ToList()
                ;


            foreach (var weapon in weaponSkills
                .Select(s => FindNewEquipmentBySkill(s.skill, e => PrimaryWeaponFilter(e, classDef)))
                .Where(e => e != null)
            )
            {
                var ammoType = ItemObject.GetAmmoTypeForItemType(weapon.Type);

                // We need at least 2 slots if the weapon requires ammo, so just skip if we don't have 2 left
                if (ammoType != weapon.Type && ammoType != ItemObject.ItemTypeEnum.Invalid && currSlot >= EquipmentIndex.Weapon3)
                    continue;
                
                adoptedHero.BattleEquipment[currSlot++] = new EquipmentElement(weapon);
                addedWeapons.Add(weapon);
                
                // Exit once we run out of weapon slots
                if (currSlot > EquipmentIndex.Weapon3)
                    break;
                
                // Add one ammo if we need it
                if (ammoType != weapon.Type && ammoType != ItemObject.ItemTypeEnum.Invalid)
                {
                    primaryAmmo = FindNewEquipmentByType(ammoType);
                    if (primaryAmmo != null)
                    {
                        adoptedHero.BattleEquipment[currSlot++] = new EquipmentElement(primaryAmmo);
                        if (currSlot > EquipmentIndex.Weapon3)
                            break;
                    }
                }
                else if (ammoType == weapon.Type)
                {
                    primaryAmmo = weapon;
                }
            }

            //bool WeaponNotRequires(ItemObject w, ItemObject.ItemUsageSetFlags flag) => w.Weapons.All(c => c.ItemUsage == null || !MBItem.GetItemUsageSetFlags(c.ItemUsage).HasFlag(flag));    
            
            // If we have space left and existing weapons don't support swinging, then add a weapon that does, appropriate to our skills
            if (currSlot <= EquipmentIndex.Weapon3 && !addedWeapons.Any(WeaponIsSwingable))
            {
                var weapon = SkillGroup.MeleeSkillItemPairs
                    .OrderByDescending(s => adoptedHero.GetSkillValue(s.skill))
                    .Select(s => 
                        FindNewEquipmentBySkill(
                            s.skill, 
                            o => PrimaryWeaponFilter(o, classDef) && WeaponIsSwingable(o)))
                    .FirstOrDefault(w => w != null);
                    ;
                if (weapon != null)
                {
                    adoptedHero.BattleEquipment[currSlot++] = new EquipmentElement(weapon);
                    addedWeapons.Add(weapon);
                }
            }
            
            // Add one more primary ammo
            if(currSlot <= EquipmentIndex.Weapon3 && primaryAmmo != null)
            {
                adoptedHero.BattleEquipment[currSlot++] = new EquipmentElement(primaryAmmo);
            }
            
            // If we have space left then add a shield if we have a 1H weapon that allows shield
            if (currSlot <= EquipmentIndex.Weapon3 
                && addedWeapons.Any(w => !WeaponRequires(w, ItemObject.ItemUsageSetFlags.RequiresNoShield)))
            {
                var shield = FindNewEquipmentByType(ItemObject.ItemTypeEnum.Shield);
                if (shield != null)
                {
                    adoptedHero.BattleEquipment[currSlot++] = new EquipmentElement(shield);
                }   
            }

            // Always want armor obviously
            UpgradeArmor(adoptedHero, targetTier, keepBetter);

            // We should assign a horse if using a class definition that specifies riding, OR 
            // if not using class definition and the riding skill is better than athletics, or polearm
            // is the top combat skill
            if (HeroShouldUseHorse(adoptedHero, classDef))
            {
                UpgradeHorse(adoptedHero, targetTier, keepBetter);
            }
            else
            {
                adoptedHero.BattleEquipment[EquipmentIndex.Horse] = EquipmentElement.Invalid;
                adoptedHero.BattleEquipment[EquipmentIndex.HorseHarness] = EquipmentElement.Invalid;
            }

            UpgradeCivilian(adoptedHero, targetTier, keepBetter);
        }

        public static bool HeroShouldUseHorse(Hero adoptedHero, HeroClassDef classDef)
        {
            var heroWeapons = adoptedHero.BattleEquipment.YieldFilledWeaponSlots().Select(e => e.Item).ToList();
            return classDef is {Mounted: true} 
                   || classDef == null
                   && (
                       // One of our weapons requires a mount (not sure this is actually a thing)
                       heroWeapons.Any(s => WeaponRequires(s, ItemObject.ItemUsageSetFlags.RequiresMount))
                       ||
                       // Any of our weapons *allows* a mount
                       !heroWeapons.All(s => WeaponRequires(s, ItemObject.ItemUsageSetFlags.RequiresNoMount))
                       // Either our riding skill is better than athletics or we have a thrust only polearm
                       && (adoptedHero.GetSkillValue(DefaultSkills.Riding) > adoptedHero.GetSkillValue(DefaultSkills.Athletics)
                           || heroWeapons.Any(s => s.Type == ItemObject.ItemTypeEnum.Polearm && !WeaponIsSwingable(s)))
                   );
        }

        public static bool WeaponIsSwingable(ItemObject w) => w.PrimaryWeapon?.IsMeleeWeapon == true && w.PrimaryWeapon?.SwingDamageType != DamageTypes.Invalid;

        public static bool WeaponRequires(ItemObject w, ItemObject.ItemUsageSetFlags flag) => w.PrimaryWeapon?.ItemUsage != null && MBItem.GetItemUsageSetFlags(w.PrimaryWeapon.ItemUsage).HasFlag(flag);

        // private static IEnumerable<ItemObject> UpgradeMelee(Hero adoptedHero, int targetTier)
        // {
        //     var itemsPurchased = new List<ItemObject>();
        //     
        //     // We want to be left with only one melee weapon of the appropriate skill, of the highest tier, then we will 
        //     // try and upgrade it
        //     var (highestSkill, newWeapon) = SkillGroup.MeleeSkills
        //         .OrderByDescending(adoptedHero.GetSkillValue)
        //         .Select(skill => (
        //             skill,
        //             weapon: UpgradeWeapon(skill,
        //                 SkillGroup.MeleeSkills, SkillGroup.MeleeItems, EquipmentIndex.Weapon0,
        //                 adoptedHero, adoptedHero.BattleEquipment, targetTier)))
        //         .FirstOrDefault(s => s.weapon != null);
        //
        //     if (newWeapon != null)
        //     {
        //         itemsPurchased.Add(newWeapon);
        //     }
        //
        //     var shieldSlots = adoptedHero.BattleEquipment
        //         .YieldWeaponSlots()
        //         .Where(e => e.element.Item?.Type == ItemObject.ItemTypeEnum.Shield)
        //         .ToList();
        //
        //     if (highestSkill == DefaultSkills.OneHanded)
        //     {
        //         var (element, index) =
        //             !shieldSlots.Any() ? FindEmptyWeaponSlot(adoptedHero.BattleEquipment) : shieldSlots.First();
        //         if (index == EquipmentIndex.None)
        //             index = EquipmentIndex.Weapon1;
        //
        //         if (element.Item == null || element.Item.Tier < (ItemObject.ItemTiers) targetTier)
        //         {
        //             var shield = FindRandomTieredEquipment(DefaultSkills.OneHanded, targetTier, adoptedHero,
        //                 null, ItemObject.ItemTypeEnum.Shield);
        //             if (shield != null)
        //             {
        //                 adoptedHero.BattleEquipment[index] = new EquipmentElement(shield);
        //                 itemsPurchased.Add(shield);
        //             }
        //         }
        //     }
        //
        //     return itemsPurchased;
        // }

        // private static IEnumerable<ItemObject> UpgradeRanged(Hero adoptedHero, int targetTier)
        // {
        //     var itemsPurchased = new List<ItemObject>();
        //     
        //     // We want to be left with only one weapon of the appropriate skill, of the highest tier, then we will 
        //     // try and upgrade it
        //     var highestSkill = SkillGroup.RangedSkills.OrderByDescending(s => adoptedHero.GetSkillValue(s)).First();
        //
        //     var weapon = UpgradeWeapon(highestSkill, SkillGroup.RangedSkills, SkillGroup.RangedItems, EquipmentIndex.Weapon3,
        //         adoptedHero,
        //         adoptedHero.BattleEquipment, targetTier);
        //
        //     if (weapon?.Type == ItemObject.ItemTypeEnum.Thrown)
        //     {
        //         // add more to free slots
        //         var (_, index) = FindEmptyWeaponSlot(adoptedHero.BattleEquipment);
        //         if (index != EquipmentIndex.None)
        //         {
        //             adoptedHero.BattleEquipment[index] = new EquipmentElement(weapon);
        //         }
        //     }
        //     else if (weapon?.Type is ItemObject.ItemTypeEnum.Bow or ItemObject.ItemTypeEnum.Crossbow)
        //     {
        //         var ammoType = ItemObject.GetAmmoTypeForItemType(weapon.Type);
        //         var arrowSlots = adoptedHero.BattleEquipment
        //             .YieldWeaponSlots()
        //             .Where(e => e.element.Item?.Type == ammoType)
        //             .ToList();
        //         var (slot, index) = !arrowSlots.Any() ? FindEmptyWeaponSlot(adoptedHero.BattleEquipment) : arrowSlots.First();
        //         if (index == EquipmentIndex.None)
        //             index = EquipmentIndex.Weapon3;
        //         if (slot.Item == null || slot.Item.Tier < (ItemObject.ItemTiers) targetTier)
        //         {
        //             var ammo = FindRandomTieredEquipment(null, targetTier, adoptedHero, null, ammoType);
        //             if (ammo != null)
        //             {
        //                 adoptedHero.BattleEquipment[index] = new EquipmentElement(ammo);
        //                 itemsPurchased.Add(ammo);
        //             }
        //         }
        //     }
        //
        //     return itemsPurchased;
        // }

        private static IEnumerable<ItemObject> UpgradeArmor(Hero adoptedHero, int targetTier, bool keepBetter)
        {
            var itemsPurchased = new List<ItemObject>();
            foreach (var (index, itemType) in SkillGroup.ArmorIndexType)
            {
                var newItem = UpgradeItemInSlot(index, itemType, targetTier, keepBetter, adoptedHero.BattleEquipment, adoptedHero);
                if (newItem != null) itemsPurchased.Add(newItem);
            }

            return itemsPurchased;
        }

        private static void UpgradeHorse(Hero adoptedHero, int targetTier, bool keepBetter)
        {
            UpgradeItemInSlot(EquipmentIndex.Horse, ItemObject.ItemTypeEnum.Horse, targetTier, keepBetter,
                adoptedHero.BattleEquipment, adoptedHero, h => h.HorseComponent?.IsMount == true);
            var horse = adoptedHero.BattleEquipment[EquipmentIndex.Horse];
            if (!horse.IsEmpty)
            {
                int horseType = horse.Item.HorseComponent.Monster.FamilyType;
                UpgradeItemInSlot(EquipmentIndex.HorseHarness,
                    ItemObject.ItemTypeEnum.HorseHarness,
                    targetTier, keepBetter, adoptedHero.BattleEquipment, adoptedHero,
                    h => horseType == h.ArmorComponent?.FamilyType);
            }
        }

        private static IEnumerable<ItemObject> UpgradeCivilian(Hero adoptedHero, int targetTier, bool keepBetter)
        {
            var itemsPurchased = new List<ItemObject>();
            foreach (var (index, itemType) in SkillGroup.ArmorIndexType)
            {
                var newItem = UpgradeItemInSlot(index, itemType, targetTier, keepBetter, adoptedHero.CivilianEquipment, adoptedHero,
                    o => o.IsCivilian);
                if (newItem != null) itemsPurchased.Add(newItem);
            }

            // Clear weapon slots beyond 0
            foreach (var x in adoptedHero.CivilianEquipment.YieldWeaponSlots().Skip(1))
            {
                adoptedHero.CivilianEquipment[x.index] = EquipmentElement.Invalid;
            }
            
            UpgradeItemInSlot(EquipmentIndex.Weapon0, ItemObject.ItemTypeEnum.OneHandedWeapon, targetTier, keepBetter,
                adoptedHero.CivilianEquipment, adoptedHero);

            return itemsPurchased;
        }

        private static ItemObject UpgradeItemInSlot(EquipmentIndex equipmentIndex, ItemObject.ItemTypeEnum itemTypeEnum, int tier, bool keepBetter, Equipment equipment, Hero hero, Func<ItemObject, bool> filter = null)
        {
            var slot = equipment[equipmentIndex];
            if (!keepBetter || slot.Item == null || slot.Item.Tier < (ItemObject.ItemTiers) tier)
            {
                var item = FindRandomTieredEquipment(null, tier, hero, FindFlags.None, filter, itemTypeEnum);
                if (item != null && (!keepBetter || slot.Item == null || slot.Item.Tier < item.Tier))
                {
                    equipment[equipmentIndex] = new EquipmentElement(item);
                    return item;
                }
            }

            return null;
        }

        // private static ItemObject UpgradeWeapon(SkillObject skill, SkillObject[] skillGroup, ItemObject.ItemTypeEnum[] itemTypeEnums, EquipmentIndex defaultEquipmentIndex, Hero hero, Equipment equipment, int tier, Func<ItemObject, bool> filter = null)
        // {
        //     // Remove all non-skill matching weapons
        //     RemoveNonBestSkillItems(skillGroup, skill, equipment, itemTypeEnums);
        //
        //     // Remove all but the *best* matching weapon
        //     RemoveNonBestMatchingWeapons(skill, equipment, itemTypeEnums);
        //
        //     // Get slot of correct skill weapon we can replace  
        //     var weaponSlots = GetMatchingItems(skill, equipment, itemTypeEnums);
        //
        //     // If there isn't one then find an empty slot
        //     var (element, index) = !weaponSlots.Any()
        //         ? FindEmptyWeaponSlot(equipment)
        //         : weaponSlots.First();
        //
        //     if (index == EquipmentIndex.None)
        //     {
        //         // We will just replace the first weapon if we can't find any slot (shouldn't happen)
        //         index = defaultEquipmentIndex;
        //     }
        //
        //     if (element.Item == null || element.Item.Tier < (ItemObject.ItemTiers) tier)
        //     {
        //         var newWeapon = FindRandomTieredEquipment(skill, tier, hero, filter, itemTypeEnums);
        //         if (newWeapon != null)
        //         {
        //             equipment[index] = new EquipmentElement(newWeapon);
        //             return newWeapon;
        //         }
        //     }
        //
        //     return element.Item;
        // }

        [Flags]
        public enum FindFlags
        {
            None = 0,
            IgnoreAbility = 1 << 0,
            AllowNonMerchandise = 1 << 1,
        }
        public static ItemObject FindRandomTieredEquipment(SkillObject skill, int tier, Hero hero, FindFlags flags = FindFlags.None, Func<ItemObject, bool> filter = null, params ItemObject.ItemTypeEnum[] itemTypeEnums)
        {
            var items =
                HeroHelpers.AllItems
                // Usable
                .Where(item => (!item.NotMerchandise || flags.HasFlag(FindFlags.AllowNonMerchandise))
                               && CanUseItem(item, hero, flags.HasFlag(FindFlags.IgnoreAbility))
                               && item.PrimaryWeapon?.WeaponClass != WeaponClass.Dagger
                               //&& (item.PrimaryWeapon == null || !MBItem.GetItemUsageSetFlags(item.PrimaryWeapon.ItemUsage).HasFlag(ItemObject.ItemUsageSetFlags.RequiresNoMount))
                               && (filter == null || filter(item)))
                // Correct type
                .Where(item => !itemTypeEnums.Any() || itemTypeEnums.Contains(item.Type))
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

        // private static (EquipmentElement element, EquipmentIndex index) FindEmptyWeaponSlot(Equipment equipment)
        // {
        //     var emptySlots = FindAllEmptyWeaponSlots(equipment);
        //     return emptySlots.Any() ? emptySlots.First() : (EquipmentElement.Invalid, EquipmentIndex.None);
        // }
        //
        // private static List<(EquipmentElement element, EquipmentIndex index)> FindAllEmptyWeaponSlots(Equipment equipment)
        // {
        //     return equipment.YieldWeaponSlots()
        //         .Where(e => e.element.IsEmpty)
        //         .ToList();
        // }

        // private static void RemoveNonBestMatchingWeapons(SkillObject skillObject, Equipment equipment, params ItemObject.ItemTypeEnum[] itemTypeEnums)
        // {
        //     foreach (var x in GetMatchingItems(skillObject, equipment, itemTypeEnums)
        //         // Highest tier first
        //         .OrderByDescending(e => e.element.Item.Tier)
        //         .Skip(1)
        //         .ToList())
        //     {
        //         equipment[x.index] = EquipmentElement.Invalid;
        //     }
        // }
        //
        // private static void RemoveNonBestSkillItems(IEnumerable<SkillObject> skills, SkillObject bestSkill, Equipment equipment, params ItemObject.ItemTypeEnum[] itemTypeEnums)
        // {
        //     foreach (var x in equipment.YieldWeaponSlots()
        //         .Where(e => !e.element.IsEmpty)
        //         // Correct type
        //         .Where(e => itemTypeEnums.Contains(e.element.Item.Type))
        //         .Where(e => skills.Contains(e.element.Item.RelevantSkill) && e.element.Item.RelevantSkill != bestSkill)
        //         .ToList())
        //     {
        //         equipment[x.index] = EquipmentElement.Invalid;
        //     }
        // }

        // private static List<(EquipmentElement element, EquipmentIndex index)> GetMatchingItems(SkillObject skill, Equipment equipment, params ItemObject.ItemTypeEnum[] itemTypeEnums)
        // {
        //     return equipment.YieldWeaponSlots()
        //         .Where(e => !e.element.IsEmpty)
        //         .Where(e => itemTypeEnums.Contains(e.element.Item.Type))
        //         .Where(e => e.element.Item.RelevantSkill == skill)
        //         .ToList();
        // }
        
        public static bool CanUseItem(ItemObject item, Hero hero, bool overrideAbility)
        {
            var relevantSkill = item.RelevantSkill;
            return    (overrideAbility || relevantSkill == null || hero.GetSkillValue(relevantSkill) >= item.Difficulty) 
                   && (!hero.CharacterObject.IsFemale || !item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByFemale)) 
                   && (hero.CharacterObject.IsFemale || !item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale));
        }
    }
}