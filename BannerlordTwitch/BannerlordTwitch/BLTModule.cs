using HarmonyLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using BannerlordTwitch.Models;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTOverlay;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Debug = TaleWorlds.Library.Debug;

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
#if e180
				"e1.8.0"
#else
				#error Version not defined!
#endif
			;

		static BLTModule()
		{
			if (!GameVersion.IsVersion(ExpectedVersion))
			{
				MessageBox.Show("{=IO9rnFpk}This build of the mod is for game version {ExpectedVersion}. You are running game version {GameVersion}. Exiting now."
                    .Translate(
                        ("ExpectedVersion", ExpectedVersion),
                        ("GameVersion", GameVersion.GameVersionString)), 
                    "{=Oru6b9Cy}Bannerlord Twitch ERROR".Translate());
				Application.Current.Shutdown(1);
			}
			
			// Set a consistent Window title so streaming software can find it
			SetWindowText(Process.GetCurrentProcess().MainWindowHandle, "Bannerlord Game Window");

			MainThreadSync.InitMainThread();

            AssemblyHelper.Redirect("Microsoft.Extensions.Logging.Abstractions", Version.Parse("3.1.5.0"), "adb9793829ddae60");
            AssemblyHelper.Redirect("Microsoft.Owin", Version.Parse("4.2.0.0"), "31bf3856ad364e35");
            AssemblyHelper.Redirect("Microsoft.Owin.FileSystems", Version.Parse("4.2.0.0"), "31bf3856ad364e35");
            AssemblyHelper.Redirect("Microsoft.Owin.Security", Version.Parse("4.2.0.0"), "31bf3856ad364e35");
            AssemblyHelper.Redirect("Newtonsoft.Json", Version.Parse("13.0.0.0"), "30ad4fe6b2a6aeed");

            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
			{
				string folderPath = Path.GetDirectoryName(typeof(BLTModule).Assembly.Location);
				string assemblyPath = Path.Combine(folderPath ?? string.Empty, new AssemblyName(args.Name).Name + ".dll");
                if (!File.Exists(assemblyPath))
                {
                    Debug.Print($"[BLT] Couldn't resolve assembly {args.Name} with {assemblyPath}");
                    return null;
                }
                Debug.Print($"[BLT] Resolved assembly {args.Name} with {assemblyPath}");
				return Assembly.LoadFrom(assemblyPath);
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
					Log.LogFeedSystem("{=45Q44kgm}Loaded v{ModVersion}".Translate(
                        ("ModVersion", Assembly.GetExecutingAssembly().GetName().Version.ToString(3))));

					ActionManager.Init();
					Log.LogFeedSystem("{=5G73vqNS}Action Manager initialized".Translate());
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
								"{=PhRzCo9t}Bannerlord Twitch Mod WARNING".Translate(),
								"{=7b4tU6y9}You have auto save disabled, crashes could result in lost channel points!\nRecommended you set it to 15 minutes or less.".Translate(),
								true, false, "{=hpFXglKx}Okay".Translate(), null,
								() => { }, () => { }), true);
					}
					
					CampaignEvents.DailyTickEvent.ClearListeners(ownerHandle);
				});
			}
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			RestartTwitchService();
            
            try
            {
                if (game.GameType is Campaign)
                {
                    gameStarterObject.AddModel(new BLTAgentStatCalculateModel(gameStarterObject.Models
                        .OfType<AgentStatCalculateModel>().FirstOrDefault()));
                }
            }
            catch (Exception e)
            {
                Log.Exception(nameof(OnGameStart), e);
                MessageBox.Show(
                    "{=C0G8s2Lv}Error in {Location}, please report this on the discord: {Error}"
                        .Translate(
                            ("Location", $"BannerlordTwitch.{nameof(OnGameStart)}"),
                            ("Error", e.ToString())
                            ), 
                    "{=cuXwwHRe}Bannerlord Twitch Mod STARTUP ERROR".Translate());
            }
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
		
		public override void OnMissionBehaviorInitialize(Mission mission)
		{
			mission.AddMissionBehavior(new BLTAgentModifierBehavior());
			mission.AddMissionBehavior(new BLTAgentPfxBehaviour());
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
						"{=Sphd7XTS}Bannerlord Twitch Mod DISABLED".Translate(),
						ex.Message,
						true, false, "{=hpFXglKx}Okay".Translate(), null,
						() => {}, () => {}), true);
				TwitchService = null;
				Log.Exception($"TwitchService could not start: {ex.Message}", ex);
				return false;
			}
		}
	}
}
