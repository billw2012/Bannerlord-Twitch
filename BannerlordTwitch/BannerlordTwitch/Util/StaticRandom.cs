using System;
using System.Threading;

namespace BannerlordTwitch.Util
{
    public static class StaticRandom
    {
        private static int seed = Environment.TickCount;

        private static readonly ThreadLocal<Random> random =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

        public static double Next()
        {
            return random.Value.NextDouble();
        }
    }
}