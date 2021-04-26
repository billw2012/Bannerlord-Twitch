using HarmonyLib;
using System;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using TaleWorlds.MountAndBlade;

#pragma warning disable IDE0051 // Remove unused private members
namespace BannerlordTwitch
{
	// ReSharper disable once ClassNeverInstantiated.Global
	internal class BLTModule : MBSubModuleBase
	{
		public const string Name = "BannerlordTwitch";
		public const string Ver = "0.1.1";
		
		private static Harmony harmony = null;

		public static TwitchService TwitchService;

		public BLTModule()
		{
			MainThreadSync.InitMainThread();
			AssemblyHelper.Redirect("Newtonsoft.Json", Version.Parse("13.0.0.0"), "30ad4fe6b2a6aeed");
			AssemblyHelper.Redirect("Microsoft.Extensions.Logging.Abstractions", Version.Parse("3.1.5.0"), "adb9793829ddae60");
		}

		public override void OnSubModuleUnloaded()
		{
			RewardManager.GenerateDocumentation();
			base.OnSubModuleUnloaded();
		}

		public override void OnBeforeInitialModuleScreenSetAsRoot()
		{
			if (harmony == null)
			{
				try
				{
					harmony = new Harmony("mod.bannerlord.bannerlordtwitch");
					harmony.PatchAll();
					Log.Screen($"Loaded v{Ver}");
					
					TwitchService = new TwitchService();
					Log.Screen("API initialized");

					RewardManager.Init();
					Log.Screen("Reward Manager initialized");
				}
				catch (Exception ex)
				{
					Log.ScreenCritical($"Error Initialising Bannerlord Twitch: {ex.Message}");
				}
			}
		}

		public override void OnApplicationTick(float dt)
		{
			base.OnApplicationTick(dt);
			MainThreadSync.RunQueued();
		}
	}
}
