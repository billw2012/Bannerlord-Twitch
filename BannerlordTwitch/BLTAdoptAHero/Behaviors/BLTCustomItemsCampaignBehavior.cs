using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.SaveSystem.Extensions;
using BLTAdoptAHero.Actions.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace BLTAdoptAHero
{
    public class BLTCustomItemsCampaignBehavior : CampaignBehaviorBase
    {
        public static BLTCustomItemsCampaignBehavior Current => GetCampaignBehavior<BLTCustomItemsCampaignBehavior>();
        
        private class ItemModifierData
        {
            [SaveableProperty(0)]
            public string Name { get; set; }
            [SaveableProperty(1)]
            public string StringId { get; set; }
            
            [SaveableProperty(2)]
            public int Damage { get; set; }
            [SaveableProperty(3)]
            public int Speed { get; set; }
            [SaveableProperty(4)]
            public int MissileSpeed { get; set; }
            [SaveableProperty(5)]
            public int Armor { get; set; }
            [SaveableProperty(6)]
            public short HitPoints { get; set; }
            [SaveableProperty(7)]
            public short StackCount { get; set; }
            [SaveableProperty(8)]
            public float MountSpeed { get; set; }
            [SaveableProperty(9)]
            public float Maneuver { get; set; }
            [SaveableProperty(10)]
            public float ChargeDamage { get; set; }
            [SaveableProperty(11)]
            public float MountHitPoints { get; set; }

            public void Apply(ItemModifier toModifier)
            {
                toModifier.SetName(new (Name));
                toModifier.StringId = StringId;
                toModifier.SetDamageModifier(Damage);
                toModifier.SetSpeedModifier(Speed);
                toModifier.SetMissileSpeedModifier(MissileSpeed);
                toModifier.SetArmorModifier(Armor);
                toModifier.SetHitPointsModifier(HitPoints);
                toModifier.SetStackCountModifier(StackCount);
                toModifier.SetMountSpeedModifier(MountSpeed);
                toModifier.SetManeuverModifier(Maneuver);
                toModifier.SetChargeDamageModifier(ChargeDamage);
                toModifier.SetMountHitPointsModifier(MountHitPoints);
            }
        }
        
        // private class CustomItemInitializationData
        // {
        //     public ItemObject BaseItem { get; set; }
        //     public string ItemName { get; set; }
        //     public ItemModifierData Modifiers { get; set; }
        // }

        private Dictionary<ItemModifier, ItemModifierData> customItemModifiers = new();
        
        public override void RegisterEvents() {}

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsLoading)
            {
                var savedModiferList = new List<ItemModifier>();
                dataStore.SyncData("ModifierList", ref savedModiferList);
                var savedModiferDataList = new List<ItemModifierData>();
                dataStore.SyncDataAsJson("ModifierData", ref savedModiferDataList);

                // ItemModifier is hashed by string id, so we need to initialize them BEFORE putting them into the dictionary
                customItemModifiers = new();
                foreach (var (modifier, data) in savedModiferList.Zip(savedModiferDataList, (modifier, data) => (modifier, data)))
                {
                    modifier.StringId = data.StringId;
                    var registeredModifier = MBObjectManager.Instance.RegisterObject(modifier);
                    registeredModifier.IsReady = true;
                    data.Apply(registeredModifier);
                    customItemModifiers.Add(registeredModifier, data);
                }
            }
            else
            {
                var savedModiferList = customItemModifiers.Keys.ToList();
                dataStore.SyncData("ModifierList", ref savedModiferList);
                var savedModiferDataList = customItemModifiers.Values.ToList();
                dataStore.SyncDataAsJson("ModifierData", ref savedModiferDataList);
            }
        }

        public ItemModifier CreateArmorModifier(string modifiedName, int armorModifier) =>
            RegisterModifier(new ItemModifierData
            {
                Name = modifiedName,
                Armor = armorModifier,
            });
        
        public ItemModifier CreateWeaponModifier(string modifiedName, int damageModifier, int speedModifier, int missileSpeedModifier) =>
            RegisterModifier(new ItemModifierData
            {
                Name = modifiedName,
                Damage = damageModifier,
                MissileSpeed = missileSpeedModifier,
                Speed = speedModifier,
            });
        
        public ItemModifier CreateAmmoModifier(string modifiedName, int damageModifier, short stackModifier) =>
            RegisterModifier(new ItemModifierData
            {
                Name = modifiedName,
                Damage = damageModifier,
                StackCount = stackModifier,
            });
        
        public ItemModifier CreateMountModifier(string modifiedName, float maneuverModifier, float mountSpeedModifier, float chargeDamageModifier, float mountHitPointsModifier) =>
            RegisterModifier(new ItemModifierData
            {
                Name = modifiedName,
                Maneuver = maneuverModifier,
                MountSpeed = mountSpeedModifier,
                ChargeDamage = chargeDamageModifier,
                MountHitPoints = mountHitPointsModifier,
            });
        
        private ItemModifier RegisterModifier(ItemModifierData modifierData)
        {
            modifierData.StringId = Guid.NewGuid().ToString();
            var modifier = new ItemModifier();
            modifierData.Apply(modifier);
            var registeredModifier = MBObjectManager.Instance.RegisterObject(modifier);
            customItemModifiers.Add(registeredModifier, modifierData);
            return registeredModifier;
        }
        
        // public void InitializeCraftingElements()
        // {
        //     List<ItemObject> list = new List<ItemObject>();
        //     foreach (KeyValuePair<ItemObject, CraftingCampaignBehavior.CraftedItemInitializationData> keyValuePair in this._craftedItemDictionary)
        //     {
        //         ItemObject itemObject = Crafting.InitializePreCraftedWeaponOnLoad(keyValuePair.Key, keyValuePair.Value.CraftedData, keyValuePair.Value.ItemName, keyValuePair.Value.Culture, keyValuePair.Value.OverrideData);
        //         if (itemObject == DefaultItems.Trash)
        //         {
        //             list.Add(keyValuePair.Key);
        //             if (MBObjectManager.Instance.GetObject(keyValuePair.Key.Id) != null)
        //             {
        //                 MBObjectManager.Instance.UnregisterObject(keyValuePair.Key);
        //             }
        //         }
        //         else
        //         {
        //             ItemObject.InitAsPlayerCraftedItem(ref itemObject);
        //         }
        //     }
        //     foreach (ItemObject key in list)
        //     {
        //         this._craftedItemDictionary.Remove(key);
        //     }
        //     foreach (KeyValuePair<Town, CraftingCampaignBehavior.CraftingOrderSlots> keyValuePair2 in this.CraftingOrders)
        //     {
        //         foreach (CraftingOrder craftingOrder in keyValuePair2.Value.Slots)
        //         {
        //             if (craftingOrder != null)
        //             {
        //                 craftingOrder.InitializeCraftingOrderOnLoad();
        //             }
        //         }
        //     }
        // }
    }
}