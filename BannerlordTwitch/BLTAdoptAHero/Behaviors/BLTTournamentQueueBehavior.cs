using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BannerlordTwitch.SaveSystem;
using BannerlordTwitch.Util;
using HarmonyLib;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
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
            using var scopedJsonSync = new ScopedJsonSync(dataStore, nameof(BLTTournamentQueueBehavior));
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
                scopedJsonSync.SyncDataAsJson("Queue2", ref queue);
            }
            else
            {
                List<Hero> usedHeroList = null;
                dataStore.SyncData("UsedHeroObjectList", ref usedHeroList);
                List<TournamentQueueEntrySavable> queue = null;
                scopedJsonSync.SyncDataAsJson("Queue2", ref queue);
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
            public int HeroIndex { get; set; }
            public bool IsSub { get; set; }
            public int EntryFee { get; set; }
        }

        public List<TournamentQueueEntry> TournamentQueue = new();

        public bool TournamentAvailable => TournamentQueue.Any();

        public (bool success, string reply) AddToQueue(Hero hero, bool isSub, int entryFree)
        {
            if (TournamentQueue.Any(sh => sh.Hero == hero))
            {
                return (false, "{=JtZIstbB}You are already in the tournament queue!".Translate());
            }

            TournamentQueue.Add(new TournamentQueueEntry(hero, isSub, entryFree));
            TournamentHub.UpdateEntrants();
            return (true, "{=1duM11Gt}You are position {QueuePosition} in the tournament queue!"
                .Translate(("QueuePosition", TournamentQueue.Count)));
        }

        public void RemoveFromQueue(Hero hero)
        {
            if(TournamentQueue.RemoveAll(e => e.Hero == hero) > 0)
            {
                TournamentHub.UpdateEntrants();
            }
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
            MissionState.Current.CurrentMission.AddMissionBehavior(startingTournament);
            MissionState.Current.CurrentMission.AddMissionBehavior(new BLTTournamentBetMissionBehavior());
            MissionState.Current.CurrentMission.AddMissionBehavior(new BLTTournamentSkillAdjustBehavior());

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
            try
            {
                a();
            }
            catch (Exception e)
            {
                Log.Exception($"{nameof(BLTTournamentQueueBehavior)}.{fnName}", e);
            }
        }
        
        // MissionState.Current.CurrentMission doesn't have any behaviours yet added during this function,
        // so we split the initialization that requires access to mission behaviours into another patch below
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(FightTournamentGame), nameof(FightTournamentGame.GetParticipantCharacters))]
        public static void GetParticipantCharactersPostfix(Settlement settlement, List<CharacterObject> __result)
        {
            SafeCallStatic(() => Current?.GetParticipantCharactersPostfixImpl(settlement, __result));
        }
        #endregion
    }
}