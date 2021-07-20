using System;
using System.Collections.Generic;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox;
using SandBox.TournamentMissions.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [HarmonyPatch, Description("Puts adopted heroes in queue for the next tournament"), UsedImplicitly]
    internal class JoinTournament : ActionHandlerBase
    {
        [CategoryOrder("General", 1)]
        private class Settings : IDocumentable
        {
            [Category("General"), Description("Gold cost to join"), PropertyOrder(4)]
            public int GoldCost { get; [UsedImplicitly] set; }

            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                if (GoldCost != 0) generator.PropertyValuePair("Cost", $"{GoldCost}{Naming.Gold}");
            }
        }
        
        protected override Type ConfigType => typeof(Settings);
        
        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Settings) config;
            
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(AdoptAHero.NoHeroMessage);
                return;
            }
            
            int availableGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(adoptedHero);
            if (availableGold < settings.GoldCost)
            {
                onFailure(Naming.NotEnoughGold(settings.GoldCost, availableGold));
                return;
            }

            (bool success, string reply) = BLTTournamentQueueBehavior.Current.AddToQueue(adoptedHero, context.IsSubscriber, settings.GoldCost);
            if (!success)
            {
                onFailure(reply);
            }
            else
            {
                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(adoptedHero, -settings.GoldCost);
                onSuccess(reply);
            }
        }

        public static void SetupGameMenus(CampaignGameStarter campaignGameSystemStarter)
        {
            campaignGameSystemStarter.AddGameMenuOption(
                "town_arena", "blt_join_tournament", "JOIN the viewer tournament", 
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                    return BLTTournamentQueueBehavior.Current.TournamentAvailable;
                },
                _ =>
                {
                    BLTTournamentQueueBehavior.Current.JoinViewerTournament();
                    GameMenu.SwitchToMenu("town");
                }, 
                index: 2);
            campaignGameSystemStarter.AddGameMenuOption(
                "town_arena", "blt_watch_tournament", "WATCH the viewer tournament", 
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                    return BLTTournamentQueueBehavior.Current.TournamentAvailable;
                },
                _ =>
                {
                    BLTTournamentQueueBehavior.Current.WatchViewerTournament();
                    GameMenu.SwitchToMenu("town");
                }, 
                index: 3);
        }

        #region Patches
        // MissionState.Current.CurrentMission doesn't have any behaviours yet added during this function,
        // so we split the initialization that requires access to mission behaviours into another patch below
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentGame), nameof(TournamentGame.GetParticipantCharacters))]
        public static void GetParticipantCharactersPostfix(Settlement settlement,
            int maxParticipantCount, bool includePlayer, List<CharacterObject> __result)
        {
            try
            {
                BLTTournamentQueueBehavior.Current.GetParticipantCharacters(settlement, __result);
            }
            catch (Exception e)
            {
                Log.Exception($"{nameof(JoinTournament)}.{nameof(GetParticipantCharactersPostfix)}", e);
            }
        }

        // After PrepareForTournamentGame the MissionState.Current.CurrentMission contains the behaviors
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentGame), nameof(TournamentGame.PrepareForTournamentGame))]
        public static void PrepareForTournamentGamePostfix(TournamentGame __instance, bool isPlayerParticipating)
        {
            try
            {
                BLTTournamentQueueBehavior.Current.PrepareForTournamentGame();
            }
            catch (Exception e)
            {
                Log.Exception($"{nameof(JoinTournament)}.{nameof(PrepareForTournamentGamePostfix)}", e);
            }
        }
        
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentFightMissionController), "GetTeamWeaponEquipmentList")]
        public static void GetTeamWeaponEquipmentListPostfix(List<Equipment> __result)
        {
            try
            {
                BLTTournamentQueueBehavior.Current.GetTeamWeaponEquipmentList(__result);
            }
            catch (Exception e)
            {
                Log.Exception($"{nameof(JoinTournament)}.{nameof(GetTeamWeaponEquipmentListPostfix)}", e);
            }
        }
        
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentBehavior), "AfterStart")]
        public static void AfterStartPostfix(TournamentBehavior __instance)
        {
            try
            {
                // Only called at the start of the tournament
                BLTBetMissionBehavior.Current?.OpenBetting(__instance);
            }
            catch (Exception e)
            {
                Log.Exception($"{nameof(JoinTournament)}.{nameof(AfterStartPostfix)}", e);
            }
        }

        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(TournamentBehavior), "StartMatch")]
        public static void StartMatchPrefix(TournamentBehavior __instance)
        {
            try
            {
                BLTBetMissionBehavior.Current?.CloseBetting(__instance);
            }
            catch (Exception e)
            {
                Log.Exception($"{nameof(JoinTournament)}.{nameof(StartMatchPrefix)}", e);
            }
        }

        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(TournamentBehavior), "SkipMatch")]
        public static void SkipMatchPrefix(TournamentBehavior __instance)
        {
            try
            {
                BLTBetMissionBehavior.Current?.CloseBetting(__instance);
            }
            catch (Exception e)
            {
                Log.Exception($"{nameof(JoinTournament)}.{nameof(SkipMatchPrefix)}", e);
            }
        }
         
        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(TournamentBehavior), "EndCurrentMatch")]
        public static void EndCurrentMatchPrefix(TournamentBehavior __instance)
        {
            try
            {
                BLTTournamentQueueBehavior.Current?.EndCurrentMatchPrefix(__instance);
            }
            catch (Exception e)
            {
                Log.Exception($"{nameof(JoinTournament)}.{nameof(EndCurrentMatchPrefix)}", e);
            }
        }
        
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentBehavior), "EndCurrentMatch")]
        public static void EndCurrentMatchPostfix(TournamentBehavior __instance)
        {
            try
            {
                BLTTournamentQueueBehavior.Current?.EndCurrentMatchPostfix(__instance);
            }
            catch (Exception e)
            {
                Log.Exception($"{nameof(JoinTournament)}.{nameof(EndCurrentMatchPostfix)}", e);
            }
        }
        #endregion

        public static void OnGameEnd(Campaign campaign)
        {
            campaign.GetCampaignBehavior<BLTTournamentQueueBehavior>()?.Dispose();
        }
    }
}
