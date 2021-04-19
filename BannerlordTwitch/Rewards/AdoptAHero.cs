using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using HarmonyLib;
using Helpers;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using SandBox;
using SandBox.Source.Missions;
using SandBox.View.Missions;
using StoryMode.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

#pragma warning disable 649

namespace BannerlordTwitch.Rewards
{
    [UsedImplicitly]
    internal class AdoptAHero : IRedemptionAction
    {
        public static readonly (CharacterAttributesEnum val, string shortName)[] CharAttributes = {
            (CharacterAttributesEnum.Vigor, "Vig"),
            (CharacterAttributesEnum.Control, "Con"),
            (CharacterAttributesEnum.Endurance, "End"),
            (CharacterAttributesEnum.Cunning, "Cun"),
            (CharacterAttributesEnum.Social, "Soc"),
            (CharacterAttributesEnum.Intelligence, "Int"),
        };

        internal const string NoHeroMessage = "Couldn't find your hero, did you adopt one yet?";
        internal const string NotStartedMessage = "The game isn't started yet";
        internal const string Tag = "[BLT]";
        
        // public class GlobalSettings
        // {
        // }
        //
        // private static GlobalSettings gSettings;

        // public static GlobalSettings GetGlobalSettings()
        // {
        //     if (gSettings == null)
        //     {
        //         var config = RewardManager.FindGlobalConfig("AdoptAHero"); // ?? 
        //         if (config == null)
        //         {
        //             Log.Trace($"{nameof(AdoptAHero)} - No global config found. Setting defaults.");
        //         }
        //         else
        //         {
        //             try
        //             {
        //                 gSettings = config.ToObject<GlobalSettings>();
        //             }
        //             catch (Exception ex)
        //             {
        //                 Log.ScreenCritical(
        //                     $"{nameof(AdoptAHero)} - global settings object couldn't be parsed ({ex.Message}), check your config file. Setting defaults.");
        //             }
        //         }
        //     }
        //     gSettings ??= new GlobalSettings {};
        //     return gSettings;
        // }

        public struct Settings
        {
            public bool IsNoble;
            public bool IsPlayerCompanion;
            public bool IsWanderer;
            public bool OnlySameFaction;
            public bool AllowNewAdoptionOnDeath;
        }

        private static IEnumerable<Hero> GetAvailableHeroes(Settings settings)
        {
            var tagText = new TextObject(Tag);
            return Campaign.Current?.AliveHeroes?.Where(h =>
                // Don't want notables ever
                !h.IsNotable &&
                (settings.IsPlayerCompanion && h.IsPlayerCompanion
                 || settings.IsNoble && h.IsNoble
                 || settings.IsWanderer && h.IsWanderer)
                && (!settings.OnlySameFaction || Clan.PlayerClan?.MapFaction != null &&
                    Clan.PlayerClan?.MapFaction == h.Clan?.MapFaction)
            ).Where(n => !n.Name.Contains(tagText));
        }

        internal static string GetFullName(string name) => $"{name} {AdoptAHero.Tag}";

        internal static Hero GetAdoptedHero(string name)
        {
            var tagObject = new TextObject(AdoptAHero.Tag);
            var nameObject = new TextObject(name);
            return Campaign.Current?.AliveHeroes?
                .FirstOrDefault(h => (h.Name?.Contains(tagObject) ?? false) && (h.FirstName?.Contains(nameObject) ?? false));
        }

        public void Enqueue(Guid redemptionId, string _, string userName, JObject config)
        {
            if (Campaign.Current == null)
            {
                RewardManager.NotifyCancelled(redemptionId, AdoptAHero.NotStartedMessage);
                return;
            }
            
            var settings = config.ToObject<Settings>();
            var hero = GetAdoptedHero(userName);
            if (hero?.IsAlive == true)
            {
                RewardManager.NotifyCancelled(redemptionId, $"You have already adopted a hero!");
                return;
            }
            if (hero?.IsAlive == false && !settings.AllowNewAdoptionOnDeath)
            {
                RewardManager.NotifyCancelled(redemptionId, $"Your hero died, and you may not adopt another!");
                return;
            }
            
            var randomHero = GetAvailableHeroes(settings).SelectRandom();
            if (randomHero == null)
            {
                RewardManager.NotifyCancelled(redemptionId, $"You can't adopt a hero: no available hero matching the requirements was found!");
                return;
            }
            
            var oldName = randomHero.Name.ToString();
            randomHero.FirstName = new TextObject(userName);
            randomHero.Name = new TextObject(GetFullName(userName));
            RewardManager.NotifyComplete(redemptionId, $"You have adopted {oldName}!");
        }
    }

    [UsedImplicitly]
    internal class HeroCommand : IBotCommand
    {
        public struct Settings
        {
            public bool ShowGeneral;
            public int MinSkillToShow;
            public bool ShowTopSkills;
            public bool ShowAttributes;
            public bool ShowEquipment;
        }
        
        public void Execute(string args, string userName, string replyId, JObject config)
        {
            var settings = config.ToObject<Settings>();
            var usersHero = AdoptAHero.GetAdoptedHero(userName);
            var infoStrings = new List<string>{};
            if (usersHero == null)
            {
                infoStrings.Add(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
            }
            else
            {
                if (settings.ShowGeneral)
                {
                    infoStrings.Add($"{usersHero.HitPoints} / {usersHero.CharacterObject.MaxHitPoints()} hp");
                    infoStrings.Add($"{usersHero.Gold} gold");
                    infoStrings.Add(StringHelpers.GetLastKnownLocation(usersHero).ToString());
                }
                if (settings.ShowTopSkills)
                {
                    infoStrings.Add($"Level {usersHero.Level}");
                    infoStrings.Add("Skills: " + string.Join(" | ", 
                        SkillObject.All
                        .Where(s => usersHero.GetSkillValue(s) >= settings.MinSkillToShow)
                        .OrderByDescending(s => usersHero.GetSkillValue(s))
                        .Select(skill => $"{skill.Name}: {usersHero.GetSkillValue(skill)} " +
                                         $"({usersHero.HeroDeveloper.GetFocus(skill)} focus)")
                        ));
                }
                if (settings.ShowAttributes)
                {
                    infoStrings.Add("Attr: " + string.Join(" | ", AdoptAHero.CharAttributes
                            .Select(a => $"{a.shortName} {usersHero.GetAttributeValue(a.val)}")));
                }
                if (settings.ShowEquipment)
                {
                    infoStrings.Add("Battle: " + string.Join(" | ", usersHero.BattleEquipment
                        .YieldEquipmentSlots()
                        .Where(e => !e.element.IsEmpty)
                        .Select(e => $"{e.element.Item.Name}")
                    ));
                    infoStrings.Add("Civilian: " + string.Join(" | ", usersHero.CivilianEquipment
                        .YieldEquipmentSlots()
                        .Where(e => !e.element.IsEmpty)
                        .Select(e => $"{e.element.Item.Name}")
                    ));
                }
            }

            RewardManager.SendReply(replyId, infoStrings.ToArray());
        }
    }

    internal abstract class ImproveAdoptedHero : IRedemptionAction
    {
        public struct Settings
        {
            public string Improvement;
            public int AmountLow;
            public int AmountHigh;
            public bool Random;
        }
        
        public void Enqueue(Guid redemptionId, string _, string userName, JObject config)
        {
            var settings = config.ToObject<Settings>();
            var usersHero = AdoptAHero.GetAdoptedHero(userName);
            if (usersHero == null)
            {
                RewardManager.NotifyCancelled(redemptionId, Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            int amount = MBRandom.RandomInt(settings.AmountLow, settings.AmountHigh);
            (bool success, string description) = Improve(redemptionId, userName, usersHero, amount, settings);
            if (success)
            {
                RewardManager.NotifyComplete(redemptionId, description);
            }
            else
            {
                RewardManager.NotifyCancelled(redemptionId, description);
            }
        }
        protected abstract (bool success, string description) Improve(Guid redemptionId, string userName,
            Hero usersHero, int amount, Settings settings);

        // One Handed, Two Handed, Polearm, Bow, Crossbow, Throwing, Riding, Athletics, Smithing
        // Scouting, Tactics, Roguery, Charm, Leadership, Trade, Steward, Medicine, Engineering
        protected static SkillObject GetSkill(Hero hero, string improvement, bool random, Func<SkillObject, bool> predicate = null)
        {
            var skills = improvement.Split(',').Select(s => s.Trim());
            var selectedSkills = DefaultSkills.GetAllSkills()
                .Where(so => skills.Any(sn => string.Equals(so.Name.ToString(), sn, StringComparison.CurrentCultureIgnoreCase)))
                .Where(predicate ?? (s => true))
                .ToList();
            return random 
                ? selectedSkills.SelectRandom() 
                : selectedSkills.OrderByDescending(hero.GetSkillValue).FirstOrDefault();
        }
    }
    
    [UsedImplicitly]
    internal class SkillXP : ImproveAdoptedHero
    {
        protected override (bool success, string description) Improve(Guid redemptionId, string userName,
            Hero usersHero, int amount, Settings settings)
        {
            var skill = GetSkill(usersHero, settings.Improvement, settings.Random);
            if (skill == null) return (false, $"Couldn't improve skill {settings.Improvement}: its not a valid skill name!");
            usersHero.HeroDeveloper.AddSkillXp(skill, amount);
            return (true, $"You have gained {amount}xp in {skill.Name}, you now have {usersHero.HeroDeveloper.GetPropertyValue(skill)}!");
        }
    }

    [UsedImplicitly]
    internal class AttributePoints : ImproveAdoptedHero
    {

        // Vigor, Control, Endurance, Cunning, Social, Intelligence
        protected override (bool success, string description) Improve(Guid redemptionId, string userName,
            Hero usersHero, int amount, Settings settings)
        {
            // Get attributes that can be buffed
            var improvableAttributes = AdoptAHero.CharAttributes
                .Select(c => c.val)
                .Where(a => usersHero.GetAttributeValue(a) < 10)
                .ToList();

            if (!improvableAttributes.Any())
            {
                return (false, $"Couldn't improve any attributes, they are all at max level!");
            }
            CharacterAttributesEnum attribute;
            if (settings.Random)
            {
                attribute = improvableAttributes.SelectRandom();
            }
            else
            {
                var matching = improvableAttributes
                    .Where(a => string.Equals(a.ToString(), settings.Improvement, StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
                if (!matching.Any())
                {
                    return (false, $"Couldn't improve attribute {settings.Improvement}, its not a valid attribute name!");
                }
                attribute = matching.First();
            }

            amount = Math.Min(amount, 10 - usersHero.GetAttributeValue(attribute));
            usersHero.HeroDeveloper.AddAttribute(attribute, amount, checkUnspentPoints: false);
            return (true, $"You have gained {amount} point{(amount > 1? "s" : "")} in {attribute}, you now have {usersHero.GetAttributeValue(attribute)}!");
        }
    }
    
    [UsedImplicitly]
    internal class FocusPoints : ImproveAdoptedHero
    {
        protected override (bool success, string description) Improve(Guid redemptionId, string userName,
            Hero usersHero, int amount, Settings settings)
        {
            var skill = GetSkill(usersHero, settings.Improvement, settings.Random,
                s => usersHero.HeroDeveloper.GetFocus(s) < 5);
            if (skill == null)
            {
                return (false, $"Couldn't find a valid skill to add focus points to!");
            }
            
            amount = Math.Min(amount, 5 - usersHero.HeroDeveloper.GetFocus(skill));
            usersHero.HeroDeveloper.AddFocus(skill, amount, checkUnspentFocusPoints: false);
            return (true, $"You have gained {amount} focus point{(amount > 1? "s" : "")} in {skill}, you now have {usersHero.HeroDeveloper.GetFocus(skill)}!");
        }
    }

    [UsedImplicitly]
    internal class SummonHero : IRedemptionAction
    {
        public struct Settings
        {
            public bool AllowFieldBattle;
            public bool AllowFriendlyMission;
            public bool AllowArena;
            public bool AllowHideOut;
            public bool OnPlayerSide;
        }

        private delegate Agent SpawnWanderingAgentDelegate(
            MissionAgentHandler instance,
            LocationCharacter locationCharacter,
            MatrixFrame spawnPointFrame,
            bool hasTorch,
            bool noHorses);

        private static readonly SpawnWanderingAgentDelegate SpawnWanderingAgent = (SpawnWanderingAgentDelegate)
            AccessTools.Method(typeof(MissionAgentHandler),
                    "SpawnWanderingAgent",
                    new[] {typeof(LocationCharacter), typeof(MatrixFrame), typeof(bool), typeof(bool)})
                .CreateDelegate(typeof(SpawnWanderingAgentDelegate));
        
        public void Enqueue(Guid redemptionId, string message, string userName, JObject config)
        {
            var settings = config.ToObject<Settings>();
            var usersHero = AdoptAHero.GetAdoptedHero(userName);
            if (usersHero == null)
            {
                RewardManager.NotifyCancelled(redemptionId, Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            if (usersHero.IsPlayerCompanion)
            {
                RewardManager.NotifyCancelled(redemptionId, $"You are a player companion, you cannot be summoned in this manner!");
                return;
            }
            if (Mission.Current == null 
                || Mission.Current.Mode is MissionMode.Barter or MissionMode.Conversation or MissionMode.Deployment or
                    MissionMode.Duel or MissionMode.Replay or MissionMode.CutScene)
            {
                RewardManager.NotifyCancelled(redemptionId, $"You cannot be summoned now!");
                return;
            }
            
            if(InArenaMission() && !settings.AllowArena
               || InFieldBattleMission() && !settings.AllowFieldBattle
               || Mission.Current.IsFriendlyMission && !settings.AllowFriendlyMission
               || InHideOutMission() && !settings.AllowHideOut
               || InTrainingFieldMission()
               || InArenaVisitingArea()
               )
            {
                RewardManager.NotifyCancelled(redemptionId, $"You cannot be summoned now, this mission does not allow it!");
                return;
            }
            if (!Mission.Current.IsLoadingFinished)
            {
                RewardManager.NotifyCancelled(redemptionId, $"You cannot be summoned now, the mission has not started yet!");
                return;
            }
            if (Mission.Current.IsMissionEnding || (Mission.Current.MissionResult?.BattleResolved ?? false))
            {
                RewardManager.NotifyCancelled(redemptionId, $"You cannot be summoned now, the mission is ending!");
                return;
            }
            
            if (HeroIsSpawned(usersHero))
            {
                RewardManager.NotifyCancelled(redemptionId, $"You cannot be summoned, you are already here!");
                return;
            }
            
            if (CampaignMission.Current.Location != null)
            {
                var locationCharacter = LocationCharacter.CreateBodyguardHero(usersHero,
                    MobileParty.MainParty,
                    SandBoxManager.Instance.AgentBehaviorManager.AddBodyguardBehaviors);
                
                var missionAgentHandler = Mission.Current.GetMissionBehaviour<MissionAgentHandler>();
                var worldFrame = missionAgentHandler.Mission.MainAgent.GetWorldFrame();
                worldFrame.Origin.SetVec2(worldFrame.Origin.AsVec2 + (worldFrame.Rotation.f * 1f + worldFrame.Rotation.s).AsVec2);
                
                CampaignMission.Current.Location.AddCharacter(locationCharacter);
                var agent = SpawnWanderingAgent(missionAgentHandler, locationCharacter, worldFrame.ToGroundMatrixFrame(), false, true); 

                agent.SetTeam(missionAgentHandler.Mission.PlayerTeam, true);

                if (agent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
                {
                    var behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
                    (behaviorGroup.GetBehavior<FollowAgentBehavior>() ?? behaviorGroup.AddBehavior<FollowAgentBehavior>()).SetTargetAgent(Agent.Main);
                    behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
                }
                
                missionAgentHandler.SimulateAgent(agent);
            }
            else
            {
                Mission.Current.SpawnTroop(
                    new PartyAgentOrigin(PartyBase.MainParty, usersHero.CharacterObject),
                    isPlayerSide: settings.OnPlayerSide,
                    hasFormation: true,
                    spawnWithHorse: usersHero.CharacterObject.HasMount() && Mission.Current.Mode != MissionMode.Stealth,
                    isReinforcement: true,
                    enforceSpawningOnInitialPoint: false,
                    formationTroopCount: 1,
                    formationTroopIndex: 8,
                    isAlarmed: true,
                    wieldInitialWeapons: true);
            }
            
            RewardManager.NotifyComplete(redemptionId, $"You have joined the battle!");
        }

        private static bool HeroIsSpawned(Hero hero)
        {
            return (CampaignMission.Current.Location?.ContainsCharacter(hero) ?? false)
                   || (Mission.Current?.Agents.Any(a => a.Character == hero.CharacterObject) ?? false);
        }

        private static bool InHideOutMission() => Mission.Current?.Mode == MissionMode.Stealth;
        private static bool InFieldBattleMission() => Mission.Current?.IsFieldBattle ?? false;
        private static bool InArenaMission() => CampaignMission.Current?.Location?.StringId == "arena" 
                                                      && Mission.Current?.Mode == MissionMode.Battle;
        private static bool InArenaVisitingArea() => CampaignMission.Current?.Location?.StringId == "arena" 
                                                && Mission.Current?.Mode != MissionMode.Battle;
        private static bool InConversation() => Mission.Current?.Mode == MissionMode.Conversation;
        private static bool InTrainingFieldMission() => Mission.Current?.GetMissionBehaviour<TrainingFieldMissionController>() != null;
        private static bool InVillageEncounter() => PlayerEncounter.LocationEncounter?.GetType() == typeof(VillageEncouter);
    }
    
    [UsedImplicitly]
    internal class EquipHero : IRedemptionAction
    {
        // These must be properties not fields, as these values are dynamic
        private static SkillObject[] MeleeSkills => new [] {
            DefaultSkills.OneHanded,
            DefaultSkills.TwoHanded,
            DefaultSkills.Polearm,
        };

        private static ItemObject.ItemTypeEnum[] MeleeItems => new [] {
            ItemObject.ItemTypeEnum.OneHandedWeapon,
            ItemObject.ItemTypeEnum.TwoHandedWeapon,
            ItemObject.ItemTypeEnum.Polearm,
        };

        private static SkillObject[] RangedSkills => new [] {
            DefaultSkills.Bow,
            DefaultSkills.Crossbow,
            DefaultSkills.Throwing,
        };
        
        private static ItemObject.ItemTypeEnum[] RangedItems => new [] {
            ItemObject.ItemTypeEnum.Bow,
            ItemObject.ItemTypeEnum.Crossbow,
            ItemObject.ItemTypeEnum.Thrown,
        };

        private static (EquipmentIndex, ItemObject.ItemTypeEnum)[] ArmorIndexType => new[] {
            (EquipmentIndex.Head, ItemObject.ItemTypeEnum.HeadArmor),
            (EquipmentIndex.Body, ItemObject.ItemTypeEnum.BodyArmor),
            (EquipmentIndex.Leg, ItemObject.ItemTypeEnum.LegArmor),
            (EquipmentIndex.Gloves, ItemObject.ItemTypeEnum.HandArmor),
            (EquipmentIndex.Cape, ItemObject.ItemTypeEnum.Cape),
        };

        public struct Settings
        {
            public bool Armor;
            public bool Melee;
            public bool Ranged;
            public bool Horse;
            public bool Civilian;
            public bool AllowCompanionUpgrade;
            public int Tier; // 0 to 5
        }

        public void Enqueue(Guid redemptionId, string message, string userName, JObject config)
        {
            var settings = config.ToObject<Settings>();
            var usersHero = AdoptAHero.GetAdoptedHero(userName);
            if (usersHero == null)
            {
                RewardManager.NotifyCancelled(redemptionId, Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            if (!settings.AllowCompanionUpgrade && usersHero.IsPlayerCompanion)
            {
                RewardManager.NotifyCancelled(redemptionId, $"You are a player companion, you cannot change your own equipment!");
                return;
            }
            if (Mission.Current != null)
            {
                RewardManager.NotifyCancelled(redemptionId, $"You cannot upgrade equipment, as a mission is active!");
                return;
            }

            var itemsPurchased = new List<ItemObject>();

            static bool CanUseItem(ItemObject item, Hero hero)
            {
                var relevantSkill = item.RelevantSkill;
                return (relevantSkill == null || hero.GetSkillValue(relevantSkill) >= item.Difficulty) 
                       && (!hero.IsFemale || !item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByFemale)) 
                       && (hero.IsFemale || !item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale));
            }

            static List<(EquipmentElement element, EquipmentIndex index)> GetMatchingItems(SkillObject skill, Equipment equipment, params ItemObject.ItemTypeEnum[] itemTypeEnums) =>
                equipment
                    .YieldWeaponSlots()
                    .Where(e => !e.element.IsEmpty)
                    .Where(e => itemTypeEnums.Contains(e.element.Item.Type))
                    .Where(e => e.element.Item.RelevantSkill == skill)
                    .ToList();

            static void RemoveNonBestSkillItems(IEnumerable<SkillObject> skills, SkillObject bestSkill, Equipment equipment, params ItemObject.ItemTypeEnum[] itemTypeEnums)
            {
                foreach (var x in equipment
                    .YieldWeaponSlots()
                    .Where(e => !e.element.IsEmpty)
                    // Correct type
                    .Where(e => itemTypeEnums.Contains(e.element.Item.Type))
                    .Where(e => skills.Contains(e.element.Item.RelevantSkill)
                                && e.element.Item.RelevantSkill != bestSkill)
                    .ToList())
                {
                    equipment[x.index] = EquipmentElement.Invalid;
                }
            }

            static void RemoveNonBestMatchingWeapons(SkillObject skillObject, Equipment equipment, params ItemObject.ItemTypeEnum[] itemTypeEnums)
            {
                foreach (var x in GetMatchingItems(skillObject, equipment, itemTypeEnums)
                    // Highest tier first
                    .OrderByDescending(e => e.element.Item.Tier)
                    .Skip(1)
                    .ToList())
                {
                    equipment[x.index] = EquipmentElement.Invalid;
                }
            }

            static List<(EquipmentElement element, EquipmentIndex index)> FindAllEmptyWeaponSlots(Equipment equipment)
            {
                return equipment
                    .YieldWeaponSlots()
                    .Where(e => e.element.IsEmpty)
                    .ToList();
            }
            
            static (EquipmentElement element, EquipmentIndex index) FindEmptyWeaponSlot(Equipment equipment)
            {
                var emptySlots = FindAllEmptyWeaponSlots(equipment);
                return emptySlots.Any() ? emptySlots.First() : (EquipmentElement.Invalid, EquipmentIndex.None);
            }

            static ItemObject FindRandomTieredEquipment(SkillObject skill, int tier, Hero hero, Func<ItemObject, bool> filter = null, params ItemObject.ItemTypeEnum[] itemTypeEnums)
            {
                var items = ItemObject.All
                    // Usable
                    .Where(item => !item.NotMerchandise && CanUseItem(item, hero) && (filter == null || filter(item)))
                    // Correct type
                    .Where(item => itemTypeEnums.Contains(item.Type))
                    // Correct skill
                    .Where(item => skill == null || item.RelevantSkill == skill)
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

            static ItemObject UpgradeWeapon(SkillObject skill, SkillObject[] skillGroup, ItemObject.ItemTypeEnum[] itemTypeEnums, EquipmentIndex defaultEquipmentIndex, Hero hero, Equipment equipment, int tier, Func<ItemObject, bool> filter = null)
            {
                // Remove all non-skill matching weapons
                RemoveNonBestSkillItems(skillGroup, skill, equipment, itemTypeEnums);

                // Remove all but the *best* matching weapon
                RemoveNonBestMatchingWeapons(skill, equipment, itemTypeEnums);

                // Get slot of correct skill weapon we can replace  
                var weaponSlots = GetMatchingItems(skill, equipment, itemTypeEnums);

                // If there isn't one then find an empty slot
                var (element, index) = !weaponSlots.Any()
                    ? FindEmptyWeaponSlot(equipment)
                    : weaponSlots.First();

                if (index == EquipmentIndex.None)
                {
                    // We will just replace the first weapon if we can't find any slot (shouldn't happen)
                    index = defaultEquipmentIndex;
                }

                if (element.Item == null || element.Item.Tier < (ItemObject.ItemTiers) tier)
                {
                    var newWeapon = FindRandomTieredEquipment(skill, tier, hero, filter, itemTypeEnums);
                    if (newWeapon != null)
                    {
                        equipment[index] = new EquipmentElement(newWeapon);
                        return newWeapon;
                    }
                }

                return element.Item;
            }
            
            static ItemObject UpgradeItemInSlot(EquipmentIndex equipmentIndex, ItemObject.ItemTypeEnum itemTypeEnum, int tier, Equipment equipment, Hero hero, Func<ItemObject, bool> filter = null)
            {
                var slot = equipment[equipmentIndex];
                if (slot.Item == null || slot.Item.Tier < (ItemObject.ItemTiers) tier)
                {
                    var item = FindRandomTieredEquipment(null, tier, hero, filter, itemTypeEnum);
                    if (item != null && (slot.Item == null || slot.Item.Tier < item.Tier))
                    {
                        equipment[equipmentIndex] = new EquipmentElement(item);
                        return item;
                    }
                }
                return null;
            }
            
            if (settings.Melee)
            {
                // We want to be left with only one melee weapon of the appropriate skill, of the highest tier, then we will 
                // try and upgrade it
                var highestSkill = MeleeSkills.OrderByDescending(s => usersHero.GetSkillValue(s)).First();

                var newWeapon = UpgradeWeapon(highestSkill, MeleeSkills, MeleeItems, EquipmentIndex.Weapon0, usersHero, usersHero.BattleEquipment, settings.Tier);
                if (newWeapon != null)
                {
                    itemsPurchased.Add(newWeapon);
                }

                var shieldSlots = usersHero.BattleEquipment
                    .YieldWeaponSlots()
                    .Where(e => e.element.Item?.Type == ItemObject.ItemTypeEnum.Shield)
                    .ToList();

                if (highestSkill == DefaultSkills.OneHanded)
                {
                    var (element, index) = !shieldSlots.Any() ? FindEmptyWeaponSlot(usersHero.BattleEquipment) : shieldSlots.First();
                    if (index == EquipmentIndex.None)
                        index = EquipmentIndex.Weapon1;

                    if (element.Item == null || element.Item.Tier < (ItemObject.ItemTiers) settings.Tier)
                    {
                        var shield = FindRandomTieredEquipment(DefaultSkills.OneHanded, settings.Tier, usersHero,
                            null, ItemObject.ItemTypeEnum.Shield);
                        if (shield != null)
                        {
                            usersHero.BattleEquipment[index] = new EquipmentElement(shield);
                            itemsPurchased.Add(shield);
                        }
                    }
                }
            }

            if (settings.Ranged)
            {
                // We want to be left with only one weapon of the appropriate skill, of the highest tier, then we will 
                // try and upgrade it
                var highestSkill = RangedSkills.OrderByDescending(s => usersHero.GetSkillValue(s)).First();
                
                var weapon = UpgradeWeapon(highestSkill, RangedSkills, RangedItems, EquipmentIndex.Weapon3, usersHero, usersHero.BattleEquipment, settings.Tier);

                if (weapon?.Type == ItemObject.ItemTypeEnum.Thrown)
                {
                    // add more to free slots
                    var (_, index) = FindEmptyWeaponSlot(usersHero.BattleEquipment);
                    if (index != EquipmentIndex.None)
                    {
                        usersHero.BattleEquipment[index] = new EquipmentElement(weapon);
                    }
                }
                else if(weapon?.Type is ItemObject.ItemTypeEnum.Bow or ItemObject.ItemTypeEnum.Crossbow)
                {
                    var ammoType = ItemObject.GetAmmoTypeForItemType(weapon.Type);
                    var arrowSlots = usersHero.BattleEquipment
                        .YieldWeaponSlots()
                        .Where(e => e.element.Item?.Type == ammoType)
                        .ToList();
                    var (slot, index) = !arrowSlots.Any() ? FindEmptyWeaponSlot(usersHero.BattleEquipment) : arrowSlots.First();
                    if (index == EquipmentIndex.None)
                        index = EquipmentIndex.Weapon3;
                    if (slot.Item == null || slot.Item.Tier < (ItemObject.ItemTiers) settings.Tier)
                    {
                        var ammo = FindRandomTieredEquipment(null, settings.Tier, usersHero, null, ammoType);
                        if (ammo != null)
                        {
                            usersHero.BattleEquipment[index] = new EquipmentElement(ammo);
                            itemsPurchased.Add(ammo);
                        }
                    }
                }
            }

            if (settings.Armor)
            {
                foreach (var (index, itemType) in ArmorIndexType)
                {
                    var newItem = UpgradeItemInSlot(index, itemType, settings.Tier, usersHero.BattleEquipment, usersHero);
                    if (newItem != null) itemsPurchased.Add(newItem); 
                }
            }

            if (settings.Horse)
            {
                var newHorse = UpgradeItemInSlot(EquipmentIndex.Horse, ItemObject.ItemTypeEnum.Horse, settings.Tier, usersHero.BattleEquipment, usersHero);
                if (newHorse != null) itemsPurchased.Add(newHorse); 
                var newHarness = UpgradeItemInSlot(EquipmentIndex.HorseHarness, ItemObject.ItemTypeEnum.HorseHarness, settings.Tier, usersHero.BattleEquipment, usersHero);
                if (newHarness != null) itemsPurchased.Add(newHarness);
            }

            if (settings.Civilian)
            {
                foreach (var (index, itemType) in ArmorIndexType)
                {
                    var newItem = UpgradeItemInSlot(index, itemType, settings.Tier, usersHero.CivilianEquipment, usersHero, o => o.IsCivilian);
                    if (newItem != null) itemsPurchased.Add(newItem); 
                }

                var upgradeSlot = usersHero.CivilianEquipment.YieldWeaponSlots().FirstOrDefault(s => !s.element.IsEmpty);
                if (upgradeSlot.element.IsEmpty)
                    upgradeSlot = FindEmptyWeaponSlot(usersHero.CivilianEquipment);

                UpgradeItemInSlot(upgradeSlot.index, ItemObject.ItemTypeEnum.OneHandedWeapon, settings.Tier, usersHero.CivilianEquipment, usersHero);
            }
            
            if (!itemsPurchased.Any())
            {
                RewardManager.NotifyCancelled(redemptionId, $"Couldn't find any items to upgrade!");
                return;
            }
            
            var itemsStr = string.Join(", ", itemsPurchased.Select(i => i.Name.ToString()));
            RewardManager.NotifyComplete(redemptionId, $"You purchased these items: {itemsStr}!");
        }
    }
}