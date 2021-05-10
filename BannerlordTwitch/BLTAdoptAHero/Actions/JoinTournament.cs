using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox;
using SandBox.TournamentMissions.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [HarmonyPatch, Description("Puts adopted heroes in queue for the next tournament"), UsedImplicitly]
    internal class JoinTournament : ActionHandlerBase
    {
        [CategoryOrder("General", 1)]
        [CategoryOrder("Tournament Effects", 2)]
        [CategoryOrder("Match Effects", 3)]
        [CategoryOrder("Kill Effects", 4)]
        private class Settings
        {
            [Category("General"), Description("Whether the tournament will include the player or not"), PropertyOrder(1)]
            public bool ViewersOnly { get; set; }
            
            [Category("General"), Description("Whether the hero will start the tournament with full HP"), PropertyOrder(2)]
            public bool StartWithFullHP { get; set; }
            
            [Category("General"), Description("Gold cost to join"), PropertyOrder(3)]
            public int GoldCost { get; set; }
            
            [Category("General"), Description("Multiplier applied to all effects for subscribers"), PropertyOrder(4)]
            public float SubBoost { get; set; } = 1;

            [Category("Tournament Effects"), Description("Gold won if the hero wins the tournaments"), PropertyOrder(1)]
            public int WinGold { get; set; }
            [Category("Tournament Effects"), Description("XP given if the hero wins the tournaments"), PropertyOrder(2)]
            public int WinXP { get; set; }
            [Category("Tournament Effects"), Description("XP given if the hero participates in a tournament"), PropertyOrder(3)]
            public int ParticipateXP { get; set; }
            
            [Category("Match Effects"), Description("Gold won if the hero wins their match"), PropertyOrder(1)]
            public int WinMatchGold { get; set; }
            [Category("Match Effects"), Description("XP given if the hero wins their match"), PropertyOrder(2)]
            public int WinMatchXP { get; set; }
            [Category("Match Effects"), Description("XP given if the hero participates in a match"), PropertyOrder(3)]
            public int ParticipateMatchXP { get; set; }
            
            [Category("Kill Effects"), Description("Gold the hero gets for every kill"), PropertyOrder(1)]
            public int GoldPerKill { get; set; }
            [Category("Kill Effects"), Description("XP the hero gets for every kill"), PropertyOrder(2)]
            public int XPPerKill { get; set; }
            [Category("Kill Effects"), Description("XP the hero gets when killed"), PropertyOrder(3)]
            public int XPPerKilled { get; set; }
            [Category("Kill Effects"), Description("HP the hero gets for every kill"), PropertyOrder(4)]
            public int HealPerKill { get; set; }
            [Category("Kill Effects"), Description("How much to scale the reward by, based on relative level of the two characters. If this is 0 (or not set) then the rewards are always as specified, if this is higher than 0 then the rewards increase if the killed unit is higher level than the hero, and decrease if it is lower. At a value of 0.5 (recommended) at level difference of 10 would give about 2.5 times the normal rewards for gold, xp and health."), PropertyOrder(5)]
            public float? RelativeLevelScaling { get; set; }
            [Category("Kill Effects"), Description("Caps the maximum multiplier for the level difference, defaults to 5 if not specified"), PropertyOrder(6)]
            public float? LevelScalingCap { get; set; }
        }
        
        protected override Type ConfigType => typeof(Settings);

        private static readonly List<(ReplyContext context, Settings settings, Hero hero)> tournamentQueue = new();
        private static readonly List<(ReplyContext context, Settings settings, Hero hero)> viewerTournamentQueue = new();
        private static readonly List<(ReplyContext context, Settings settings, Hero hero)> currentTournament = new();

        private static readonly List<(Hero adoptedHero, Hero targetHero, int bet)> placedBets = new();

        private static bool doViewerTournament = false;

        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Settings) config;
            
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            
            void AddToQueue(List<(ReplyContext context, Settings settings, Hero hero)> queue, int max, string name, string instructions)
            {
                if (queue.Count >= max)
                {
                    onFailure($"The {name} queue is full!");
                    return;
                }

                if (queue.Any(sh => sh.hero == adoptedHero))
                {
                    onFailure($"You are already in the {name} queue!");
                    return;
                }

                int availableGold = BLTAdoptAHeroCampaignBehavior.Get().GetHeroGold(adoptedHero);
                if (availableGold < settings.GoldCost)
                {
                    onFailure($"You do not have enough gold: you need {settings.GoldCost}, and you only have {availableGold}!");
                    return;
                }
                
                BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(adoptedHero, settings.GoldCost);

                queue.Add((context, settings: settings, hero: adoptedHero));
                if (queue.Count == max)
                {
                    ActionManager.SendReply(context, $"You are in the {name} queue, no spots remaining");
                    InformationManager.AddQuickInformation(new TextObject($"The {name} is ready! To start it, just {instructions}."),
                        1000, null, "event:/ui/mission/horns/attack");
                }
                else
                {
                    ActionManager.SendReply(context, $"You are in the {name} queue, {max - queue.Count} spots remaining");
                }
            }

            if (settings.ViewersOnly)
            {
                AddToQueue(viewerTournamentQueue, 16, "viewer tournament", "go to the nearest arena");
            }
            else
            {
                AddToQueue(tournamentQueue, 15, "tournament", "join the nearest active tournament");
            }
        }

        public static void SetupGameMenus(CampaignGameStarter campaignGameSystemStarter)
        {
            campaignGameSystemStarter.AddGameMenuOption(
                "town_arena", "blt_viewer_tournament", "Watch the viewer tournament", 
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                    return viewerTournamentQueue.Any();
                },
                JoinViewerTournament, 
                index: 2);
        }

        private static ItemObject FindRandomTieredEquipment(int tier, params ItemObject.ItemTypeEnum[] itemTypeEnums)
        {
            var items = ItemObject.All
                // Usable
                .Where(item => !item.NotMerchandise)
                // Correct type
                .Where(item => itemTypeEnums.Contains(item.Type))
                .ToList();

            // Correct tier
            var tieredItems = items.Where(item => (int) item.Tier == tier).ToList();

            // We might not find an item at the specified tier, so find the closest tier we can
            while (!tieredItems.Any() && tier >= 0)
            {
                tier--;
                tieredItems = items.Where(item => (int) item.Tier == tier).ToList();
            }

            return tieredItems.SelectRandom();
        }
        
        private static void JoinViewerTournament(MenuCallbackArgs args)
        {
            doViewerTournament = true;
            var tournamentGame = Campaign.Current.Models.TournamentModel.CreateTournament(Settlement.CurrentSettlement.Town);
            // AccessTools.Property(typeof(TournamentGame), "Prize").SetValue(tournamentGame, DefaultItems.Charcoal);
            GameMenu.SwitchToMenu("town");
            tournamentGame.PrepareForTournamentGame(false);
        }

        // MissionState.Current.CurrentMission doesn't have any behaviours added during this function, so we split the initialization that requires access
        // to mission behaviours into another patch below
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentGame), nameof(TournamentGame.GetParticipantCharacters))]
        public static void GetParticipantCharactersPostfix(Settlement settlement,
            int maxParticipantCount, bool includePlayer, List<CharacterObject> __result)
        {
            if (Settlement.CurrentSettlement == settlement && maxParticipantCount == 16)
            {
                if (includePlayer)
                {
                    //__result.RemoveAll(c => c.HeroObject != Hero.MainHero);
                    __result.Remove(Hero.MainHero.CharacterObject);
                    __result.RemoveRange(0, Math.Min(__result.Count, tournamentQueue.Count));

                    __result.Add(Hero.MainHero.CharacterObject);
                    __result.AddRange(tournamentQueue.Select(q => q.hero.CharacterObject));
                }
                else if(doViewerTournament)
                {
                    __result.RemoveRange(0, Math.Min(__result.Count, viewerTournamentQueue.Count));
                    __result.AddRange(viewerTournamentQueue.Select(q => q.hero.CharacterObject));
                }
            }
        }
        
        // After PrepareForTournamentGame the MissionState.Current.CurrentMission contains the behaviors
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentGame), nameof(TournamentGame.PrepareForTournamentGame))]
        public static void PrepareForTournamentGamePostfix(TournamentGame __instance, bool isPlayerParticipating)
        {
            static (bool used, string failReason) UpgradeToItem(Hero hero, ItemObject item)
            {
                if (EquipHero.CanUseItem(item, hero))
                {
                    // Find a slot
                    var slot = hero.BattleEquipment
                        .YieldEquipmentSlots()
                        .Cast<(EquipmentElement element, EquipmentIndex index)?>()
                        .FirstOrDefault(e
                            => e.HasValue && Equipment.IsItemFitsToSlot(e.Value.index, item)
                               && (e.Value.element.IsEmpty || e.Value.element.Item.Type == item.Type && e.Value.element.Item.Tierf <= item.Tierf));
                    if (slot.HasValue)
                    {
                        hero.BattleEquipment[slot.Value.index] = new EquipmentElement(item);
                        return (true, null);
                    }
                    else
                    {
                        return (false, "your existing equipment is better");
                    }
                }
                else
                {
                    return (false, "you can't use this item");
                }
            }

            if (isPlayerParticipating || doViewerTournament)
            {
                var tournamentBehaviour = MissionState.Current.CurrentMission.GetMissionBehaviour<TournamentBehavior>();
                // var tournamentFightMissionController = MissionState.Current.CurrentMission.GetMissionBehaviour<TournamentFightMissionController>();
                
                var queue = doViewerTournament ? viewerTournamentQueue : tournamentQueue;
                
                currentTournament.Clear();
                currentTournament.AddRange(queue);
                queue.Clear();
                
                float SettingsSubBoost(ReplyContext replyContext, Settings settings)
                {
                    return replyContext.IsSubscriber ? settings.SubBoost : 1;
                }

                tournamentBehaviour.TournamentEnd += () =>
                {
                    // Win results
                    foreach (var (context, settings, hero) in currentTournament)
                    {
                        float actualBoost = SettingsSubBoost(context, settings);
                        var results = new List<string>();
                        if(tournamentBehaviour.Winner.Character?.HeroObject == hero)
                        {
                            results.Add("WINNER!");
                            // Winner gets their gold back also
                            int actualGold = (int) (settings.WinGold * actualBoost + settings.GoldCost);
                            if (actualGold > 0)
                            {
                                BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(hero, actualGold);
                                results.Add($"+{actualGold} gold");
                            }
                            int xp = (int) (settings.WinXP * actualBoost);
                            if (xp > 0)
                            {
                                (bool success, string description) = SkillXP.ImproveSkill(hero, xp, Skills.All,
                                    random: false, auto: true);
                                if (success)
                                {
                                    results.Add(description);
                                }
                            }

                            var prize = tournamentBehaviour.TournamentGame.Prize;
                            (bool upgraded, string failReason) = UpgradeToItem(hero, prize);
                            if (!upgraded)
                            {
                                BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(hero, prize.Value);
                                results.Add($"sold {prize.Name} for {prize.Value} gold ({failReason})");
                            }
                            else
                            {
                                results.Add($"received {prize.Name}");
                            }
                        }
                        else
                        {
                            int xp = (int) (settings.ParticipateXP * actualBoost);
                            if (xp > 0)
                            {
                                (bool success, string description) =
                                    SkillXP.ImproveSkill(hero, xp, Skills.All, random: false, auto: true);
                                if (success)
                                {
                                    results.Add(description);
                                }
                            }
                        }
                        if (results.Any())
                        {
                            ActionManager.SendReply(context, results.ToArray());
                        }
                    }

                    doViewerTournament = false;
                };

                foreach (var (context, settings, hero) in currentTournament)
                {
                    float actualBoost = SettingsSubBoost(context, settings);

                    // Kill effects
                    BLTMissionBehavior.Current.AddListeners(hero,
                        onGotAKill: (killer, killed, state) =>
                        {
                            var results = BLTMissionBehavior.ApplyKillEffects(
                                hero, killer, killed, state,
                                settings.GoldPerKill,
                                settings.HealPerKill, 
                                settings.XPPerKill,
                                actualBoost,
                                settings.RelativeLevelScaling,
                                settings.LevelScalingCap
                                );
                            
                            if (results.Any())
                            {
                                ActionManager.SendReply(context, results.ToArray());
                            }
                        },
                        onGotKilled: (_, killer, state) =>
                        {
                            var results = BLTMissionBehavior.ApplyKilledEffects(
                                hero, killer, state,
                                settings.XPPerKilled,
                                actualBoost,
                                settings.RelativeLevelScaling,
                                settings.LevelScalingCap
                            );
                            
                            if (results.Any())
                            {
                                ActionManager.SendReply(context, results.ToArray());
                            }
                        }
                    );
                }
            }
        }

        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentBehavior), "EndCurrentMatch")]
        public static void EndCurrentMatchPrefix(TournamentBehavior __instance)
        {
            // If the tournament is over
            if (__instance.CurrentRoundIndex == 4 || __instance.LastMatch == null)
                return;

            // End round effects (as there is no event handler for it :/)
            foreach (var (context, settings, adoptedHero) in currentTournament)
            {
                float actualBoost = context.IsSubscriber ? settings.SubBoost : 1;
                
                var results = new List<string>();

                if(__instance.LastMatch.Winners.Any(w => w.Character?.HeroObject == adoptedHero))
                {
                    int actualGold = (int) (settings.WinMatchGold * actualBoost);
                    if (actualGold > 0)
                    {
                        BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(adoptedHero, actualGold);
                        results.Add($"+{actualGold} gold");
                    }
                    int xp = (int) (settings.WinMatchXP * actualBoost);
                    if (xp > 0)
                    {
                        (bool success, string description) =
                            SkillXP.ImproveSkill(adoptedHero, xp, Skills.All, random: false, auto: true);
                        if (success)
                        {
                            results.Add(description);
                        }
                    }
                }
                else if (__instance.LastMatch.Participants.Any(w => w.Character?.HeroObject == adoptedHero))
                {
                    int xp = (int) (settings.ParticipateMatchXP * actualBoost);
                    if (xp > 0)
                    {
                        (bool success, string description) =
                            SkillXP.ImproveSkill(adoptedHero, xp, Skills.All, random: false, auto: true);
                        if (success)
                        {
                            results.Add(description);
                        }
                    }
                }
                if (results.Any())
                {
                    ActionManager.SendReply(context, results.ToArray());
                }
            }
        }

        #if false
        private static float Odds(Hero forHero)
        {
            List<KeyValuePair<Hero, int>> leaderboard = Campaign.Current.TournamentManager.GetLeaderboard();
			int forHeroRank = 0;
			int maxHeroRank = 0;
			for (int i = 0; i < leaderboard.Count; i++)
			{
				if (leaderboard[i].Key == forHero)
				{
					forHeroRank = leaderboard[i].Value;
				}
				if (leaderboard[i].Value > maxHeroRank)
				{
					maxHeroRank = leaderboard[i].Value;
				}
			}
			float num3 = 30f + (float)forHero.Level + (float)Math.Max(0, forHeroRank * 12 - maxHeroRank * 2);
			float num4 = 0f;
			float num5 = 0f;
			float num6 = 0f;
			foreach (TournamentMatch tournamentMatch in this.CurrentRound.Matches)
			{
				foreach (TournamentTeam tournamentTeam in tournamentMatch.Teams)
				{
					float num7 = 0f;
					foreach (TournamentParticipant tournamentParticipant in tournamentTeam.Participants)
					{
						if (tournamentParticipant.Character != CharacterObject.PlayerCharacter)
						{
							int num8 = 0;
							if (tournamentParticipant.Character.IsHero)
							{
								for (int k = 0; k < leaderboard.Count; k++)
								{
									if (leaderboard[k].Key == tournamentParticipant.Character.HeroObject)
									{
										num8 = leaderboard[k].Value;
									}
								}
							}
							num7 += (float)(tournamentParticipant.Character.Level + Math.Max(0, num8 * 8 - maxHeroRank * 2));
						}
					}
					if (tournamentTeam.Participants.Any((TournamentParticipant x) => x.Character == CharacterObject.PlayerCharacter))
					{
						num5 = num7;
						foreach (TournamentTeam tournamentTeam2 in tournamentMatch.Teams)
						{
							if (tournamentTeam != tournamentTeam2)
							{
								foreach (TournamentParticipant tournamentParticipant2 in tournamentTeam2.Participants)
								{
									int num9 = 0;
									if (tournamentParticipant2.Character.IsHero)
									{
										for (int l = 0; l < leaderboard.Count; l++)
										{
											if (leaderboard[l].Key == tournamentParticipant2.Character.HeroObject)
											{
												num9 = leaderboard[l].Value;
											}
										}
									}
									num6 += (float)(tournamentParticipant2.Character.Level + Math.Max(0, num9 * 8 - maxHeroRank * 2));
								}
							}
						}
					}
					num4 += num7;
				}
			}
			float num10 = (num5 + num3) / (num6 + num5 + num3);
			float num11 = num3 / (num5 + num3 + 0.5f * (num4 - (num5 + num6)));
			float num12 = num10 * num11;
			float num13 = MathF.Clamp((float)Math.Pow((double)(1f / num12), 0.75), 1.1f, 4f);
			return (float)((int)(num13 * 10f)) / 10f;
        }
        public static void PlaceBet(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var adoptedHero = AdoptAHero.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            if (context.Args == null || !string.IsNullOrEmpty(context.Args?.Trim()))
            {
                onFailure("Arguments are missing: (gold) (name)");
                return;
            }
            string[] parts = context.Args.Trim().Split(' ');
            if (!int.TryParse(parts[0], out int betAmount))
            {
                onFailure("Arguments are incorrect: (gold) (name)");
                return;
            }

            string name = string.Join(" ", parts.Skip(1));
            var targetHero = Hero.All.FirstOrDefault(h 
                => h.FirstName.ToLower().Contains(name.ToLower()) 
                   && string.Equals(h.FirstName.ToString(), name, StringComparison.CurrentCultureIgnoreCase));
            if (targetHero == null)
            {
                onFailure($"Couldn't find any hero called {name}");
                return;
            }
            
            placedBets.Add((adoptedHero, targetHero, betAmount));
            //onSuccess.
        }
        #endif
    }

    #if false
    internal class BetOnTournamentMatch : ActionHandlerBase
    {
        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            JoinTournament.PlaceBet(context, config, onSuccess, onFailure);
        }
    }
    #endif
}
