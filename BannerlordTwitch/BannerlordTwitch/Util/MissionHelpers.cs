using System.Linq;
using SandBox;
using SandBox.Source.Missions;
using StoryMode.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerlordTwitch.Util
{
    public static class MissionHelpers
    {
        public static bool HeroIsSpawned(Hero hero) 
            => //CampaignMission.Current.Location?.ContainsCharacter(hero) == true || 
                Mission.Current?.Agents.Any(a => a.Character == hero.CharacterObject) == true;

        public static bool InHideOutMission() => Mission.Current?.GetMissionBehaviour<HideoutMissionController>() != null;

        public static bool InFieldBattleMission() => Mission.Current?.IsFieldBattle == true;

        public static bool InSiegeMission() 
            => Mission.Current?.IsFieldBattle != true 
               && Mission.Current?.GetMissionBehaviour<CampaignSiegeStateHandler>() != null;

        public static bool InArenaPracticeMission() 
            => CampaignMission.Current?.Location?.StringId == "arena"
               && Mission.Current?.Mode == MissionMode.Battle;

        public static bool InArenaPracticeVisitingArea() 
            => CampaignMission.Current?.Location?.StringId == "arena"
               && Mission.Current?.Mode != MissionMode.Battle;

        public static bool InTournament()
            => Mission.Current?.GetMissionBehaviour<TournamentFightMissionController>() != null 
               && Mission.Current?.Mode == MissionMode.Battle;

        public static bool InFriendlyMission() 
            => Mission.Current?.IsFriendlyMission == true && !InArenaPracticeMission();

        public static bool InConversation() => Mission.Current?.Mode == MissionMode.Conversation;

        public static bool InTrainingFieldMission()
            => Mission.Current?.GetMissionBehaviour<TrainingFieldMissionController>() != null;

        public static bool InVillageEncounter()
            => PlayerEncounter.LocationEncounter?.GetType() == typeof(VillageEncouter);
    }
}