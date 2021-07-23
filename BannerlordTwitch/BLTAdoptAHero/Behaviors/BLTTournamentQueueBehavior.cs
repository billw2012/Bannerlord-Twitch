using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Bannerlord.ButterLib.SaveSystem.Extensions;
using BannerlordTwitch.Util;
using HarmonyLib;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace BLTAdoptAHero
{
    [HarmonyPatch]
    public class BLTTournamentQueueBehavior : CampaignBehaviorBase, IDisposable
    {
        public static BLTTournamentQueueBehavior Current => Campaign.Current?.GetCampaignBehavior<BLTTournamentQueueBehavior>();

        public override void RegisterEvents()
        {
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, (_, _, _, _) =>
            {
                TournamentQueue.RemoveAll(e => e.Hero == null || e.Hero.IsDead);
            });
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                var usedHeroList = TournamentQueue.Select(t => t.Hero).ToList();
                dataStore.SyncData("UsedHeroObjectList", ref usedHeroList);
                var queue = TournamentQueue.Select(e => new TournamentQueueEntrySavable
                {
                    HeroIndex = usedHeroList.IndexOf(e.Hero),
                    IsSub = e.IsSub,
                    EntryFee = e.EntryFee,
                }).ToList();
                dataStore.SyncDataAsJson("Queue2", ref queue);
            }
            else
            {
                List<Hero> usedHeroList = null;
                dataStore.SyncData("UsedHeroObjectList", ref usedHeroList);
                List<TournamentQueueEntrySavable> queue = null;
                dataStore.SyncDataAsJson("Queue2", ref queue);
                if (usedHeroList != null && queue != null)
                {
                    TournamentQueue = queue.Select(e => new TournamentQueueEntry
                    {
                        Hero = usedHeroList[e.HeroIndex],
                        IsSub = e.IsSub,
                        EntryFee = e.EntryFee,
                    }).ToList();
                }
            }
            TournamentQueue ??= new();
            TournamentQueue.RemoveAll(e => e.Hero == null || e.Hero.IsDead);
            TournamentHub.UpdateEntrants();
        }

        public (int entrants, int tournamentSize) GetTournamentQueueSize()
        {
            return (TournamentQueue.Count, 16);
        }
        
        // private void UpdatePanel()
        // {
        //     (int entrants, int tournamentSize) = GetTournamentQueueSize();
        //     TournamentHub.Refresh(entrants, tournamentSize, GetTotalBets());
        // }

        public class TournamentQueueEntry
        {
            [UsedImplicitly] 
            public Hero Hero { get; set; }
            [UsedImplicitly] 
            public bool IsSub { get; set; }
            [UsedImplicitly] 
            public int EntryFee { get; set; }

            public TournamentQueueEntry(Hero hero = null, bool isSub = false, int entryFee = 0)
            {
                Hero = hero;
                IsSub = isSub;
                EntryFee = entryFee;
            }
        }

        private class TournamentQueueEntrySavable
        {
            [SaveableProperty(0)]
            public int HeroIndex { get; set; }
            [SaveableProperty(1)]
            public bool IsSub { get; set; }
            [SaveableProperty(2)]
            public int EntryFee { get; set; }
        }

        public List<TournamentQueueEntry> TournamentQueue = new();

        public bool TournamentAvailable => TournamentQueue.Any();

        public (bool success, string reply) AddToQueue(Hero hero, bool isSub, int entryFree)
        {
            if (TournamentQueue.Any(sh => sh.Hero == hero))
            {
                return (false, $"You are already in the tournament queue!");
            }

            TournamentQueue.Add(new TournamentQueueEntry(hero, isSub, entryFree));
            TournamentHub.UpdateEntrants();
            return (true, $"You are position {TournamentQueue.Count} in the tournament queue!");
        }
        
        public void JoinViewerTournament()
        {
            StartViewerTournament(true);
        }

        public void WatchViewerTournament()
        {
            StartViewerTournament(false);
        }

        private BLTTournamentMissionBehavior startingTournament;
        private void StartViewerTournament(bool isPlayerParticipating)
        {
            var tournamentGame = Campaign.Current.Models.TournamentModel.CreateTournament(Settlement.CurrentSettlement.Town);

            startingTournament = new BLTTournamentMissionBehavior(isPlayerParticipating, tournamentGame);

            tournamentGame.PrepareForTournamentGame(isPlayerParticipating);

            startingTournament.PrepareForTournamentGame();
            
            // Mission is created by PrepareForTournamentGame, so we can add to it here
            MissionState.Current.CurrentMission.AddMissionBehaviour(startingTournament);
            MissionState.Current.CurrentMission.AddMissionBehaviour(new BLTTournamentBetMissionBehavior());
            MissionState.Current.CurrentMission.AddMissionBehaviour(new BLTTournamentSkillAdjustBehavior());

            startingTournament = null;
        }

        private void ReleaseUnmanagedResources()
        {
            TournamentHub.Reset();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~BLTTournamentQueueBehavior()
        {
            ReleaseUnmanagedResources();
        }
        
        
        private void GetParticipantCharactersPostfixImpl(Settlement settlement, List<CharacterObject> __result)
        {
            if (Settlement.CurrentSettlement == settlement && startingTournament != null)
            {
                __result.Clear();
                __result.AddRange(startingTournament.GetParticipants());
            }
        }
        
        #region Patches

        private static void SafeCallStatic(Action a, [CallerMemberName]string fnName = "")
        {
#if !DEBUG
            try
            {
#endif
            a();
#if !DEBUG
            }
            catch (Exception e)
            {
                Log.Exception($"{nameof(BLTTournamentQueueBehavior)}.{fnName}", e);
            }
#endif
        }
        
        // MissionState.Current.CurrentMission doesn't have any behaviours yet added during this function,
        // so we split the initialization that requires access to mission behaviours into another patch below
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentGame), nameof(TournamentGame.GetParticipantCharacters))]
        public static void GetParticipantCharactersPostfix(Settlement settlement,
            int maxParticipantCount, bool includePlayer, List<CharacterObject> __result)
        {
            SafeCallStatic(() => Current?.GetParticipantCharactersPostfixImpl(settlement, __result));
        }
        #endregion
    }
}