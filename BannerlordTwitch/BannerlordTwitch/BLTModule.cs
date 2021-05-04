using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Media;
using BannerlordTwitch.Overlay;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using TaleWorlds.MountAndBlade;
using Color = TaleWorlds.Library.Color;

#pragma warning disable IDE0051 // Remove unused private members
namespace BannerlordTwitch
{
	// ReSharper disable once ClassNeverInstantiated.Global
	internal class BLTModule : MBSubModuleBase
	{
		public const string Name = "BannerlordTwitch";
		public const string Ver = "0.2.1";
		
		private static readonly Thread thread;
		private static OverlayWindow wnd;
		
		private static Harmony harmony = null;

		public static TwitchService TwitchService;

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
				wnd = new OverlayWindow();
				wnd.Show();
				System.Windows.Threading.Dispatcher.Run();
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.IsBackground = true;
			thread.Start();
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
			TwitchService?.Exit();
			base.OnSubModuleUnloaded();
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
					
					TwitchService = new TwitchService();
					Log.LogFeedSystem("API initialized");

					ActionManager.Init();
					Log.LogFeedSystem("Reward Manager initialized");
				}
				catch (Exception ex)
				{
					Log.LogFeedCritical($"Error Initialising Bannerlord Twitch: {ex.Message}");
				}
			}
		}

		protected override void OnApplicationTick(float dt)
		{
			base.OnApplicationTick(dt);
			MainThreadSync.RunQueued();
		}
	}
}
