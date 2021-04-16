using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BannerlordTwitch.Util
{
    internal static class MainThreadSync
    {
        private static readonly ConcurrentQueue<Action> actions = new();
        
        public static void Run()
        {
            var st = new Stopwatch();
            st.Start();
            while (actions.TryDequeue(out var action) && st.ElapsedMilliseconds < 2)
            {
                action();
            }
        }

        public static void Enqueue(Action action)
        {
            actions.Enqueue(action);
        }
    }
}