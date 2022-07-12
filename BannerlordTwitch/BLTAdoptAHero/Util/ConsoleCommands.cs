using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace BLTAdoptAHero.Util
{
    internal static class ConsoleCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("showhero", "blt")]
        [UsedImplicitly]
        public static string ShowHero(List<string> strings)
        {
            if (strings.Count != 1)
            {
                return "Provide the hero name";
            }
            
            var hero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(strings[0]);
            if (hero == null)
            {
                return $"Couldn't find hero {strings[0]}";
            }
            
            if (!PartyScreenAllowed)
                return $"Can't open inventory now";
            
            if (ScreenManager.TopScreen is not MapScreen)
            {
                Game.Current.GameStateManager.PopState();
            }
        
            OpenScreenAsInventoryOf(hero.CharacterObject);
        
            return $"Opened inventory of {strings[0]}";
        }
        
        private class FakeMarketData : IMarketData
        {
            public int GetPrice(ItemObject item, MobileParty tradingParty, bool isSelling, PartyBase merchantParty)
            {
                return item.Value;
            }

            public int GetPrice(EquipmentElement itemRosterElement, MobileParty tradingParty, bool isSelling, PartyBase merchantParty)
            {
                return itemRosterElement.ItemValue;
            }
        }
        
        private static void OpenScreenAsInventoryOf(CharacterObject character)
        {
            var inventoryLogicFieldInfo = AccessTools.Field(typeof(InventoryManager), "_inventoryLogic");
            
            //InventoryManager.OpenScreenAsInventoryOf(MobileParty.MainParty, character);
            
            // Might be broken since 1.7.0
            var inventoryLogic = new InventoryLogic(null);
            inventoryLogicFieldInfo.SetValue(InventoryManager.Instance, inventoryLogic);
            var memberRoster = new TroopRoster(null);
            memberRoster.AddToCounts(character, 1);
            inventoryLogic.Initialize(new(), new(), memberRoster, false, true, character, InventoryManager.InventoryCategoryType.None, new FakeMarketData(), false);
            var state = Game.Current.GameStateManager.CreateState<InventoryState>();
            state.InitializeLogic(inventoryLogic);
            Game.Current.GameStateManager.PushState(state);
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("showheroes", "blt")]
        [UsedImplicitly]
        public static string ShowHeroes(List<string> strings)
        {
            return OpenAdoptedHeroScreen() ? "Opened hero screen" : "Hero screen already open";
        }

        private static bool PartyScreenAllowed
        {
            get
            {
                if (Game.Current.GameStateManager.ActiveState is PartyState)
                {
                    return false;
                }
                if (Hero.MainHero.HeroState == Hero.CharacterStates.Prisoner)
                {
                    return false;
                }
                if (MobileParty.MainParty.MapEvent != null)
                {
                    return false;
                }
                return Mission.Current == null || Mission.Current.IsPartyWindowAccessAllowed;
            }
        }

        private static bool OpenAdoptedHeroScreen()
        {
            if (!PartyScreenAllowed)
                return false;
            
            if (ScreenManager.TopScreen is not MapScreen)
            {
                Game.Current.GameStateManager.PopState();
            }
        
            // var _partyScreenLogic = new PartyScreenLogic();
            // AccessTools.Field(typeof(PartyScreenManager), "_partyScreenLogic").SetValue(PartyScreenManager.Instance, _partyScreenLogic);
            // AccessTools.Field(typeof(PartyScreenManager), "_currentMode").SetValue(PartyScreenManager.Instance, PartyScreenMode.Normal);
            
            var heroRoster = new TroopRoster(null);
            foreach (var hero in BLTAdoptAHeroCampaignBehavior.GetAllAdoptedHeroes().OrderBy(h => h.Name.Raw().ToLower()))
            {
                heroRoster.AddToCounts(hero.CharacterObject, 1);
            }

            if (heroRoster.Count == 0)
            {
                return false;
            }
            
            PartyScreenManager.OpenScreenWithDummyRoster(
                heroRoster, 
                new TroopRoster(null), 
                new TroopRoster(null), 
                new TroopRoster(null), new("Viewers"), 
                null, 0, 0, null, null, null);

            // _partyScreenLogic.Initialize(new PartyScreenLogicInitializationData
            // {
            //     LeftMemberRoster = heroRoster,
            //     Header = new ("Viewers"),
            // });
            //_partyScreenLogic.Initialize(heroRoster, new(null), MobileParty.MainParty, false, new("Viewers"), 0, (_, _, _, _, _, _, _, _, _) => true, new("BLT Viewer Heroes"), false);
            //_partyScreenLogic.InitializeTrade(PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable);
            //_partyScreenLogic.SetTroopTransferableDelegate((_, _, _, _) => false);
            
             // var partyState = Game.Current.GameStateManager.CreateState<PartyState>();
             // partyState.InitializeLogic(_partyScreenLogic);
             // Game.Current.GameStateManager.PushState(partyState);
        
            return true;
        }
    }
}