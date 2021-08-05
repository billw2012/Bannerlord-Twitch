using HarmonyLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTOverlay;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

#pragma warning disable IDE0051 // Remove unused private members
namespace BannerlordTwitch
{
	// ReSharper disable once ClassNeverInstantiated.Global
	internal class BLTModule : MBSubModuleBase
	{
        private static Harmony harmony;

		public static TwitchService TwitchService { get; private set; }

		[DllImport("user32.dll")]
		private static extern int SetWindowText(IntPtr hWnd, string text);

		private const string ExpectedVersion =
#if e159
				"e1.5.9"
#elif e1510
				"e1.5.10"
#elif e160
				"e1.6.0"
#elif e161
				"e1.6.1"
#endif
			;

		static BLTModule()
		{
			if (!GameVersion.IsVersion(ExpectedVersion))
			{
				MessageBox.Show($"This build of the mod is for game version {ExpectedVersion}. You are running game version {GameVersion.GameVersionString}. Exiting now.", "Bannerlord Twitch ERROR");
				Application.Current.Shutdown(1);
			}
			
			// Set a consistent Window title so streaming software can find it
			SetWindowText(Process.GetCurrentProcess().MainWindowHandle, "Bannerlord Game Window");

			MainThreadSync.InitMainThread();
			AssemblyHelper.Redirect("Newtonsoft.Json", Version.Parse("13.0.0.0"), "30ad4fe6b2a6aeed");
			AssemblyHelper.Redirect("Microsoft.Extensions.Logging.Abstractions", Version.Parse("3.1.5.0"), "adb9793829ddae60");
			
			AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
			{
				string folderPath = Path.GetDirectoryName(typeof(BLTModule).Assembly.Location);
				string assemblyPath = Path.Combine(folderPath ?? string.Empty, new AssemblyName(args.Name).Name + ".dll");
				if (!File.Exists(assemblyPath)) return null;
				var assembly = Assembly.LoadFrom(assemblyPath);
				return assembly;
			};
        }

		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
			if (harmony == null)
			{
				try
				{
					harmony = new Harmony("mod.bannerlord.bannerlordtwitch");
					harmony.PatchAll();
					Log.LogFeedSystem($"Loaded v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}");

					ActionManager.Init();
					Log.LogFeedSystem("Action Manager initialized");
				}
				catch (Exception ex)
				{
					Log.Exception($"Error Initialising Bannerlord Twitch: {ex.Message}", ex);
				}

                ConsoleFeedHub.Register();
                
                BLTOverlay.BLTOverlay.Start();
            }
		}

		public static void AddToFeed(string text, string style)
        {
            ConsoleFeedHub.SendMessage(text, style);
		}

		public override void OnGameInitializationFinished(Game game)
		{
			if(game.GameType is Campaign)
			{
				object ownerHandle = new object();
				CampaignEvents.DailyTickEvent.AddNonSerializedListener(ownerHandle, () =>
				{
					if (
#if e159
						CampaignOptions.AutoSaveInMinutes
#else
						Campaign.Current.SaveHandler.AutoSaveInterval
#endif
						<= 0
					)
					{
						InformationManager.ShowInquiry(
							new InquiryData(
								"Bannerlord Twitch Mod WARNING",
								$"You have auto save disabled, crashes could result in lost channel points!\n" +
								$"Recommended you set it to 15 minutes or less.",
								true, false, "Okay", null,
								() => { }, () => { }), true);
					}
					
					CampaignEvents.DailyTickEvent.ClearListeners(ownerHandle);
				});
			}
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			RestartTwitchService();
		}

		protected override void OnApplicationTick(float dt)
		{
			base.OnApplicationTick(dt);
			MainThreadSync.RunQueued();
		}

		public override void OnGameEnd(Game game)
		{
			TwitchService?.Dispose();
			TwitchService = null;
		}
		
		public override void OnMissionBehaviourInitialize(Mission mission)
		{
			mission.AddMissionBehaviour(new BLTAgentModifierBehavior());
			mission.AddMissionBehaviour(new BLTAgentPfxBehaviour());
		}

		public static bool RestartTwitchService()
		{
			TwitchService?.Dispose();
			try
			{
				TwitchService = new TwitchService();
				return true;
			}
			catch (Exception ex)
			{
				InformationManager.ShowInquiry(
					new InquiryData(
						"Bannerlord Twitch Mod DISABLED",
						ex.Message,
						true, false, "Okay", null,
						() => {}, () => {}), true);
				TwitchService = null;
				Log.Exception($"TwitchService could not start: {ex.Message}", ex);
				return false;
			}
		}
	}
}
