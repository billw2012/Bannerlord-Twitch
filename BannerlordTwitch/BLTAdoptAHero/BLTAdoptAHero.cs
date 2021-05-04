using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

#pragma warning disable 649

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    public class BLTAdoptAHeroModule : MBSubModuleBase
    {
        public const string Name = "BLTAdoptAHero";
        public const string Ver = "1.0.1";

        public BLTAdoptAHeroModule()
        {
            ActionManager.RegisterAll(typeof(BLTAdoptAHeroModule).Assembly);
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            static string KillDetailVerb(KillCharacterAction.KillCharacterActionDetail detail)
            {
                switch (detail)
                {
                    case KillCharacterAction.KillCharacterActionDetail.Murdered:
                        return "was murdered";
                    case KillCharacterAction.KillCharacterActionDetail.DiedInLabor:
                        return "died in labor";
                    case KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge:
                        return "died of old age";
                    case KillCharacterAction.KillCharacterActionDetail.DiedInBattle:
                        return "died in battle";
                    case KillCharacterAction.KillCharacterActionDetail.WoundedInBattle:
                        return "was wounded in battle";
                    case KillCharacterAction.KillCharacterActionDetail.Executed:
                        return "was executed";
                    case KillCharacterAction.KillCharacterActionDetail.Lost:
                        return "was lost";
                    default:
                    case KillCharacterAction.KillCharacterActionDetail.None:
                        return "was ended";
                }
            }
            base.OnGameStart(game, gameStarterObject);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, (victim, killer, detail, _) =>
            {
                if (victim?.IsAdopted() == true || killer?.IsAdopted() == true)
                {
                    string verb = KillDetailVerb(detail);
                    if (killer != null && victim != null)
                    {
                        Log.LogFeedEvent($"{victim.Name} {verb} by {killer.Name}!");
                    }
                    else if (killer != null)
                    {
                        Log.LogFeedEvent($"{killer.Name} {verb}!");
                    }
                }
            });
            CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, (hero, _) =>
            {
                if (hero.IsAdopted())
                    Log.LogFeedEvent($"{hero.Name} is now level {hero.Level}!");
            });
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, (party, hero) =>
            {
                if (hero.IsAdopted())
                {
                    if(party != null)
                        Log.LogFeedEvent($"{hero.Name} was taken prisoner by {party.Name}!");
                    else
                        Log.LogFeedEvent($"{hero.Name} was taken prisoner!");
                }
            });
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, (hero, party, _, _) =>
            {
                if (hero.IsAdopted())
                {
                    if(party != null)
                        Log.LogFeedEvent($"{hero.Name} is no longer a prisoner of {party.Name}!");
                    else
                        Log.LogFeedEvent($"{hero.Name} is no longer a prisoner!");
                }
            });
            CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, (hero, clan) =>
            {
                if(hero.IsAdopted())
                    Log.LogFeedEvent($"{hero.Name} is now a member of {clan?.Name.ToString() ?? "no clan"}!");
            });
        }

        // public override void BeginGameStart(Game game)
        // {
        //     base.BeginGameStart(game);
        // }
        //
        // public override void OnCampaignStart(Game game, object starterObject)
        // {
        //     base.OnCampaignStart(game, starterObject);
        // }

        public override void OnGameEnd(Game game)
        {
            base.OnGameEnd(game);
        }

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