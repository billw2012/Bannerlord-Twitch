using System.Linq;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.MissionLogics;
using SandBox.Tournaments.MissionLogics;
using StoryMode.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;

namespace BannerlordTwitch.Helpers
{
    public static class MissionHelpers
    {
        public static bool HeroIsSpawned(Hero hero) 
            => //CampaignMission.Current.Location?.ContainsCharacter(hero) == true || 
                Mission.Current?.Agents.Any(a => a.Character == hero.CharacterObject) == true;

        public static bool InHideOutMission() 
            => Mission.Current?.GetMissionBehavior<HideoutMissionController>() != null;

        public static bool InFieldBattleMission() 
            => Mission.Current?.IsFieldBattle == true;
        
        public static bool InLordsHallBattleMission() 
        #if e159 || e1510 || e160
            => false;
        #else
            => Mission.Current?.GetMissionBehavior<LordsHallFightMissionController>() != null;
        #endif

        public static bool InSiegeMission() 
            => Mission.Current?.IsFieldBattle != true 
               && Mission.Current?.GetMissionBehavior<CampaignSiegeStateHandler>() != null
               && !InLordsHallBattleMission();

        public static bool InArenaPracticeMission() 
            => CampaignMission.Current?.Location?.StringId == "arena"
               && Mission.Current?.Mode == MissionMode.Battle;

        public static bool InArenaPracticeVisitingArea() 
            => CampaignMission.Current?.Location?.StringId == "arena"
               && Mission.Current?.Mode != MissionMode.Battle;

        public static bool InTournament()
            => Mission.Current?.GetMissionBehavior<TournamentFightMissionController>() != null 
               && Mission.Current?.Mode == MissionMode.Battle;

        public static bool InFriendlyMission() 
            => Mission.Current?.IsFriendlyMission == true && !InArenaPracticeMission();

        public static bool InConversation() => Mission.Current?.Mode == MissionMode.Conversation;
        
        public static bool InConversationMission() => Mission.Current?.GetMissionBehavior<ConversationMissionLogic>() != null;

        public static bool InTrainingFieldMission()
            => Mission.Current?.GetMissionBehavior<TrainingFieldMissionController>() != null;

        public static bool InVillageEncounter()
            => PlayerEncounter.LocationEncounter?.GetType() == typeof(VillageEncounter);
    }
}