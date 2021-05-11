using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using BannerlordTwitch.Overlay;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
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
		public const string Ver = "1.1.0";
		
		private static readonly Thread thread;
		private static OverlayWindow wnd;
		
		private static Harmony harmony;

		public static TwitchService TwitchService { get; private set; }

		static BLTModule()
		{
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
					wnd = new OverlayWindow();
					wnd.Show();
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
				// RestartTwitchService();
			}
		}

		public static void AddToFeed(string text, Color color)
		{
			wnd?.AddToFeed(text, System.Windows.Media.Color.FromScRgb(color.Alpha, color.Red, color.Green, color.Blue));
		}

		protected override void OnSubModuleUnloaded()
		{
			wnd.Dispatcher.Invoke(() =>
			{
				wnd.Close();
			});
			ActionManager.GenerateDocumentation();
			base.OnSubModuleUnloaded();
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
