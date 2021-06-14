using HarmonyLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using BannerlordTwitch.Overlay;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Color = TaleWorlds.Library.Color;

#pragma warning disable IDE0051 // Remove unused private members
namespace BannerlordTwitch
{
	// ReSharper disable once ClassNeverInstantiated.Global
	internal class BLTModule : MBSubModuleBase
	{
		public const string Name = "BannerlordTwitch";
		public const string Ver = "1.4.4";
		
		private static readonly Thread thread;
		private static OverlayWindow overlayWindow;
		
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
				string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
				if (!File.Exists(assemblyPath)) return null;
				var assembly = Assembly.LoadFrom(assemblyPath);
				return assembly;
			};
            
			thread = new Thread(() =>
			{
				try
				{
					overlayWindow = new OverlayWindow { ShowActivated = false };
					overlayWindow.Show();
					System.Windows.Threading.Dispatcher.Run();
				}
				catch (Exception e)
				{
					MessageBox.Show($"Exception occurred (please report this on the discord or nexusmods):\n{e}", "BLT Overlay Window Crashed!");
				}
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.IsBackground = true;
			thread.Start();
		}

		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
			if (harmony == null)
			{
				try
				{
					harmony = new Harmony("mod.bannerlord.bannerlordtwitch");
					harmony.PatchAll();
					Log.LogFeedSystem($"Loaded v{Ver}");

					ActionManager.Init();
					Log.LogFeedSystem("Action Manager initialized");
				}
				catch (Exception ex)
				{
					Log.LogFeedCritical($"Error Initialising Bannerlord Twitch: {ex.Message}");
				}
			}
		}

		public static void AddInfoPanel(Func<UIElement> construct)
		{
			overlayWindow?.AddInfoPanel(construct);
		}
		
		public static void RemoveInfoPanel(UIElement element)
		{
			overlayWindow?.RemoveInfoPanel(element);
		}

		public static void RunInfoPanelUpdate(Action action)
		{
			overlayWindow?.RunInfoPanelUpdate(action);
		}

		public static void AddToFeed(string text, Color color)
		{
			overlayWindow?.AddToFeed(text, System.Windows.Media.Color.FromScRgb(color.Alpha, color.Red, color.Green, color.Blue));
		}

		protected override void OnSubModuleUnloaded()
		{
			overlayWindow.Dispatcher.Invoke(() =>
			{
				overlayWindow.Close();
			});
			overlayWindow.Dispatcher.InvokeShutdown();
			thread.Join(TimeSpan.FromMilliseconds(500));
			ActionManager.GenerateDocumentation();
			base.OnSubModuleUnloaded();
		}

		public override void OnGameLoaded(Game game, object initializerObject)
		{
			base.OnGameLoaded(game, initializerObject);
		}

		public override void BeginGameStart(Game game)
		{
			base.BeginGameStart(game);
		}

		public override void OnCampaignStart(Game game, object starterObject)
		{
			base.OnCampaignStart(game, starterObject);
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
				Log.LogFeedCritical($"TwitchService could not start: {ex.Message}");
				return false;
			}
		}
	}
}
