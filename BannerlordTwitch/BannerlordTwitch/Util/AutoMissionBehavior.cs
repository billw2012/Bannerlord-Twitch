using System;
using System.Runtime.CompilerServices;
using TaleWorlds.MountAndBlade;

namespace BannerlordTwitch.Util
{
    public abstract class AutoMissionBehavior<T> : MissionBehaviour where T : MissionBehaviour
    {
        public override MissionBehaviourType BehaviourType => MissionBehaviourType.Other;

        public static T Current => MissionState.Current?.CurrentMission?.GetMissionBehaviour<T>();

        protected void SafeCall(Action a, [CallerMemberName]string fnName = "")
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
                Log.Exception($"{this.GetType().Name}.{fnName}", e);
            }
#endif
        }
        
        protected static void SafeCallStatic(Action a, [CallerMemberName]string fnName = "")
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
                Log.Exception($"{typeof(T).Name}.{fnName}", e);
            }
#endif
        }
        
        protected static U SafeCallStatic<U>(Func<U> a, U def, [CallerMemberName]string fnName = "")
        {
#if !DEBUG
            try
            {
#endif
            return a();
#if !DEBUG
            }
            catch (Exception e)
            {
                Log.Exception($"{typeof(T).Name}.{fnName}", e);
                return def;
            }
#endif
        }
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