using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Models;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Achievements;
using BLTAdoptAHero.Actions.Util;
using BLTAdoptAHero.Util;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox;
using SandBox.TournamentMissions.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;

namespace BLTAdoptAHero
{
    [HarmonyPatch]
    public class BLTTournamentMissionBehavior : AutoMissionBehavior<BLTTournamentMissionBehavior>
    {
        private readonly List<BLTTournamentQueueBehavior.TournamentQueueEntry> activeTournament = new();

        private bool isPlayerParticipating;

        public BLTTournamentMissionBehavior(bool isPlayerParticipating, TournamentGame tournamentGame)
        {
            this.isPlayerParticipating = isPlayerParticipating;
            SetPlaceholderPrize(tournamentGame);
        }

        public List<CharacterObject> GetParticipants()
        {
            var tournamentQueue = BLTTournamentQueueBehavior.Current.TournamentQueue;
                
            var participants = new List<CharacterObject>();
            if(isPlayerParticipating)
                participants.Add(Hero.MainHero.CharacterObject);
            
            int viewersToAddCount = Math.Min(16 - participants.Count, tournamentQueue.Count);
                
            var viewersToAdd = tournamentQueue.Take(viewersToAddCount).ToList();
            participants.AddRange(viewersToAdd.Select(q => q.Hero.CharacterObject));
            activeTournament.AddRange(viewersToAdd);
            tournamentQueue.RemoveRange(0, viewersToAddCount);
            
            var basicTroops = CampaignHelpers.AllCultures
                .SelectMany(c => new[] {c.BasicTroop, c.EliteBasicTroop})
                .Where(t => t != null)
                .ToList();

            while (participants.Count < 16)
            {
                participants.Add(basicTroops.SelectRandom());
            }
            
            TournamentHub.UpdateEntrants();

            foreach (var hero in viewersToAdd.Select(v => v.Hero))
            {
                int tournamentWins = BLTAdoptAHeroCampaignBehavior.Current.GetAchievementTotalStat(hero,
                    AchievementStatsData.Statistic.TotalTournamentFinalWins);
                if (tournamentWins > 0)
                {
                    var debuffs = BLTAdoptAHeroModule.TournamentConfig.PreviousWinnerDebuffs
                        .Select(d => d.ToModifier(tournamentWins)).ToList();
                    if (debuffs.Any())
                    {
                        BLTAgentStatCalculateModel.Current.AddModifiers(hero, debuffs);
                    }
                }
            }

            return participants;
        }
        
        private static int GetModifiedSkill(Hero hero, SkillObject skill, int baseModifiedSkill)
        {
            if (baseModifiedSkill == 0) return 0;
            
            var debuff = BLTAdoptAHeroModule.TournamentConfig.PreviousWinnerDebuffs
                .FirstOrDefault(d => SkillGroup.GetSkills(d.Skill).Contains(skill));
            if (debuff != null)
            {
                int tournamentWins = BLTAdoptAHeroCampaignBehavior.Current.GetAchievementTotalStat(hero,
                    AchievementStatsData.Statistic.TotalTournamentFinalWins);
                if (tournamentWins > 0)
                {
                    return (int) (baseModifiedSkill * debuff.SkillModifier(tournamentWins));
                }
            }

            return baseModifiedSkill;
        }
            
        private static IEnumerable<(Equipment equipment, IEnumerable<EquipmentType> types)> GetAllTournamentEquipment()
        {
            return CampaignHelpers.AllCultures.SelectMany(c => 
                    (c.TournamentTeamTemplatesForOneParticipant ?? Enumerable.Empty<CharacterObject>())
                        .Concat(c.TournamentTeamTemplatesForTwoParticipant ?? Enumerable.Empty<CharacterObject>())
                        .Concat(c.TournamentTeamTemplatesForFourParticipant ?? Enumerable.Empty<CharacterObject>()))
                .SelectMany(c => c.BattleEquipments ?? Enumerable.Empty<Equipment>())
                .Where(e => e != null)
                .Select(c => (
                    equipment: c,
                    types: c.YieldFilledWeaponSlots()
                        .Select(w => w.element.Item.GetEquipmentType())
                        .Where(e => e != EquipmentType.None)
                ));
        }

        private void GetTeamWeaponEquipmentListPostfixImpl(List<Equipment> equipments)
        {
            if (BLTAdoptAHeroModule.TournamentConfig.NoHorses)
            {
                foreach (var e in equipments)
                {
                    e[EquipmentIndex.Horse] = EquipmentElement.Invalid;
                    e[EquipmentIndex.HorseHarness] = EquipmentElement.Invalid;
                }
            }

            if (BLTAdoptAHeroModule.TournamentConfig.RandomizeWeaponTypes)
            {
                // Basic intention of this bit of code:
                // Each equipment set has a set of skills associated with it.
                // Each participant has a set of skills associated with their class.
                // Randomly select tournament equipment set weighted by how well it matches the participants skills.
                    
                var tournamentBehavior = MissionState.Current.CurrentMission.GetMissionBehaviour<TournamentBehavior>();
                    
                // Get all equipment sets, and their associated skills
                var availableEquipment = GetAllTournamentEquipment()
                    .Select(e => (
                        e.equipment,
                        skills: SkillGroup.GetSkills(SkillGroup.GetSkillsForEquipmentType(e.types).Distinct().ToList())))
                    // Exclude spears (defined as non-swingable polearms) if the config mandates it
                    .Where(e => !BLTAdoptAHeroModule.TournamentConfig.NoSpears 
                                || e.equipment.YieldWeaponSlots()
                                    .All(s => s.element.Item?.Type != ItemObject.ItemTypeEnum.Polearm 
                                              || s.element.Item.IsSwingable()))
                    .ToList();

                // Get the skill sets of the participating adopted heroes, by class
                var participantSkills = tournamentBehavior.CurrentMatch.Participants
                    // Get all the participating adopted heroes only
                    .Select(p => p.Character.HeroObject).Where(h => h?.IsAdopted() == true)
                    // Get the heroes associated class equipment
                    .Select(h => h.GetClass()?.WeaponSkills.ToList()).Where(s => s != null)
                    .ToList();

                // Select for each participant a random equipment set that closely matches theirs, then randomly select
                // from between those sets
                var tournamentSet = participantSkills
                    .Select(p => (
                        equipment: availableEquipment
                            .Shuffle()
                            // Ordering based on number of matching skills between the two sets, then by mismatching skills (quite rough...)
                            .OrderByDescending(e => e.skills.Intersect(p).Count() * 20 - e.skills.Except(p).Count())
                            .FirstOrDefault().equipment,
                        weight: 7f / participantSkills.Count)
                    )
                    // Add 2 random sets for some variety
                    .Concat(availableEquipment.Shuffle().Take(2).Select(e => (equipment: e.equipment, weight: 1f)))
                    // Add an unarmed set for some fun
                    .Concat((equipment: new Equipment(), weight: 0.5f).Yield())
                    // Select a random one
                    .SelectRandomWeighted(e => e.weight)
                    .equipment;

                MissionState.Current.CurrentMission
                    .GetMissionBehaviour<BLTTournamentSkillAdjustBehavior>()
                    .UnarmedRound = tournamentSet.IsEmpty();

                foreach (var e in equipments)
                {
                    foreach (var (_, index) in e.YieldWeaponSlots())
                    {
                        e[index] = tournamentSet[index];
                    }
                }
            }
            else if (BLTAdoptAHeroModule.TournamentConfig.NoSpears)
            {
                var replacementWeapon = CampaignHelpers.AllItems
                    .FirstOrDefault(i => i.StringId == "empire_sword_1_t2_blunt");
                if (replacementWeapon != null)
                {
                    foreach (var e in equipments)
                    {
                        foreach (var (element, index) in e.YieldWeaponSlots())
                        {
                            if (element.Item?.Type == ItemObject.ItemTypeEnum.Polearm && !element.Item.IsSwingable())
                            {
                                e[index] = new(replacementWeapon);
                            }
                        }
                    }
                }
            }
        }


        private bool AddRandomClothesPrefixImpl(CultureObject culture, TournamentParticipant participant)
        {
            if (BLTAdoptAHeroModule.TournamentConfig.NormalizeArmor)
            {
                var tier = (ItemObject.ItemTiers)Math.Max(0, Math.Min(5, BLTAdoptAHeroModule.TournamentConfig.NormalizeArmorTier - 1));
                var replacements = SkillGroup.ArmorIndexType
                    .Select(slotItemTypePair =>
                    (
                        slot: slotItemTypePair.slot, 
                        item: EquipHero.SelectRandomItemNearestTier(
                                  CampaignHelpers.AllItems.Where(i 
                                      => i.Culture == culture && i.ItemType == slotItemTypePair.itemType), (int)tier)
                              ?? EquipHero.SelectRandomItemNearestTier(CampaignHelpers.AllItems.Where(i => i.ItemType == slotItemTypePair.itemType), (int)tier)
                    )).ToList();
                    
                foreach (var (slot, item) in replacements)
                {
                    participant.MatchEquipment[slot] = new(item);
                }

                return true;
            }

            return false;
        }

        public void PrepareForTournamentGame()
        {
            MissionState.Current.CurrentMission.GetMissionBehaviour<TournamentBehavior>()
                .TournamentEnd += OnTournamentEnd;
        }

        private void OnTournamentEnd()
        {
            var tournamentBehavior = MissionState.Current.CurrentMission.GetMissionBehaviour<TournamentBehavior>();

            // Win results, put winner last
            foreach (var entry in activeTournament.OrderBy(e => e.Hero == tournamentBehavior.Winner.Character?.HeroObject))
            {
                float actualBoost = entry.IsSub ? Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1) : 1;
                var results = new List<string>();
                if (entry.Hero != null && entry.Hero == tournamentBehavior.Winner.Character?.HeroObject)
                {
                    results.Add("{=jb5vaUCD}WINNER!".Translate());

                    BLTAdoptAHeroCampaignBehavior.Current.IncreaseTournamentChampionships(entry.Hero);
                    // Winner gets their gold back also
                    int actualGold = (int) (BLTAdoptAHeroModule.TournamentConfig.WinGold * actualBoost + entry.EntryFee);
                    if (actualGold > 0)
                    {
                        BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(entry.Hero, actualGold);
                        results.Add($"{Naming.Inc}{actualGold}{Naming.Gold}");
                    }

                    int xp = (int) (BLTAdoptAHeroModule.TournamentConfig.WinXP * actualBoost);
                    if (xp > 0)
                    {
                        (bool success, string description) = SkillXP.ImproveSkill(entry.Hero, xp, SkillsEnum.All, auto: true);
                        if (success)
                        {
                            results.Add(description);
                        }
                    }

                    var (item, itemModifier, slot) = BLTAdoptAHeroModule.TournamentConfig.Prize.Generate(entry.Hero);

                    results.Add(item == null
                        ? "{=80PitGR4}no prize available for you!".Translate()
                        : RewardHelpers.AssignCustomReward(entry.Hero, item, itemModifier, slot));
                }
                else
                {
                    int xp = (int) (BLTAdoptAHeroModule.TournamentConfig.ParticipateXP * actualBoost);
                    if (xp > 0)
                    {
                        (bool success, string description) = SkillXP.ImproveSkill(entry.Hero, xp, SkillsEnum.All, auto: true);
                        if (success)
                        {
                            results.Add(description);
                        }
                    }
                }

                if (results.Any() && entry.Hero != null)
                {
                    Log.LogFeedResponse(entry.Hero.FirstName.ToString(), results.ToArray());
                }
            }

            activeTournament.Clear();
        }

        private void EndCurrentMatchPrefixImpl(TournamentBehavior tournamentBehavior)
        {
            // If the tournament is over we need to make sure player gets the real prize. 
            // Need to do this before EndCurrentMatch, as the player gets the prize in this function.
            if (tournamentBehavior.CurrentRoundIndex == 3)
            {
                // Reset the prize if the player won
                if (originalPrize != null
                    && tournamentBehavior.CurrentMatch.IsPlayerWinner())
                {
                    SetPrize(tournamentBehavior.TournamentGame, originalPrize);
                }
            }
        }

        private void EndCurrentMatchPostfixImpl(TournamentBehavior tournamentBehavior)
        {
            BLTTournamentBetMissionBehavior.Current?.CompleteBetting(tournamentBehavior.LastMatch);

            if(tournamentBehavior.CurrentMatch != null)
            {
                BLTTournamentBetMissionBehavior.Current?.OpenBetting(tournamentBehavior);
            }
                
            int lastRoundIndex = tournamentBehavior.CurrentMatch == null ? 3 : tournamentBehavior.CurrentRoundIndex - 1;
            var rewards = BLTAdoptAHeroModule.TournamentConfig.RoundRewards[
                // Better safe than sorry, maybe some mod will add more rounds
                Math.Max(0, Math.Min(lastRoundIndex, BLTAdoptAHeroModule.TournamentConfig.RoundRewards.Length - 1))
            ];
                
            // End round effects (as there is no event handler for it :/)
            foreach (var entry in activeTournament)
            {
                float actualBoost = entry.IsSub ? Math.Max(BLTAdoptAHeroModule.CommonConfig.SubBoost, 1) : 1;
                        
                var results = new List<string>();
                if(tournamentBehavior.LastMatch.Winners.Any(w => w.Character?.HeroObject == entry.Hero))
                {
                    int actualGold = (int) (rewards.WinGold * actualBoost);
                    if (actualGold > 0)
                    {
                        BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(entry.Hero, actualGold);
                        results.Add($"{Naming.Inc}{actualGold}{Naming.Gold}");
                    }
                    int xp = (int) (rewards.WinXP * actualBoost);
                    if (xp > 0)
                    {
                        (bool success, string description) =
                            SkillXP.ImproveSkill(entry.Hero, xp, SkillsEnum.All, auto: true);
                        if (success)
                        {
                            results.Add(description);
                        }
                    }
                    BLTAdoptAHeroCampaignBehavior.Current.IncreaseTournamentRoundWins(entry.Hero);
                }
                else if (tournamentBehavior.LastMatch.Participants.Any(w => w.Character?.HeroObject == entry.Hero))
                {
                    int xp = (int) (rewards.LoseXP * actualBoost);
                    if (xp > 0)
                    {
                        (bool success, string description) =
                            SkillXP.ImproveSkill(entry.Hero, xp, SkillsEnum.All, auto: true);
                        if (success)
                        {
                            results.Add(description);
                        }
                    }
                    BLTAdoptAHeroCampaignBehavior.Current.IncreaseTournamentRoundLosses(entry.Hero);
                }
                if (results.Any())
                {
                    Log.LogFeedResponse(entry.Hero.FirstName.ToString(), results.ToArray());
                }
            }
        }

        private ItemObject originalPrize;
            
        private void SetPlaceholderPrize(TournamentGame tournamentGame)
        {
            originalPrize = tournamentGame.Prize;
            SetPrize(tournamentGame, DefaultItems.Charcoal);
        }

        private static void SetPrize(TournamentGame tournamentGame, ItemObject prize)
        {
            AccessTools.Property(typeof(TournamentGame), nameof(TournamentGame.Prize))
                .SetValue(tournamentGame, prize);
        }
        
        #region Patches

        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentFightMissionController), "GetTeamWeaponEquipmentList")]
        public static void GetTeamWeaponEquipmentListPostfix(List<Equipment> __result)
        {
            SafeCallStatic(() => Current?.GetTeamWeaponEquipmentListPostfixImpl(__result));
        }
        
        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(TournamentFightMissionController),
             "AddRandomClothes")]
        public static bool AddRandomClothesPrefix(CultureObject culture, TournamentParticipant participant)
        {
            // Harmony Prefix should return false to skip the original function
            return SafeCallStatic(() => Current?.AddRandomClothesPrefixImpl(culture, participant) != true, true);
        }
        
        [UsedImplicitly, HarmonyPrefix, HarmonyPatch(typeof(TournamentBehavior), "EndCurrentMatch")]
        public static void EndCurrentMatchPrefix(TournamentBehavior __instance)
        {
            SafeCallStatic(() => Current?.EndCurrentMatchPrefixImpl(__instance));
        }
        
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(TournamentBehavior), "EndCurrentMatch")]
        public static void EndCurrentMatchPostfix(TournamentBehavior __instance)
        {
            SafeCallStatic(() => Current?.EndCurrentMatchPostfixImpl(__instance));
        }
        
        #endregion
        
                
#if DEBUG
        [CommandLineFunctionality.CommandLineArgumentFunction("testprize", "blt")]
        [UsedImplicitly]
        public static string TestTournamentCustomReward(List<string> strings)
        {
            if (strings.Count == 1)
            {
                int count = int.Parse(strings[0]);
                for (int i = 0; i < count; i++)
                {
                    var (item, modifier, _) = BLTAdoptAHeroModule.TournamentConfig.Prize.Generate(Hero.MainHero);
                    if (item == null)
                    {
                        return $"Couldn't generate a matching item";
                    }
                    var equipment = new EquipmentElement(item, modifier);
                    Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(equipment, 1);
                }
                return $"Added {count} items to {Hero.MainHero.Name}";
            }
            else if (strings.Count == 3)
            {
                int count = int.Parse(strings[2]);
                var rewardType = (RewardHelpers.RewardType) Enum.Parse(typeof(RewardHelpers.RewardType), strings[0]);
                var classDef = BLTAdoptAHeroModule.HeroClassConfig.FindClass(strings[1]);

                for (int i = 0; i < count; i++)
                {
                    var (item, modifier, _) = RewardHelpers.GenerateRewardType(rewardType, 6, 
                        Hero.MainHero, classDef, allowDuplicates: true,
                        BLTAdoptAHeroModule.CommonConfig.CustomRewardModifiers,
                        BLTAdoptAHeroModule.TournamentConfig.Prize.CustomItemName.ToString(), 
                        BLTAdoptAHeroModule.TournamentConfig.Prize.CustomItemPower);
                    
                    if (item == null)
                    {
                        return $"Couldn't generate a matching item";
                    }

                    var equipment = new EquipmentElement(item, modifier);
                    
                    Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(equipment, 1);
                }

                return $"Added {count} items to {Hero.MainHero.Name}";
            }
            else
            {
                return "Expected 1 or 3 arguments: blt.testprize <number to make> OR blt.testprize Weapon/Armor/Mount <class name> <number to make>";
            }
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("testprize2", "blt")]
        [UsedImplicitly]
        public static string TestTournamentCustomReward2(List<string> strings)
        {
            foreach (var h in BLTAdoptAHeroCampaignBehavior.GetAllAdoptedHeroes())
            {
                var (item, itemModifier, slot) = BLTAdoptAHeroModule.TournamentConfig.Prize.Generate(h);
                if (item != null)
                {
                    var element = new EquipmentElement(item, itemModifier);
                    BLTAdoptAHeroCampaignBehavior.Current.AddCustomItem(h, element);
                    if (slot != EquipmentIndex.None)
                    {
                        h.BattleEquipment[slot] = element;
                    }
                }
                else
                {
                    Log.Error($"Failed to generate reward for {h.Name}");
                }
            }
                
            GameStateManager.Current?.UpdateInventoryUI();

            return "done";
        }
#endif
    }
}