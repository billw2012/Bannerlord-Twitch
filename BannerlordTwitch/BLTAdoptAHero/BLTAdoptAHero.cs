using BannerlordTwitch.Rewards;
using HarmonyLib;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

#pragma warning disable 649

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    public class BLTAdoptAHeroModule : MBSubModuleBase
    {
        private Harmony harmony;
        public const string Name = "BLTAdoptAHero";
        public const string Ver = "1.0.2";

        public BLTAdoptAHeroModule()
        {
            ActionManager.RegisterAll(typeof(BLTAdoptAHeroModule).Assembly);
        }
        
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            if (harmony == null)
            {
                harmony = new Harmony("mod.bannerlord.bltadoptahero");
                harmony.PatchAll();
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if(game.GameType is Campaign) 
            {
                var campaignStarter = (CampaignGameStarter) gameStarterObject;
                campaignStarter.AddBehavior(new BLTAdoptAHeroCampaignBehavior());
            }
        }

        // public override void BeginGameStart(Game game)
        // {
        //     base.BeginGameStart(game);
        // }
        
        // public override void OnCampaignStart(Game game, object starterObject)
        // {
        //     base.OnCampaignStart(game, starterObject);
        //     // JoinTournament.SetupGameMenus(starterObject as CampaignGameStarter);
        // }
        //
        // public override void OnGameEnd(Game game)
        // {
        //     base.OnGameEnd(game);
        // }

        internal const string Tag = "[BLT]";
    }

    // We could do this, but they could also gain money so...
    // public static class Patches
    // {
    //     [HarmonyPrefix]
    //     [HarmonyPatch(typeof(Hero), nameof(Hero.Gold), MethodType.Setter)]
    //     public static bool set_GoldPrefix(Hero __instance, int value)
    //     {
    //         // Don't allow changing gold of our adopted heroes, as we use it ourselves
    //         return !__instance.GetName().Contains(AdoptAHero.Tag);
    //     }
    // }
}