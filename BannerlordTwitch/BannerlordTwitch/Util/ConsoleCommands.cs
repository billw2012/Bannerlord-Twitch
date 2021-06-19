using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace BannerlordTwitch.Util
{
    internal static class ConsoleCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("reward", "blt")]
        [UsedImplicitly]
        public static string SpoofReward(List<string> strings)
        {
            if (BLTModule.TwitchService == null)
            { 
                return "TwitchService is not running";
            }
            var parts = string.Join(" ", strings).Split(',').Select(p => p.Trim()).ToList();
            return BLTModule.TwitchService?.TestRedeem(parts[0], parts.Count > 1 ? parts[1] : "Test User",
                parts.Count > 2 ? parts[2] : null) == true 
                ? $"Tested redemption of {parts[0]}"
                : $"Couldn't test redemption of {parts[0]}, either it doesn't exist, or wasn't enabled";

            // Rewards.RewardManager.Enqueue(reward, Guid.Empty, null);
            //Log.Info("--Current FPS: " + Utilities.GetFps(), 0, Debug.DebugColor.White, 17179869184UL);
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("command", "blt")]
        [UsedImplicitly]
        public static string SpoofCommand(List<string> strings)
        {
            if (BLTModule.TwitchService == null)
            { 
                return "TwitchService is not running";
            }
            var parts = string.Join(" ", strings).Split(',').Select(p => p.Trim()).ToList();
            return BLTModule.TwitchService?.TestCommand(parts[0], parts.Count > 1 ? parts[1] : "Test User",
                parts.Count > 2 ? parts[2] : null) == true 
                ? $"Tested redemption of {parts[0]}"
                : $"Couldn't test command {parts[0]}, either it doesn't exist, or wasn't enabled";

            // Rewards.RewardManager.Enqueue(reward, Guid.Empty, null);
            //Log.Info("--Current FPS: " + Utilities.GetFps(), 0, Debug.DebugColor.White, 17179869184UL);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("simteststart", "blt")]
        [UsedImplicitly]
        public static string SimTest(List<string> strings) =>
            BLTModule.TwitchService?.StartSim() == true 
                ? "Sim started" 
                : "Twitch service isn't started";

        [CommandLineFunctionality.CommandLineArgumentFunction("simteststop", "blt")]
        [UsedImplicitly]
        public static string SimTestStop(List<string> strings) =>
            BLTModule.TwitchService?.StopSim() == true 
                ? "Sim stopped" 
                : "Sim wasn't running";

        [CommandLineFunctionality.CommandLineArgumentFunction("reload", "blt")]
        [UsedImplicitly]
        public static string Reload(List<string> strings)
        {
            if (Campaign.Current == null)
            {
                return "You don't need to reload before the game has started.";
            }
            return BLTModule.RestartTwitchService()
                ? "Success"
                : "Failed";
        }
    }
}