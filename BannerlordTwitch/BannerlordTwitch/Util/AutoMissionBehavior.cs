using TaleWorlds.MountAndBlade;

namespace BannerlordTwitch.Util
{
    public abstract class AutoMissionBehavior<T> : MissionBehaviour where T : MissionBehaviour, new()
    {
        public override MissionBehaviourType BehaviourType => MissionBehaviourType.Other;

        public static T Current => MissionState.Current?.CurrentMission?.GetMissionBehaviour<T>();
        
        // public static T CurrentState
        // {
        //     get
        //     {
        //         var current = MissionState.Current.CurrentMission.GetMissionBehaviour<T>();
        //         if (current == null)
        //         {
        //             current = new T();
        //             MissionState.Current.CurrentMission.AddMissionBehaviour(current);
        //         }
        //
        //         return current;
        //     }
        // }
    }
}