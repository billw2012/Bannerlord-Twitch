using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BannerlordTwitch.Util
{
    public static class MainThreadSync
    {
        private static readonly ConcurrentQueue<(Action action, EventWaitHandle completeEvent)> actions = new();
        private static int MainThreadId;

        internal static void InitMainThread()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }
        
        internal static void RunQueued()
        {
            var st = new Stopwatch();
            st.Start();
            while (actions.TryDequeue(out var action))
            {
                action.action();
                action.completeEvent?.Set();
                if (st.ElapsedMilliseconds > 2)
                {
                    // if (st.ElapsedMilliseconds > 10)
                    // {
                    //     Log.Info($"Action took {st.ElapsedMilliseconds}ms to Enqueue, this is too slow!");
                    // }
                    break;
                }
            }
        }

        public static EventWaitHandle Run(Action action)
        {
            var waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            if (Thread.CurrentThread.ManagedThreadId == MainThreadId)
            {
                action();
                waitHandle.Set();
            }
            else
            {
                actions.Enqueue((action, waitHandle));
            }

            return waitHandle;
        }
        
        public static async Task RunWaitAsync(Action action)
        {
            if (Thread.CurrentThread.ManagedThreadId == MainThreadId)
            {
                action();
            }
            else
            {
                var waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
                actions.Enqueue((action, waitHandle));
                await Task.Run(() => waitHandle.WaitOne());
            }
        }
    }
}