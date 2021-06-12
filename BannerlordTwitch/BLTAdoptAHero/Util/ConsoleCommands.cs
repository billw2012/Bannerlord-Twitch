using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

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
            
            var character = BLTAdoptAHeroCampaignBehavior.GetAdoptedHero(strings[0])?.CharacterObject;
            if (character == null)
            {
                return $"Couldn't find hero {strings[0]}";
            }
            
            if (!PartyScreenAllowed)
                return $"Can't open inventory now";
            
            if (ScreenManager.TopScreen is not MapScreen)
            {
                Game.Current.GameStateManager.PopState();
            }
            InventoryManager.OpenScreenAsInventoryOf(MobileParty.MainParty, character);

            return $"Opened inventory of {strings[0]}";
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

            var _partyScreenLogic = new PartyScreenLogic();
            AccessTools.Field(typeof(PartyScreenManager), "_partyScreenLogic").SetValue(PartyScreenManager.Instance, _partyScreenLogic);
            AccessTools.Field(typeof(PartyScreenManager), "_currentMode").SetValue(PartyScreenManager.Instance, PartyScreenMode.Normal);
            
            var heroRoster = new TroopRoster(null);
            foreach (var hero in BLTAdoptAHeroCampaignBehavior.GetAllAdoptedHeroes().OrderBy(h => h.Name.Raw().ToLower()))
            {
                heroRoster.AddToCounts(hero.CharacterObject, 1);
            }
            
            _partyScreenLogic.Initialize(heroRoster, new TroopRoster(null), MobileParty.MainParty, false, new TextObject("Viewers"), 0, (_, _, _, _, _, _, _, _, _) => true, new TextObject("BLT Viewer Heroes"), false);
            _partyScreenLogic.InitializeTrade(PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable);
            _partyScreenLogic.SetTroopTransferableDelegate((_, _, _, _) => false);
            var partyState = Game.Current.GameStateManager.CreateState<PartyState>();
            partyState.InitializeLogic(_partyScreenLogic);
            Game.Current.GameStateManager.PushState(partyState);

            return true;
        }
    }
}