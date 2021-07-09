using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace BannerlordTwitch.Util
{
    public static class MainThreadSync
    {
        private static readonly ConcurrentQueue<Action> actions = new();
        private static int MainThreadId;

        internal static void InitMainThread()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }
        
        internal static void RunQueued()
        {
            var st = new Stopwatch();
            st.Start();
            while (st.ElapsedMilliseconds < 2 && actions.TryDequeue(out var action))
            {
                action();
            }
        }

        public static void Run(Action action)
        {
            if (Thread.CurrentThread.ManagedThreadId == MainThreadId)
            {
                action();
            }
            else
            {
                actions.Enqueue(action);
            }
        }
    }
}