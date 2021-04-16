using HarmonyLib;
using System;
using System.Windows.Forms;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

#pragma warning disable IDE0051 // Remove unused private members
namespace BannerlordTwitch
{
	public class SubModule : MBSubModuleBase
	{
		private static Harmony harmony = null;
		private Settings settings;

		public SubModule()
		{
			AssemblyHelper.Redirect("Newtonsoft.Json", Version.Parse("13.0.0.0"), "30ad4fe6b2a6aeed");
			AssemblyHelper.Redirect("Microsoft.Extensions.Logging.Abstractions", Version.Parse("3.1.5.0"), "adb9793829ddae60");
			
			settings = Settings.Load();
		}
		
		// protected override void OnBeforeInitialModuleScreenSetAsRoot()
		// {
		// 	if (harmony == null)
		// 	{
		// 		try
		// 		{
		// 			harmony = new Harmony("mod.bannerlord.bannerlordtwitch");
		// 			harmony.PatchAll();
		// 		}
		// 		catch (Exception ex)
		// 		{
		// 			MessageBox.Show($"Error Initialising Bannerlord Twitch:\n\n{ex}");
		// 		}
		// 	}
		// }

		protected override void OnApplicationTick(float dt)
		{
			base.OnApplicationTick(dt);
			MainThreadSync.Run();
		}

		private static TwitchService subListener;
		
		protected override void OnSubModuleLoad()
		{
		 	base.OnSubModuleLoad();
		    
		    try
		    {
			    harmony = new Harmony("mod.bannerlord.bannerlordtwitch");
			    harmony.PatchAll();
		    }
		    catch (Exception ex)
		    {
			    MessageBox.Show($"Error Initialising Bannerlord Twitch:\n\n{ex}");
		    }
		    
			// Module.CurrentModule.AddInitialStateOption(new InitialStateOption("Message",
			// 	new TextObject("Message", null),
			// 	9990,
			// 	() => { InformationManager.DisplayMessage(new InformationMessage("Hello World!")); },
			// 	() => false));
		
			subListener = new TwitchService(settings.AccessToken);
		}

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
