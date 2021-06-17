using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace BLTAdoptAHero.Actions.Util
{
    public static class CustomItems
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("testcraft", "blt")]
        [UsedImplicitly]
        public static string TestCraft(List<string> strings)
        {
            try
            {
                var item = CreateWeapon(Hero.MainHero, (WeaponClass) Enum.Parse(typeof(WeaponClass), strings[0]), int.Parse(strings[1]));

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

        public static WeaponClass[] CraftableWeaponClasses { get; set; } = {
            WeaponClass.Dagger,
            WeaponClass.OneHandedSword,
            WeaponClass.TwoHandedSword,
            WeaponClass.OneHandedAxe,
            WeaponClass.TwoHandedAxe,
            WeaponClass.Mace,
            WeaponClass.Pick,
            WeaponClass.TwoHandedMace,
            WeaponClass.OneHandedPolearm,
            WeaponClass.TwoHandedPolearm,
            WeaponClass.LowGripPolearm,
            WeaponClass.ThrowingAxe,
            WeaponClass.ThrowingKnife,
            WeaponClass.Javelin,
        };
        
        public static ItemObject CreateWeapon(Hero hero, WeaponClass weaponClass, int desiredTier)
        {
            return CraftableWeaponClasses.Contains(weaponClass)
                ? CreateCraftedWeapon(hero, weaponClass, desiredTier)
                : CreateCraftedWeapon(hero, WeaponClass.OneHandedSword, desiredTier)
                // TODO: 
                //: CreateVariationWeapon(hero, weaponClass, desiredTier)
                ;
        }

        private static ItemObject CreateVariationWeapon(Hero hero, WeaponClass weaponClass, int desiredTier)
        {
            var item = HeroHelpers.AllItems
                .Where(i => i.WeaponComponent?.PrimaryWeapon?.WeaponClass == weaponClass)
                .Where(i => (int)i.Tier <= desiredTier)
                .Shuffle()
                .OrderByDescending(i => i.Tier)
                .FirstOrDefault()
                ;
            if (item == null)
            {
                Log.Error($"Failed to create variation Tier {desiredTier + 1} {weaponClass} for {hero.Name}: no valid base items found");
                return null;
            }

            throw new NotImplementedException();
        }

        private static ItemObject CreateCraftedWeapon(Hero hero, WeaponClass weaponClass, int desiredTier)
        {
            var validTemplates = CraftingTemplate.All
                .Where(t => t.WeaponUsageDatas?.Any(w => w.WeaponClass == weaponClass) == true)
                .ToList();

            if (!validTemplates.Any())
            {
                Log.Error($"Failed to create Tier {desiredTier + 1} {weaponClass} for {hero.Name}: no matching templates for {weaponClass}");
                return null;
            }

            int itr = 0;
            ItemObject generatedItem = null;
            do
            {
                var crafting = new Crafting(validTemplates.SelectRandom(), hero.Culture);
                crafting.Init();
                crafting.Randomize();

                generatedItem = (ItemObject)AccessTools.Field(typeof(Crafting), "_craftedItemObject").GetValue(crafting);

                AccessTools.Property(typeof(ItemObject), nameof(ItemObject.Name))
                    .SetValue(generatedItem, new TextObject($"{crafting.CurrentCraftingTemplate.TemplateName} (Tournament Prize of {hero.FirstName})"));
            } while ((generatedItem.WeaponComponent?.PrimaryWeapon.WeaponClass != weaponClass || (int)generatedItem.Tier != desiredTier) && ++itr < 1000);
            
            if (itr >= 1000)
            {
                Log.Error($"Failed to create Tier {desiredTier + 1} {weaponClass} for {hero.Name} in {itr} iterations");
                return null;
            }
            
            Log.Info($"Created {generatedItem.Tier} ({generatedItem.Tierf:0.00}) {weaponClass} {generatedItem.Name} for {hero.Name} in {itr} iterations");
            
            generatedItem.StringId = Guid.NewGuid().ToString();
            CompleteCraftedItem(generatedItem);
            return MBObjectManager.Instance.RegisterObject(generatedItem);
        }
        
        // public static ItemObject CreateRandomCraftedItem(BasicCultureObject culture)
        // {
        //     var item = Crafting.CreateRandomCraftedItem(culture);
        //     CompleteCraftedItem(item, null);
        //     return item;
        // }
        //
        // public static ItemObject CreateRandomCraftedWeapon(Hero hero, WeaponDesign weaponDesign, int modifierTier,
        //     Crafting.OverrideData overrideData, string itemName, BasicCultureObject culture,
        //     ItemModifierGroup itemModifiers)
        // {
        //     ItemObject item = null;
        //     Crafting.GenerateItem(weaponDesign, itemName, culture, itemModifiers, ref item, overrideData);
        //     CompleteCraftedItem(item, overrideData);
        //     return item;
        // }

        private static void CompleteCraftedItem(ItemObject item, Crafting.OverrideData overrideData = null)
        {
            ItemObject.InitAsPlayerCraftedItem(ref item);
            MBObjectManager.Instance.RegisterObject(item);
            CampaignEventDispatcher.Instance.OnNewItemCrafted(item, overrideData, false);
        }
        
        
        private static float CalculateTierMeleeWeapon(WeaponComponent weaponComponent)
        {
            float highestComponentValue = float.MinValue;
            float secondHighestComponentValue = float.MinValue;
            foreach (var weaponComponentData in weaponComponent.Weapons)
            {
                float thrustValue = weaponComponentData.ThrustDamage * GetFactor(weaponComponentData.ThrustDamageType) * MathF.Pow(weaponComponentData.ThrustSpeed * 0.01f, 1.5f);
                float swingValue = weaponComponentData.SwingDamage * GetFactor(weaponComponentData.SwingDamageType) * MathF.Pow(weaponComponentData.SwingSpeed * 0.01f, 1.5f);
                float damageTypeValue = Math.Max(thrustValue, swingValue * 1.1f);
                if (weaponComponentData.WeaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand))
                {
                    damageTypeValue *= 0.8f;
                }
                
                if (weaponComponentData.WeaponClass is WeaponClass.ThrowingKnife or WeaponClass.ThrowingAxe)
                {
                    damageTypeValue *= 1.2f;
                }
                else if (weaponComponentData.WeaponClass is WeaponClass.Javelin)
                {
                    damageTypeValue *= 0.6f;
                }
                
                float lengthValue = weaponComponentData.WeaponLength * 0.01f;
                float finalComponentValue = 0.06f * (damageTypeValue * (1f + lengthValue)) - 3.5f;
                if (finalComponentValue > secondHighestComponentValue)
                {
                    if (finalComponentValue >= highestComponentValue)
                    {
                        secondHighestComponentValue = highestComponentValue;
                        highestComponentValue = finalComponentValue;
                    }
                    else
                    {
                        secondHighestComponentValue = finalComponentValue;
                    }
                }
            }

            highestComponentValue = MathF.Clamp(highestComponentValue, -1.5f, 7.5f);
            if (weaponComponent.Weapons.Count <= 1)
            {
                return highestComponentValue;
            }
            
            secondHighestComponentValue = MathF.Clamp(secondHighestComponentValue, -1.5f, 7.5f);

            return highestComponentValue * MathF.Pow(1f + (secondHighestComponentValue + 1.5f) / (highestComponentValue + 2.5f), 0.2f);
        }
        
        private static float CalculateTierCraftedWeapon(WeaponDesign craftingData)
        {
            var craftingPieces = craftingData
                .UsedPieces
                .Select(e => e.CraftingPiece)
                .Where(c => c.IsValid)
                .ToList();

            if(!craftingPieces.Any())
            {
                return 0.1f;
            }   

            float averagePieceTier = (float) craftingPieces.Average(p => p.PieceTier);

            var valuableMaterials = craftingPieces
                .SelectMany(p => p.MaterialsUsed)
                .Where(m => m.Item1 is >= CraftingMaterials.Iron1 and <= CraftingMaterials.Iron6)
                .ToList()
                ;

            if (valuableMaterials.Sum(m => m.Item2) > 0)
            {
                int materialsValue = valuableMaterials.Sum(m => (int)m.Item1 * m.Item2);
                return 0.4f * (1.25f * averagePieceTier) + 0.6f * (1.3f * materialsValue / (valuableMaterials.Count + 0.6f) - 1.3f);
            }
            else
            {
                return averagePieceTier;
            }
        }
        
        private static float GetFactor(DamageTypes swingDamageType)
        {
            if (swingDamageType == DamageTypes.Blunt)
            {
                return 1.3f;
            }
            if (swingDamageType != DamageTypes.Pierce)
            {
                return 1f;
            }
            return 1.15f;
        }
    }
}