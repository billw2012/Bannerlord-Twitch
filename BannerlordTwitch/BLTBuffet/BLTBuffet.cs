using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public const string Ver = "1.4.1";

        internal static GlobalEffectsConfig EffectsConfig { get; private set; }

        public BLTBuffetModule()
        {
            ActionManager.RegisterAll(typeof(BLTBuffetModule).Assembly);
            GlobalEffectsConfig.Register();
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
        
        internal class GlobalEffectsConfig
        {
            private const string ID = "Buffet - Effects Config";
            internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalEffectsConfig));
            internal static GlobalEffectsConfig Get() => ActionManager.GetGlobalConfig<GlobalEffectsConfig>(ID);
        
            [Description("Whether effects are disabled when a tournament is active (to stop 'cheating')")] 
            public bool DisableEffectsInTournaments { get; set; } = true;
        }
    }
}