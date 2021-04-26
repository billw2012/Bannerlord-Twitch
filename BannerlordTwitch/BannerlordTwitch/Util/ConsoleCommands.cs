using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TaleWorlds.Library;

namespace BannerlordTwitch.Util
{
    internal class ConsoleCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("reward", "blt")]
        [UsedImplicitly]
        public static string SpoofReward(List<string> strings)
        {
            var parts = string.Join(" ", strings).Split(',').Select(p => p.Trim()).ToList();
            BLTModule.TwitchService.TestRedeem(parts[0], parts.Count > 1 ? parts[1] : "Test User",
                parts.Count > 2 ? parts[2] : null);
            // Rewards.RewardManager.Enqueue(reward, Guid.Empty, null);
            //Log.Info("--Current FPS: " + Utilities.GetFps(), 0, Debug.DebugColor.White, 17179869184UL);
            return $"Tested redemption of {parts[0]}";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("simteststart", "blt")]
        [UsedImplicitly]
        public static string SimTest(List<string> strings)
        {
            BLTModule.TwitchService.StartSim();
            return "Test started";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("simteststop", "blt")]
        public static string SimTestStop(List<string> strings)
        {
            BLTModule.TwitchService.StopSim();
            return "Test stopped";
        }
    }
}