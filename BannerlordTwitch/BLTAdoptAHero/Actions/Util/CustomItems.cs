using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace BLTAdoptAHero.Actions.Util
{
    public static class CustomItems
    {
#if DEBUG
        [CommandLineFunctionality.CommandLineArgumentFunction("testcraft", "blt")]
        [UsedImplicitly]
        public static string TestCraft(List<string> strings)
        {
            try
            {
                var item = CreateCraftedWeapon(Hero.MainHero, (EquipmentType) Enum.Parse(typeof(EquipmentType), strings[0]), int.Parse(strings[1]));

                if (item != null)
                {
                    if (!Hero.MainHero.BattleEquipment[EquipmentIndex.Weapon0].IsEmpty)
                    {
                        Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(new EquipmentElement(Hero.MainHero.BattleEquipment[EquipmentIndex.Weapon0].Item), 1);
                    }
                    Hero.MainHero.BattleEquipment[EquipmentIndex.Weapon0] = new(item);
                    return $"crafted {item.Name}";
                }

                return $"Couldn't craft a matching item";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("testarmor", "blt")]
        [UsedImplicitly]
        public static string TestModifiedArmor(List<string> strings)
        {
            var item = CampaignHelpers.AllItems.Where(i => i.ItemType == ItemObject.ItemTypeEnum.BodyArmor)
                .Shuffle()
                .OrderByDescending(i => i.Tier)
                .FirstOrDefault();
            var modifier = BLTCustomItemsCampaignBehavior.Current.CreateArmorModifier("Test {ITEMNAME}", 100);
            var slotItem = new EquipmentElement(item, modifier);
            Hero.MainHero.BattleEquipment[EquipmentIndex.Body] = slotItem;
            return $"Assigned {slotItem.GetModifiedItemName()} to {Hero.MainHero.Name}";
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("testweapon", "blt")]
        [UsedImplicitly]
        public static string TestModifiedWeapon(List<string> strings)
        {
            var item = CampaignHelpers.AllItems.Where(i => i.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon)
                .Shuffle()
                .OrderByDescending(i => i.Tier)
                .FirstOrDefault();
            var modifier = BLTCustomItemsCampaignBehavior.Current.CreateWeaponModifier("Test {ITEMNAME}", 200, 200, 200, 200);
            var slotItem = new EquipmentElement(item, modifier);
            Hero.MainHero.BattleEquipment[EquipmentIndex.Weapon0] = slotItem;
            return $"Assigned {slotItem.GetModifiedItemName()} to {Hero.MainHero.Name}";
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("testbow", "blt")]
        [UsedImplicitly]
        public static string TestModifiedBow(List<string> strings)
        {
            var item = CampaignHelpers.AllItems.Where(i => i.ItemType == ItemObject.ItemTypeEnum.Bow)
                .Shuffle()
                .OrderByDescending(i => i.Tier)
                .FirstOrDefault();
            var modifier = BLTCustomItemsCampaignBehavior.Current.CreateWeaponModifier("Test {ITEMNAME}", 200, 200, 200, 200);
            var slotItem = new EquipmentElement(item, modifier);
            Hero.MainHero.BattleEquipment[EquipmentIndex.Weapon1] = slotItem;
            return $"Assigned {slotItem.GetModifiedItemName()} to {Hero.MainHero.Name}";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("testammo", "blt")]
        [UsedImplicitly]
        public static string TestModifiedAmmo(List<string> strings)
        {
            var item = CampaignHelpers.AllItems.Where(i => i.ItemType == ItemObject.ItemTypeEnum.Arrows)
                .Shuffle()
                .OrderByDescending(i => i.Tier)
                .FirstOrDefault();
            var modifier = BLTCustomItemsCampaignBehavior.Current.CreateAmmoModifier("Test {ITEMNAME}", 100, 100);
            var slotItem = new EquipmentElement(item, modifier);
            Hero.MainHero.BattleEquipment[EquipmentIndex.Weapon2] = slotItem;
            return $"Assigned {slotItem.GetModifiedItemName()} to {Hero.MainHero.Name}";
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("testmount", "blt")]
        [UsedImplicitly]
        public static string TestModifiedMount(List<string> strings)
        {
            var item = CampaignHelpers.AllItems.Where(i => i.ItemType == ItemObject.ItemTypeEnum.Horse)
                .Shuffle()
                .OrderByDescending(i => i.Tier)
                .FirstOrDefault();
            var modifier = BLTCustomItemsCampaignBehavior.Current.CreateMountModifier("Test {ITEMNAME}", 2, 2, 2, 2);
            var slotItem = new EquipmentElement(item, modifier);
            Hero.MainHero.BattleEquipment[EquipmentIndex.Horse] = slotItem;
            return $"Assigned {slotItem.GetModifiedItemName()} to {Hero.MainHero.Name}";
        }
#endif

        public static EquipmentType[] CraftableEquipmentTypes { get; set; } = {
            EquipmentType.Dagger,
            EquipmentType.OneHandedSword,
            EquipmentType.TwoHandedSword,
            EquipmentType.OneHandedAxe,
            EquipmentType.TwoHandedAxe,
            EquipmentType.OneHandedMace,
            EquipmentType.TwoHandedMace,
            EquipmentType.OneHandedLance,
            EquipmentType.TwoHandedLance,
            EquipmentType.OneHandedGlaive,
            EquipmentType.TwoHandedGlaive,
            EquipmentType.ThrowingKnives,
            EquipmentType.ThrowingAxes,
            EquipmentType.ThrowingJavelins,
        };

        public static ItemObject CreateCraftedWeapon(Hero hero, EquipmentType weaponType, int desiredTier)
        {
            // var equipmentTypes = weaponTypes.ToList();
            var equipmentWeaponClass = EquipmentTypeHelpers.GetWeaponClass(weaponType);
            var validTemplates = CraftingTemplate.All
                .Where(t => t.WeaponUsageDatas?.Any(w => w.WeaponClass == equipmentWeaponClass) == true)
                .ToList();

            if (!validTemplates.Any())
            {
                // Log.Error($"Failed to create Tier {desiredTier + 1} {weaponType} for {hero.Name}: no matching templates for these weapon classes");
                return null;
            }

            int itr = 0;
            var itemsOfCorrectType = new List<ItemObject>();
            do
            {
                var crafting = new Crafting(validTemplates.SelectRandom(), hero.Culture);
                crafting.Init();
                crafting.Randomize();

                var generatedItem = (ItemObject)AccessTools.Field(typeof(Crafting), "_craftedItemObject").GetValue(crafting);
                if (generatedItem.IsEquipmentType(weaponType))
                {
                    itemsOfCorrectType.Add(generatedItem);

                    // We can stop immediately if we found one of the best tier
                    if (generatedItem.Tier == ItemObject.ItemTiers.Tier6)
                    {
                        break;
                    }
                }
                // SetItemName(generatedItem, new ($"{crafting.CurrentCraftingTemplate.TemplateName} (Tournament Prize of {hero.FirstName})"));
            } while (++itr < 500);
            
            if (!itemsOfCorrectType.Any() && itr >= 500)
            {
                Log.Error($"Failed to create crafted {weaponType} for {hero.Name} in {itr} iterations");
                return null;
            }

            var bestItem = itemsOfCorrectType.OrderByDescending(item => item.Tier).First();
            
            Log.Info($"Created {bestItem.Tier} ({bestItem.Tierf:0.00}) {bestItem.WeaponComponent?.PrimaryWeapon.WeaponClass} {bestItem.Name} for {hero.Name} in {itr} iterations");
            
            bestItem.StringId = Guid.NewGuid().ToString();
            CompleteCraftedItem(bestItem);
            return MBObjectManager.Instance.RegisterObject(bestItem);
        }

        private static void SetItemName(ItemObject item, TextObject name) => AccessTools.Property(typeof(ItemObject), nameof(ItemObject.Name)).SetValue(item, name);

        private static void CompleteCraftedItem(ItemObject item, Crafting.OverrideData overrideData = null)
        {
            ItemObject.InitAsPlayerCraftedItem(ref item);
            MBObjectManager.Instance.RegisterObject(item);
            #if e159 || e1510
            AccessTools.Method(typeof(CampaignEventDispatcher), "OnNewItemCrafted")
                .Invoke(CampaignEventDispatcher.Instance, new object[] { item, overrideData });
            #else
            CampaignEventDispatcher.Instance.OnNewItemCrafted(item, overrideData, false);
            #endif
        }
        
        // The vanilla tier calculation for weapons:
        
        // private static float CalculateTierMeleeWeapon(WeaponComponent weaponComponent)
        // {
        //     float highestComponentValue = float.MinValue;
        //     float secondHighestComponentValue = float.MinValue;
        //     foreach (var weaponComponentData in weaponComponent.Weapons)
        //     {
        //         float thrustValue = weaponComponentData.ThrustDamage * GetFactor(weaponComponentData.ThrustDamageType) * MathF.Pow(weaponComponentData.ThrustSpeed * 0.01f, 1.5f);
        //         float swingValue = weaponComponentData.SwingDamage * GetFactor(weaponComponentData.SwingDamageType) * MathF.Pow(weaponComponentData.SwingSpeed * 0.01f, 1.5f);
        //         float damageTypeValue = Math.Max(thrustValue, swingValue * 1.1f);
        //         if (weaponComponentData.WeaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand))
        //         {
        //             damageTypeValue *= 0.8f;
        //         }
        //         
        //         if (weaponComponentData.WeaponClass is WeaponClass.ThrowingKnife or WeaponClass.ThrowingAxe)
        //         {
        //             damageTypeValue *= 1.2f;
        //         }
        //         else if (weaponComponentData.WeaponClass is WeaponClass.Javelin)
        //         {
        //             damageTypeValue *= 0.6f;
        //         }
        //         
        //         float lengthValue = weaponComponentData.WeaponLength * 0.01f;
        //         float finalComponentValue = 0.06f * (damageTypeValue * (1f + lengthValue)) - 3.5f;
        //         if (finalComponentValue > secondHighestComponentValue)
        //         {
        //             if (finalComponentValue >= highestComponentValue)
        //             {
        //                 secondHighestComponentValue = highestComponentValue;
        //                 highestComponentValue = finalComponentValue;
        //             }
        //             else
        //             {
        //                 secondHighestComponentValue = finalComponentValue;
        //             }
        //         }
        //     }
        //
        //     highestComponentValue = MathF.Clamp(highestComponentValue, -1.5f, 7.5f);
        //     if (weaponComponent.Weapons.Count <= 1)
        //     {
        //         return highestComponentValue;
        //     }
        //     
        //     secondHighestComponentValue = MathF.Clamp(secondHighestComponentValue, -1.5f, 7.5f);
        //
        //     return highestComponentValue * MathF.Pow(1f + (secondHighestComponentValue + 1.5f) / (highestComponentValue + 2.5f), 0.2f);
        // }
        //
        // private static float CalculateTierCraftedWeapon(WeaponDesign craftingData)
        // {
        //     var craftingPieces = craftingData
        //         .UsedPieces
        //         .Select(e => e.CraftingPiece)
        //         .Where(c => c.IsValid)
        //         .ToList();
        //
        //     if(!craftingPieces.Any())
        //     {
        //         return 0.1f;
        //     }   
        //
        //     float averagePieceTier = (float) craftingPieces.Average(p => p.PieceTier);
        //
        //     var valuableMaterials = craftingPieces
        //         .SelectMany(p => p.MaterialsUsed)
        //         .Where(m => m.Item1 is >= CraftingMaterials.Iron1 and <= CraftingMaterials.Iron6)
        //         .ToList()
        //         ;
        //
        //     if (valuableMaterials.Sum(m => m.Item2) > 0)
        //     {
        //         int materialsValue = valuableMaterials.Sum(m => (int)m.Item1 * m.Item2);
        //         return 0.4f * (1.25f * averagePieceTier) + 0.6f * (1.3f * materialsValue / (valuableMaterials.Count + 0.6f) - 1.3f);
        //     }
        //     else
        //     {
        //         return averagePieceTier;
        //     }
        // }
        //
        // private static float GetFactor(DamageTypes swingDamageType)
        // {
        //     if (swingDamageType == DamageTypes.Blunt)
        //     {
        //         return 1.3f;
        //     }
        //     if (swingDamageType != DamageTypes.Pierce)
        //     {
        //         return 1f;
        //     }
        //     return 1.15f;
        // }
    }
}