using System;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

#pragma warning disable 649

namespace BLTBuffet
{
    public class BLTBuffetModule : MBSubModuleBase
    {
        public const string Name = "BLTBuffet";
        public const string Ver = "1.3";

        public BLTBuffetModule()
        {
            ActionManager.RegisterAll(typeof(BLTBuffetModule).Assembly);
        }
        
        private static Harmony harmony = null;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            if (harmony == null)
            {
                try
                {
                    harmony = new Harmony("mod.bannerlord.bltbuffet");
                    harmony.PatchAll();
                }
                catch (Exception ex)
                {
                    Log.LogFeedCritical($"Error applying patches: {ex.Message}");
                }
            }
        }
    }
}