using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox.TournamentMissions.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [HarmonyPatch, Description("Puts adopted heroes in queue for the next tournament"), UsedImplicitly]
    internal class JoinTournament : ActionHandlerBase
    {
        private class Settings
        {
            [Category("General"), Description("Gold cost to summon"), PropertyOrder(1)]
            public int GoldCost { get; set; }
            [Category("Effects"), Description("Gold won if the hero win the tournaments"), PropertyOrder(2)]
            public int WinGold { get; set; }
            [Category("Effects"), Description("XP given if the hero win the tournaments"), PropertyOrder(2)]
            public int WinXP { get; set; }
            [Category("Effects"), Description("Gold the hero gets for every kill"), PropertyOrder(4)]
            public int GoldPerKill { get; set; }
            [Category("Effects"), Description("XP the hero gets for every kill. It will be distributed using the Auto behavior of the SkillXP action: randomly between the top skills from each skill group (melee, ranged, movement, support, personal)."), PropertyOrder(5)]
            public int XPPerKill { get; set; }
            [Category("Effects"), Description("HP the hero gets for every kill"), PropertyOrder(6)]
            public int HealPerKill { get; set; }
            [Category("Effects"), Description("Multiplier applied to effects for subscribers"), PropertyOrder(8)]
            public float SubBoost { get; set; } = 1;
        }
        
        protected override Type ConfigType => typeof(Settings);

        private static readonly List<(ReplyContext context, Settings settings, Hero hero)> tournamentQueue = new();

        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Settings) config;

            var adoptedHero = AdoptAHero.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            if (tournamentQueue.Count >= 15)
            {
                onFailure("The tournament queue is full!");
                return;
            }
            if (tournamentQueue.Any(sh => sh.hero == adoptedHero))
            {
                onFailure("You are already in the tournament queue!");
                return;
            }
            if (adoptedHero.Gold < settings.GoldCost)
            {
                onFailure($"You do not have enough gold: you need {settings.GoldCost}, and you only have {adoptedHero.Gold}!");
                return;
            }
            adoptedHero.Gold -= settings.GoldCost;
            tournamentQueue.Add((context, settings, adoptedHero));

            if (tournamentQueue.Count == 15)
            {
                ActionManager.SendReply(context, $"You are in the tournament queue, no spots remaining");
                InformationManager.AddQuickInformation(new TextObject("The viewer tournament is ready! To start it, just join any active tournament."), 1000, null, "event:/ui/mission/horns/attack");
            }
            else
            {
                ActionManager.SendReply(context, $"You are in the tournament queue, {15 - tournamentQueue.Count} spots remaining");
            }
        }

        // MissionState.Current.CurrentMission doesn't have any behaviours added during this function, so we split the initialization that requires access
        // to mission behaviours into another patch below
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentGame), nameof(TournamentGame.GetParticipantCharacters))]
        public static void GetParticipantCharactersPostfix(Settlement settlement,
            int maxParticipantCount, bool includePlayer, List<CharacterObject> __result)
        {
            if (Settlement.CurrentSettlement == settlement && includePlayer && maxParticipantCount == 16)
            {
                //__result.RemoveAll(c => c.HeroObject != Hero.MainHero);
                __result.Remove(Hero.MainHero.CharacterObject);
                __result.RemoveRange(0, Math.Min(__result.Count, JoinTournament.tournamentQueue.Count));

                __result.Add(Hero.MainHero.CharacterObject);
                __result.AddRange(JoinTournament.tournamentQueue.Select(q => q.hero.CharacterObject));
            }
        }
        
        // After PrepareForTournamentGame the MissionState.Current.CurrentMission contains the behaviors
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentGame), nameof(TournamentGame.PrepareForTournamentGame))]
        public static void PrepareForTournamentGamePostfix(TournamentGame __instance, bool isPlayerParticipating)
        {
            if (isPlayerParticipating)
            {
                var tournamentBehaviour = MissionState.Current.CurrentMission.GetMissionBehaviour<TournamentBehavior>();
                foreach (var (context, settings, adoptedHero) in tournamentQueue)
                {
                    float actualBoost = context.IsSubscriber ? settings.SubBoost : 1;

                    // Win results
                    tournamentBehaviour.TournamentEnd += () =>
                    {
                        if (tournamentBehaviour.LastMatch.Winners.Any(w => w.Character?.HeroObject == adoptedHero))
                        {
                            // User gets their gold back also
                            adoptedHero.ChangeHeroGold((int) (settings.WinGold * actualBoost + settings.GoldCost));
                            ActionManager.SendReply(context, $@"You won {settings.WinGold} gold!");
                            
                            int xp = (int) (settings.WinXP * actualBoost);
                            (bool success, string description) = SkillXP.ImproveSkill(adoptedHero, xp, Skills.All, random: false, auto: true);
                            if (success)
                            {
                                Log.LogFeedBattle($"{adoptedHero.FirstName}: {description}");
                            }
                        }
                    };
                    
                    BLTMissionBehavior.Current.AddListeners(adoptedHero,
                        onGotAKill: (killer, killed, state) =>
                        {
                            if (killed != null)
                            {
                                Log.LogFeedBattle($"{adoptedHero.FirstName} {BLTMissionBehavior.KillStateVerb(state)} {killed.Name}");
                            }

                            if (settings.GoldPerKill != 0)
                            {
                                int gold = (int) (settings.GoldPerKill * actualBoost);
                                adoptedHero.ChangeHeroGold(gold);
                                Log.LogFeedBattle($"{adoptedHero.FirstName}: +{gold} gold");
                            }

                            if (settings.HealPerKill != 0)
                            {
                                float prevHealth = killer.Health;
                                killer.Health = Math.Min(killer.HealthLimit, killer.Health + settings.HealPerKill * actualBoost);
                                float healthDiff = killer.Health - prevHealth;
                                if(healthDiff > 0)
                                    Log.LogFeedBattle($"{adoptedHero.FirstName}: +{healthDiff}hp");
                            }

                            if (settings.XPPerKill != 0)
                            {
                                int xp = (int) (settings.XPPerKill * actualBoost);
                                (bool success, string description) = SkillXP.ImproveSkill(adoptedHero, xp, Skills.All, random: false, auto: true);
                                if(success)
                                    Log.LogFeedBattle($"{adoptedHero.FirstName}: {description}");
                            }
                        },
                        onGotKilled: (_, killer, state) =>
                        {
                            Log.LogFeedBattle(killer != null
                                ? $"{adoptedHero.FirstName} was {BLTMissionBehavior.KillStateVerb(state)} by {killer.Name}"
                                : $"{adoptedHero.FirstName} was {BLTMissionBehavior.KillStateVerb(state)}");
                        }
                    );
                }
                
                tournamentQueue.Clear();
            }
        }
    }
}
