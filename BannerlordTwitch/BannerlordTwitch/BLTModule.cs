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

		public BLTModule()
		{
			MainThreadSync.InitMainThread();
			AssemblyHelper.Redirect("Newtonsoft.Json", Version.Parse("13.0.0.0"), "30ad4fe6b2a6aeed");
			AssemblyHelper.Redirect("Microsoft.Extensions.Logging.Abstractions", Version.Parse("3.1.5.0"), "adb9793829ddae60");
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

		public static TwitchService TwitchService;
		
		// protected override void OnSubModuleLoad()
		// {
		//  	base.OnSubModuleLoad();
		//     
		//     // try
		//     // {
		// 	   //  harmony = new Harmony("mod.bannerlord.bannerlordtwitch");
		// 	   //  harmony.PatchAll();
		//     // }
		//     // catch (Exception ex)
		//     // {
		// 	   //  MessageBox.Show($"Error Initialising Bannerlord Twitch:\n\n{ex}");
		//     // }
		//     
		// 	// Module.CurrentModule.AddInitialStateOption(new InitialStateOption("Message",
		// 	// 	new TextObject("Message", null),
		// 	// 	9990,
		// 	// 	() => { InformationManager.DisplayMessage(new InformationMessage("Hello World!")); },
		// 	// 	() => false));
		//
		// }

		// public override void OnGameLoaded(Game game, object initializerObject)
		// {
		// 	base.OnGameLoaded(game, initializerObject);
		// }

		// public override void OnCampaignStart(Game game, object starterObject)
		// {
		// 	base.OnCampaignStart(game, starterObject);
		// }
		
		//public override void OnGameEnd(Game game)
		//{
		//	base.OnGameEnd(game);
		//	BannerlordTwitch.Log("BannersampleSubModule.OnGameEnd");
		//}

		//protected override void OnSubModuleLoad()
		//{
		//	base.OnSubModuleLoad();
		//	BannerlordTwitch.Log("BannersampleSubModule.OnSubModuleLoad");

		//	// Null checks a method from the Army of Poachers Quest to prevent a bug fixed in 1.0.1
		//	// Bannersample.Prefix(
		//	//   "TaleWorlds.CampaignSystem.SandBox.Issues.MerchantArmyOfPoachersIssueBehavior",
		//	//   "MerchantArmyOfPoachersIssueQuest",
		//	//   "OnFinalize",
		//	//   "Bannersample.BannersampleSubModule",
		//	//   "OnFinalize"
		//	// );

		//	// Silently catches an exception occurring on formation change in a campaign siege as of 1.0.2
		//	// Bannersample.Finalize(
		//	//   "TaleWorlds.MountAndBlade.DetachmentManager",
		//	//   "Team_OnFormationsChanged",
		//	//   "Bannersample.Bannersample",
		//	//   "Catch"
		//	// );
		//}

		//public static bool OnFinalize(Object __instance)
		//{
		//	Type instType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.SandBox.Issues.MerchantArmyOfPoachersIssueBehavior");
		//	Traverse t = Traverse.Create(__instance);
		//	MobileParty _poachersParty = t.Field("_poachersParty").GetValue<MobileParty>();
		//	if (_poachersParty == null)
		//	{
		//		Bannersample.Log("_poachersParty NULL!");
		//		return false;
		//	}
		//	return true;
		//}
	}
}
