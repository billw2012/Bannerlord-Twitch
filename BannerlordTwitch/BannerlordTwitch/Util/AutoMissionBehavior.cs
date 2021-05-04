using TaleWorlds.MountAndBlade;

namespace BannerlordTwitch.Util
{
    public abstract class AutoMissionBehavior<T> : MissionBehaviour where T : MissionBehaviour, new()
    {
        public override MissionBehaviourType BehaviourType => MissionBehaviourType.Other;
        
        public static T Current
        {
            get
            {
                var current = Mission.Current.GetMissionBehaviour<T>();
                if (current == null)
                {
                    current = new T();
                    Mission.Current.AddMissionBehaviour(current);
                }

                return current;
            }
        }
    }
}