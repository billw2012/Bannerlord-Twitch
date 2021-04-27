using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox;
using SandBox.Source.Missions.Handlers;
using StoryMode.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

#pragma warning disable 649

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    public class BLTAdoptAHeroModule : MBSubModuleBase
    {
        public const string Name = "BLTAdoptAHero";
        public const string Ver = "1.0.0";

        public BLTAdoptAHeroModule()
        {
            RewardManager.RegisterAll(typeof(BLTAdoptAHeroModule).Assembly);
        }
    }
    
    [UsedImplicitly]
    [Desc("Allows viewer to 'adopt' a hero in game -- the hero name will change to the viewers, and they can control it with further commands")]
    public class AdoptAHero : IAction, ICommandHandler
    {
        public static readonly (CharacterAttributesEnum val, string shortName)[] CharAttributes = {
            (CharacterAttributesEnum.Vigor, "Vig"),
            (CharacterAttributesEnum.Control, "Con"),
            (CharacterAttributesEnum.Endurance, "End"),
            (CharacterAttributesEnum.Cunning, "Cun"),
            (CharacterAttributesEnum.Social, "Soc"),
            (CharacterAttributesEnum.Intelligence, "Int"),
        };
        
        public static readonly Dictionary<string, string> SkillMapping = new()
        {
            {"One Handed", "1h"},
            {"Two Handed", "2h"},
            {"Polearm", "PA"},
            {"Bow", "Bow"},
            {"Crossbow", "Xb"},
            {"Throwing", "Thr"},
            {"Riding", "Rid"},
            {"Athletics", "Ath"},
            {"Smithing", "Smt"},
            {"Scouting", "Sct"},
            {"Tactics", "Tac"},
            {"Roguery", "Rog"},
            {"Charm", "Cha"},
            {"Leadership", "Ldr"},
            {"Trade", "Trd"},
            {"Steward", "Stw"},
            {"Medicine", "Med"},
            {"Engineering", "Eng"},
        };

        internal const string NoHeroMessage = "Couldn't find your hero, did you adopt one yet?";
        internal const string NotStartedMessage = "The game isn't started yet";
        internal const string Tag = "[BLT]";
        
        // public class GlobalSettings
        // {
        // }
        //
        // private static GlobalSettings gSettings;
        //
        // public static GlobalSettings GetGlobalSettings()
        // {
        //     if (gSettings == null)
        //     {
        //         var config = RewardManager.FindGlobalConfig("BLTAdoptAHero");
        //         if (config == null)
        //         {
        //             Log.Trace($"{Name} - No global config found. Setting defaults.");
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
        //                     $"{Name} - global settings object couldn't be parsed ({ex.Message}), check your config file. Setting defaults.");
        //             }
        //         }
        //     }
        //     gSettings ??= new GlobalSettings {};
        //     return gSettings;
        // }

        private struct Settings
        {
            [Desc("Allow noble heroes")]
            public bool AllowNoble;
            [Desc("Allow wanderer heroes")]
            public bool AllowWanderer;
            [Desc("Allow companions")]
            public bool AllowPlayerCompanion;
            [Desc("Only allow same faction heroes")]
            public bool OnlySameFaction;
            [Desc("Only viewer to adopt another hero if theirs is dead")]
            public bool AllowNewAdoptionOnDeath;
            [Desc("Only subscribers can adopt")]
            public bool SubscriberOnly;
            [Desc("Only viewers who have been subscribers for at least this many months can adopt")]
            public int? MinSubscribedMonths;
            [Desc("Gold the adopted hero will start with, if you don't specify then they get the heroes existing gold")]
            public int? StartingGold;
            [Desc("Equipment tier the adopted hero will start with, if you don't specify then they get the heroes existing equipment")]
            public int? StartingEquipmentTier;
            [Desc("Whether the hero will start with a horse, only applies if <b>StartingEquipmentTier</b> is specified")]
            public bool StartWithHorse;
            [Desc("Whether the hero will start with armor, only applies if <b>StartingEquipmentTier</b> is specified")]
            public bool StartWithArmor;
        }

        private static IEnumerable<Hero> GetAvailableHeroes(Settings settings)
        {
            var tagText = new TextObject(Tag);
            return Campaign.Current?.AliveHeroes?.Where(h =>
                // Not the player of course
                h != Hero.MainHero
                // Don't want notables ever
                && !h.IsNotable && h.Age >= 18f 
                && (settings.AllowPlayerCompanion && h.IsPlayerCompanion
                   || settings.AllowNoble && h.IsNoble
                   || settings.AllowWanderer && h.IsWanderer)
                && (!settings.OnlySameFaction 
                    || Clan.PlayerClan?.MapFaction != null && Clan.PlayerClan?.MapFaction == h.Clan?.MapFaction)
            ).Where(n => !n.Name.Contains(tagText));
        }

        internal static string GetFullName(string name) => $"{name} {AdoptAHero.Tag}";

        internal static Hero GetAdoptedHero(string name)
        {
            var tagObject = new TextObject(AdoptAHero.Tag);
            var nameObject = new TextObject(name);
            return Campaign.Current?
                .AliveHeroes?
                .FirstOrDefault(h => (h.Name?.Contains(tagObject) ?? false) 
                                     && (h.FirstName?.Contains(nameObject) ?? false) 
                                     && h.FirstName?.ToString() == name);
        }

        Type IAction.ActionConfigType => typeof(Settings);
        void IAction.Enqueue(Guid redemptionId, string args, string userName, object config)
        {
            var hero = GetAdoptedHero(userName);
            if (hero?.IsAlive == true)
            {
                RewardManager.NotifyCancelled(redemptionId, "You have already adopted a hero!");
                return;
            }
            var settings = (Settings)config;
            var (success, message) = ExecuteInternal(hero, args, userName, settings);
            if (success)
            {
                RewardManager.NotifyComplete(redemptionId, message);
            }
            else
            {
                RewardManager.NotifyCancelled(redemptionId, message);
            }
        }
        
        Type ICommandHandler.HandlerConfigType => typeof(Settings);
        void ICommandHandler.Execute(string args, CommandMessage commandMessage, object config)
        {
            var hero = GetAdoptedHero(commandMessage.UserName);
            if (hero?.IsAlive == true)
            {
                RewardManager.SendReply(commandMessage.ReplyId, "You have already adopted a hero!");
                return;
            }

            var settings = (Settings)config;
            if (settings.MinSubscribedMonths > 0 && commandMessage.SubscribedMonthCount < settings.MinSubscribedMonths)
            {
                RewardManager.SendReply(commandMessage.ReplyId, $"You must be subscribed for at least {settings.MinSubscribedMonths} months to adopt a hero with this command!");
                return;
            }
            if(!commandMessage.IsSubscriber && settings.SubscriberOnly)
            {
                RewardManager.SendReply(commandMessage.ReplyId, $"You must be subscribed to adopt a hero with this command!");
                return;
            }
                
            var (_, message) = ExecuteInternal(hero, args, commandMessage.UserName, settings);
            RewardManager.SendReply(commandMessage.ReplyId, message);
        }

        private static (bool success, string message) ExecuteInternal(Hero hero, string args, string userName, Settings settings)
        {
            if (Campaign.Current == null)
            {
                return (false, AdoptAHero.NotStartedMessage);
            }
            
            if (hero?.IsAlive == false && !settings.AllowNewAdoptionOnDeath)
            {
                return (false, $"Your hero died, and you may not adopt another!");
            }
            
            var randomHero = string.IsNullOrEmpty(args)
                ? GetAvailableHeroes(settings).SelectRandom() 
                : Campaign.Current?.AliveHeroes?.FirstOrDefault(h => h.Name.Contains(args) && h.Name.ToString() == args);
            if (randomHero == null)
            {
                return (false, $"You can't adopt a hero: no available hero matching the requirements was found!");
            }
            
            var oldName = randomHero.Name.ToString();
            randomHero.FirstName = new TextObject(userName);
            randomHero.Name = new TextObject(GetFullName(userName));
            if(settings.StartingGold.HasValue)
                randomHero.Gold = settings.StartingGold.Value;

            if (settings.StartingEquipmentTier.HasValue)
            {
                EquipHero.RemoveAllEquipment(randomHero);
                EquipHero.UpgradeEquipment(randomHero, settings.StartingEquipmentTier.Value, true, true,
                    settings.StartWithArmor, settings.StartWithHorse, true);
            }

            return (true, $"You have adopted {oldName}, they have {randomHero.Gold} gold!");
        }
    }

    [UsedImplicitly]
    [Desc("Will write various hero stats to chat")]
    internal class HeroInfoCommand : ICommandHandler
    {
        
        private class Settings
        {
            [Desc("Show general info: gold, health, location, age")]
            public bool ShowGeneral;
            [Desc("Shows skills (and focuse values) above the specified MinSkillToShow value")]
            public bool ShowTopSkills;
            [Desc("If ShowTopSkills is specified, this defines what skills are shown")]
            public int MinSkillToShow;
            [Desc("Shows all hero attributes")]
            public bool ShowAttributes;
            [Desc("Shows the battle and civilian equipment of the hero")]
            public bool ShowEquipment;
        }
        
        // One Handed, Two Handed, Polearm, Bow, Crossbow, Throwing, Riding, Athletics, Smithing
        // Scouting, Tactics, Roguery, Charm, Leadership, Trade, Steward, Medicine, Engineering

        Type ICommandHandler.HandlerConfigType => typeof(Settings);
        void ICommandHandler.Execute(string args, CommandMessage commandMessage, object config)
        {
            var settings = config as Settings ?? new Settings();
            var adoptedHero = AdoptAHero.GetAdoptedHero(commandMessage.UserName);
            var infoStrings = new List<string>{};
            if (adoptedHero == null)
            {
                infoStrings.Add(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
            }
            else
            {
                if (settings.ShowGeneral)
                {
                    if (adoptedHero.Clan != null)
                    {
                        infoStrings.Add($"Clan {adoptedHero.Clan.Name}");
                    }
                    infoStrings.Add($"{adoptedHero.Culture}");
                    infoStrings.Add($"{adoptedHero.Gold} gold");
                    infoStrings.Add($"{adoptedHero.Age:0} yrs");
                    infoStrings.Add($"{adoptedHero.HitPoints} / {adoptedHero.CharacterObject.MaxHitPoints()} HP");
                    if (adoptedHero.LastSeenPlace != null)
                    {
                        infoStrings.Add($"Last seen near {adoptedHero.LastSeenPlace.Name}");
                    }
                }
                if (settings.ShowTopSkills)
                {
                    infoStrings.Add($"Level {adoptedHero.Level}");
                    infoStrings.Add("Skills ■ " + string.Join(" ■ ", 
                        SkillObject.All
                            .Where(s => adoptedHero.GetSkillValue(s) >= settings.MinSkillToShow)
                            .OrderByDescending(s => adoptedHero.GetSkillValue(s))
                            .Select(skill => $"{AdoptAHero.SkillMapping[skill.Name.ToString()]} {adoptedHero.GetSkillValue(skill)} " +
                                             $"[f{adoptedHero.HeroDeveloper.GetFocus(skill)}]")
                        ));
                }
                if (settings.ShowAttributes)
                {
                    infoStrings.Add("Attr ■ " + string.Join(" ■ ", AdoptAHero.CharAttributes
                            .Select(a => $"{a.shortName} {adoptedHero.GetAttributeValue(a.val)}")));
                }
                if (settings.ShowEquipment)
                {
                    infoStrings.Add($"Equip Tier {EquipHero.GetHeroEquipmentTier(adoptedHero) + 1}");
                    infoStrings.Add("Battle ■ " + string.Join(" ■ ", adoptedHero.BattleEquipment
                        .YieldEquipmentSlots()
                        .Where(e => !e.element.IsEmpty)
                        .Select(e => $"{e.element.Item.Name}")
                    ));
                    infoStrings.Add("Civ ■ " + string.Join(" ■ ", adoptedHero.CivilianEquipment
                        .YieldEquipmentSlots()
                        .Where(e => !e.element.IsEmpty)
                        .Select(e => $"{e.element.Item.Name}")
                    ));
                }
            }

            RewardManager.SendReply(commandMessage.ReplyId, infoStrings.ToArray());
        }
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
    
    [UsedImplicitly]
    [Desc("Gives gold to the adopted hero")]
    internal class AddGoldToHero : IAction
    {
        private class Settings
        {
            [Desc("How much gold to give the adopted hero")]
            public int Amount;
        }

        Type IAction.ActionConfigType => typeof(Settings);
        void IAction.Enqueue(Guid redemptionId, string message, string userName, object config)
        {
            var settings = (Settings)config;
            var adoptedHero = AdoptAHero.GetAdoptedHero(userName);
            if (adoptedHero == null)
            {
                RewardManager.NotifyCancelled(redemptionId, Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }

            adoptedHero.Gold += settings.Amount;
            RewardManager.NotifyComplete(redemptionId, $"+{settings.Amount} gold, you now have {adoptedHero.Gold}!");
        }
    }

    internal abstract class ImproveAdoptedHero : ActionAndHandlerBase
    {
        protected class SettingsBase
        {
            [Desc("Lower bound of amount to improve")]
            public int AmountLow;
            [Desc("Upper bound of amount to improve")]
            public int AmountHigh;
            public int GoldCost;
        }

        // protected override Type ConfigType => typeof(SettingsBase);

        protected override void ExecuteInternal(string userName, string args, object config,
            Action<string> onSuccess,
            Action<string> onFailure) 
        {
            var settings = (SettingsBase)config;
            var adoptedHero = AdoptAHero.GetAdoptedHero(userName);
            if (adoptedHero == null)
            {
                onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }

            if (adoptedHero.Gold < settings.GoldCost)
            {
                onFailure($"You do not have enough gold: you need {settings.GoldCost}, and you only have {adoptedHero.Gold}!");
                return;
            }
            
            var amount = MBRandom.RandomInt(settings.AmountLow, settings.AmountHigh);
            var (success, description) = Improve(userName, adoptedHero, amount, settings);
            if (success)
            {
                onSuccess(description);
                adoptedHero.Gold -= settings.GoldCost;
            }
            else
            {
                onFailure(description);
            }
        }

        protected abstract (bool success, string description) Improve(string userName, Hero adoptedHero, int amount, SettingsBase settings);

        // One Handed, Two Handed, Polearm, Bow, Crossbow, Throwing, Riding, Athletics, Smithing
        // Scouting, Tactics, Roguery, Charm, Leadership, Trade, Steward, Medicine, Engineering

        private static string[][] SkillGroups =
        {
            new[] { "One Handed", "Two Handed", "Polearm" }, // melee
            new[] { "Bow", "Crossbow", "Throwing" }, // ranged
            new[] { "Smithing", "Scouting", "Trade", "Steward", "Engineering" }, // support
            new[] { "Riding", "Athletics" }, // movement
            new[] { "Tactics", "Roguery", "Charm", "Leadership" }, // personal
        };
        protected static SkillObject GetSkill(Hero hero, string improvement, bool random, bool auto, Func<SkillObject, bool> predicate = null)
        {
            // DOING: fix this so that group is selected last (so if all items in a group fail the predicate that group isn't chosen. Perhaps choose top skill in each group then randomly among those).
            // We will select automatically which skill from groups
            var selectedSkills = auto
                    ? SkillGroups
                        .Select(g 
                            => g.Select(sn => DefaultSkills.GetAllSkills().FirstOrDefault(so => string.Equals(so.Name.ToString(), sn, StringComparison.CurrentCultureIgnoreCase)))
                                .Where(predicate ?? (s => true)))
                        .Where(g => g.Any())
                        .SelectRandom()
                    : improvement
                        .Split(',')
                        .Select(s => s.Trim())
                        .Select(sn => DefaultSkills.GetAllSkills().FirstOrDefault(so => string.Equals(so.Name.ToString(), sn, StringComparison.CurrentCultureIgnoreCase)))
                        .Where(predicate ?? (s => true))
                ;
            return random 
                ? selectedSkills?.SelectRandom() 
                : selectedSkills?.OrderByDescending(hero.GetSkillValue).FirstOrDefault();
        }
    }
    
    [UsedImplicitly]
    [Desc("Improve adopted heroes skills")]
    internal class SkillXP : ImproveAdoptedHero
    {
        protected class SkillXPSettings : SettingsBase
        {
            [Desc("What to improve, multiple values can be separated by commas. Skills are One Handed, Two Handed, Polearm, Bow, Crossbow, Throwing, Riding, Athletics, Smithing, Scouting, Tactics, Roguery, Charm, Leadership, Trade, Steward, Medicine, Engineering.")]
            public string Skills;
            [Desc("Improve a random skill from the Skills specified, rather than the best one")]
            public bool Random;
            [Desc("If this is specified then the best skill from a random skill group will be improved, Skills list is ignored. Groups are melee (One Handed, Two Handed, Polearm), ranged (Bow, Crossbow, Throwing), support (Smithing, Scouting, Trade, Steward, Engineering), movement (Riding, Athletics), personal (Tactics, Roguery, Charm, Leadership)")]
            public bool Auto;
        }
        
        protected override Type ConfigType => typeof(SkillXPSettings);

        protected override (bool success, string description) Improve(string userName,
            Hero adoptedHero, int amount, SettingsBase baseSettings)
        {
            var settings = (SkillXPSettings) baseSettings;
            var skill = GetSkill(adoptedHero, settings.Skills, settings.Random, settings.Auto);
            if (skill == null) return (false, $"Couldn't improve skill {settings.Skills}: its not a valid skill name!");
            float prevSkill = adoptedHero.HeroDeveloper.GetPropertyValue(skill);
            int prevLevel = adoptedHero.GetSkillValue(skill);
            adoptedHero.HeroDeveloper.AddSkillXp(skill, amount);
            float realGainedXp = adoptedHero.HeroDeveloper.GetPropertyValue(skill) - prevSkill;
            int newLevel = adoptedHero.GetSkillValue(skill);
            int gainedLevels = newLevel - prevLevel;
            return realGainedXp < 1f
                ? (false, $"Couldn't improve skill {skill.Name} any further, get more focus points!")
                : gainedLevels > 1
                    ? (true, $"You have gained {gainedLevels} levels in {skill.Name}, you are now {newLevel}!")
                    : gainedLevels == 1
                        ? (true, $"You have gained a level in {skill.Name}, you are now {newLevel}!")
                        : (true, $"You have gained {realGainedXp:0}xp (adjusted by focus) in {skill.Name}, you now have {adoptedHero.GetSkillValue(skill)}!");
        }
    }

    [UsedImplicitly]
    [Desc("Add focus points to heroes skills")]
    internal class FocusPoints : ImproveAdoptedHero
    {
        protected class FocusPointsSettings : SettingsBase
        {
            [Desc("What to add focus to, multiple values can be separated by commas. Skills are One Handed, Two Handed, Polearm, Bow, Crossbow, Throwing, Riding, Athletics, Smithing, Scouting, Tactics, Roguery, Charm, Leadership, Trade, Steward, Medicine, Engineering.")]
            public string Skills;
            [Desc("Add focus to a random skill <b>from the Skills specified</b>, rather than the best one.")]
            public bool Random;
            [Desc("If this is specified then the best skill from a random skill group will have focus added, <code>Skills</code> list is ignored. Groups are melee (One Handed, Two Handed, Polearm), ranged (Bow, Crossbow, Throwing), support (Smithing, Scouting, Trade, Steward, Engineering), movement (Riding, Athletics), personal (Tactics, Roguery, Charm, Leadership)")]
            public bool Auto;
        }
        
        protected override Type ConfigType => typeof(FocusPointsSettings);
        
        protected override (bool success, string description) Improve(string userName,
            Hero adoptedHero, int amount, SettingsBase baseSettings)
        {
            var settings = (FocusPointsSettings) baseSettings;
            
            var skill = GetSkill(adoptedHero, settings.Skills, settings.Random, settings.Auto,
                s => adoptedHero.HeroDeveloper.GetFocus(s) < 5);

            if (skill == null)
            {
                return (false, $"Couldn't find a valid skill to add focus points to!");
            }
            
            amount = Math.Min(amount, 5 - adoptedHero.HeroDeveloper.GetFocus(skill));
            adoptedHero.HeroDeveloper.AddFocus(skill, amount, checkUnspentFocusPoints: false);
            return (true, $"You have gained {amount} focus point{(amount > 1? "s" : "")} in {skill}, you now have {adoptedHero.HeroDeveloper.GetFocus(skill)}!");
        }
    }

    [UsedImplicitly]
    [Desc("Improve adopted heroes attribute points")]
    internal class AttributePoints : ImproveAdoptedHero
    {
        protected class AttributePointsSettings : SettingsBase
        {
            [Desc("Which attribute to improve (specify one only). Possible values are Random (to improve any random attribute) Vigor, Control, Endurance, Cunning, Social, Intelligence.")]
            public string Attribute;
        }
        
        protected override Type ConfigType => typeof(AttributePointsSettings);
        
        // Vigor, Control, Endurance, Cunning, Social, Intelligence
        protected override (bool success, string description) Improve(string userName,
            Hero adoptedHero, int amount, SettingsBase baseSettings)
        {
            var settings = (AttributePointsSettings) baseSettings;
            // Get attributes that can be buffed
            var improvableAttributes = AdoptAHero.CharAttributes
                .Select(c => c.val)
                .Where(a => adoptedHero.GetAttributeValue(a) < 10)
                .ToList();

            if (!improvableAttributes.Any())
            {
                return (false, $"Couldn't improve any attributes, they are all at max level!");
            }
            CharacterAttributesEnum attribute;
            if (string.Equals(settings.Attribute, "random", StringComparison.InvariantCultureIgnoreCase))
            {
                attribute = improvableAttributes.SelectRandom();
            }
            else
            {
                var matching = improvableAttributes
                    .Where(a => string.Equals(a.ToString(), settings.Attribute, StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
                if (!matching.Any())
                {
                    return (false, $"Couldn't improve attribute {settings.Attribute}, its not a valid attribute name!");
                }
                attribute = matching.First();
            }

            amount = Math.Min(amount, 10 - adoptedHero.GetAttributeValue(attribute));
            adoptedHero.HeroDeveloper.AddAttribute(attribute, amount, checkUnspentPoints: false);
            return (true, $"You have gained {amount} point{(amount > 1? "s" : "")} in {attribute}, you now have {adoptedHero.GetAttributeValue(attribute)}!");
        }
    }

    [UsedImplicitly]
    [Desc("Spawns the adopted hero into the current active mission")]
    internal class SummonHero : ActionAndHandlerBase
    {
        private class Settings
        {
            [Desc("Can summon for normal field battles between parties")]
            public bool AllowFieldBattle;
            [Desc("Can summon in village battles")]
            public bool AllowVillageBattle;
            [Desc("Can summon in sieges")]
            public bool AllowSiegeBattle;
            [Desc("This includes walking about village/town/dungeon/keep")]
            public bool AllowFriendlyMission;
            [Desc("Can summon in the practice arena")]
            public bool AllowArena;
            [Desc("NOT IMPLEMENTED YET Can summon in tournaments")]
            public bool AllowTournament;
            [Desc("Can summon in the hideout missions")]
            public bool AllowHideOut;
            [Desc("Whether the hero is on the player or enemy side")]
            public bool OnPlayerSide;
            [Desc("Gold cost to summon")]
            public int GoldCost;
            [Desc("Gold won if the heroes side wins")]
            public int WinGold;
            [Desc("Gold lost if the heroes side loses")]
            public int LoseGold;
        }

        protected override Type ConfigType => typeof(Settings);
        
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

        private class BLTMissionResultListener : MissionBehaviour
        {
            public override MissionBehaviourType BehaviourType => MissionBehaviourType.Other;

            public delegate void MissionOverDelegate(Hero hero);
            public delegate void MissionModeChangeDelegate(Hero hero, MissionMode oldMode, MissionMode newMode, bool atStart);
            public delegate void MissionResetDelegate(Hero hero);

            private struct Listeners
            {
                public MissionOverDelegate onMissionOver;
                public MissionModeChangeDelegate onModeChange;
                public MissionResetDelegate onMissionReset;
            }

            private readonly Dictionary<Hero, Listeners> listeners = new();

            public void AddListeners(Hero hero, MissionOverDelegate onMissionOver = null, MissionModeChangeDelegate onModeChange = null, MissionResetDelegate onMissionReset = null)
            {
                listeners[hero] = new()
                {
                    onMissionOver = onMissionOver,
                    onModeChange = onModeChange,
                    onMissionReset = onMissionReset,
                };
            }

            public void RemoveListeners(Hero hero)
            {
                listeners.Remove(hero);
            }

            protected override void OnEndMission()
            {
                foreach (var (hero, ev) in listeners.Select(kv => (hero: kv.Key, ev: kv.Value)).ToArray())
                {
                    ev.onMissionOver?.Invoke(hero);
                }
            }

            // public override void OnMissionActivate()
            // {
            //     base.OnMissionActivate();
            // }
            //
            // public override void OnMissionDeactivate()
            // {
            //     base.OnMissionDeactivate();
            // }
            //
            // public override void OnMissionRestart()
            // {
            //     base.OnMissionRestart();
            // }

            public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
            {
                foreach (var (hero, ev) in listeners.Select(kv => (hero: kv.Key, ev: kv.Value)).ToArray())
                {
                    ev.onModeChange?.Invoke(hero, oldMissionMode, Mission.Current.Mode, atStart);
                }
            }
            
            public static BLTMissionResultListener Get()
            {
                var beh = Mission.Current.GetMissionBehaviour<BLTMissionResultListener>();
                if (beh == null)
                {
                    beh = new BLTMissionResultListener();
                    Mission.Current.AddMissionBehaviour(beh);
                }
                return beh;
            }
        }

        private class RemoveAgentsBehavior : MissionBehaviour
        {
            private List<Hero> herosAdded = new();
            
            public override MissionBehaviourType BehaviourType => MissionBehaviourType.Other;

            public void Add(Hero hero)
            {
                herosAdded.Add(hero);
            }

            private void RemoveHeroes()
            {
                foreach (var hero in herosAdded)
                {
                    LocationComplex.Current?.RemoveCharacterIfExists(hero);
                    if(CampaignMission.Current?.Location?.ContainsCharacter(hero) ?? false)
                        CampaignMission.Current.Location.RemoveCharacter(hero);
                }
                herosAdded.Clear();
            }
            
            public override void HandleOnCloseMission()
            {
                base.HandleOnCloseMission();
                RemoveHeroes();
            }

            protected override void OnEndMission()
            {
                base.OnEndMission();
                RemoveHeroes();
            }

            public override void OnMissionDeactivate()
            {
                base.OnMissionDeactivate();
                RemoveHeroes();
            }

            public override void OnMissionRestart()
            {
                base.OnMissionRestart();
                RemoveHeroes();
            }

            public static RemoveAgentsBehavior Get()
            {
                var beh = Mission.Current.GetMissionBehaviour<RemoveAgentsBehavior>();
                if (beh == null)
                {
                    beh = new RemoveAgentsBehavior();
                    Mission.Current.AddMissionBehaviour(beh);
                }
                return beh;
            }
        }

        protected override void ExecuteInternal(string userName, string args, object config,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            var settings = (Settings) config;

            var adoptedHero = AdoptAHero.GetAdoptedHero(userName);
            if (adoptedHero == null)
            {
                onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            if (adoptedHero.Gold < settings.GoldCost)
            {
                onFailure($"You do not have enough gold: you need {settings.GoldCost}, and you only have {adoptedHero.Gold}!");
                return;
            }
            if (adoptedHero.IsPlayerCompanion)
            {
                onFailure($"You are a player companion, you cannot be summoned in this manner!");
                return;
            }
            
            // SpawnAgent crashes in MissionMode.Deployment, would be nice to make it work though
            if (Mission.Current == null 
                || Mission.Current.Mode is MissionMode.Barter or MissionMode.Conversation or MissionMode.Deployment or
                    MissionMode.Duel or MissionMode.Replay or MissionMode.CutScene)
            {
                onFailure($"You cannot be summoned now!");
                return;
            }

            if(InArenaPracticeMission() && (!settings.AllowArena || !settings.OnPlayerSide) 
               || InTournament() && (!settings.AllowTournament)
               || InFieldBattleMission() && !settings.AllowFieldBattle
               || InVillageEncounter() && !settings.AllowVillageBattle
               || InSiegeMission() && !settings.AllowSiegeBattle
               || InFriendlyMission() && !settings.AllowFriendlyMission
               || InHideOutMission() && !settings.AllowHideOut
               || InTrainingFieldMission()
               || InArenaPracticeVisitingArea()
               )
            {
                onFailure($"You cannot be summoned now, this mission does not allow it!");
                return;
            }

            if (!Mission.Current.IsLoadingFinished)
            {
                onFailure($"You cannot be summoned now, the mission has not started yet!");
                return;
            }
            if (Mission.Current.IsMissionEnding || (Mission.Current.MissionResult?.BattleResolved ?? false))
            {
                onFailure($"You cannot be summoned now, the mission is ending!");
                return;
            }
            
            if (HeroIsSpawned(adoptedHero))
            {
                onFailure($"You cannot be summoned, you are already here!");
                return;
            }


            if (CampaignMission.Current.Location != null)
            {
                var locationCharacter = LocationCharacter.CreateBodyguardHero(adoptedHero,
                    MobileParty.MainParty,
                    SandBoxManager.Instance.AgentBehaviorManager.AddBodyguardBehaviors);
                
                var missionAgentHandler = Mission.Current.GetMissionBehaviour<MissionAgentHandler>();
                var worldFrame = missionAgentHandler.Mission.MainAgent.GetWorldFrame();
                worldFrame.Origin.SetVec2(worldFrame.Origin.AsVec2 + (worldFrame.Rotation.f * 10f + worldFrame.Rotation.s).AsVec2);
                
                CampaignMission.Current.Location.AddCharacter(locationCharacter);
                var agent = SpawnWanderingAgent(missionAgentHandler, locationCharacter, worldFrame.ToGroundMatrixFrame(), false, true); 

                agent.SetTeam(settings.OnPlayerSide 
                    ? missionAgentHandler.Mission.PlayerTeam
                    : missionAgentHandler.Mission.PlayerEnemyTeam, false);

                // For arena mission we add fight everyone behaviours
                if (InArenaPracticeMission())
                {
                    if (agent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
                    {
                        var behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator
                            .GetBehaviorGroup<AlarmedBehaviorGroup>();
                        behaviorGroup.DisableCalmDown = true;
                        behaviorGroup.AddBehavior<FightBehavior>();
                        behaviorGroup.SetScriptedBehavior<FightBehavior>();
                    }
                    agent.SetWatchState(AgentAIStateFlagComponent.WatchState.Alarmed);
                }
                // For other player hostile situations we setup a 1v1 fight
                else if (!settings.OnPlayerSide)
                {
                    Mission.Current.GetMissionBehaviour<MissionFightHandler>().StartCustomFight(
                        new() {Agent.Main},
                        new() {agent}, false, false, false,
                        playerWon =>
                        {
                            if (settings.WinGold != 0)
                            {
                                if (!playerWon)
                                {
                                    Hero.MainHero.ChangeHeroGold(-settings.WinGold);
                                    // User gets their gold back also
                                    adoptedHero.ChangeHeroGold(settings.WinGold + settings.GoldCost);
                                    RewardManager.SendChat(
                                        $@"{userName} Victory! You won {settings.WinGold} gold, you now have {adoptedHero.Gold}!");
                                }
                                else if(settings.LoseGold > 0)
                                {
                                    Hero.MainHero.ChangeHeroGold(settings.LoseGold);
                                    adoptedHero.ChangeHeroGold(-settings.LoseGold);
                                    RewardManager.SendChat(
                                        $@"{userName} Defeat! You lost {settings.LoseGold + settings.GoldCost} gold, you now have {adoptedHero.Gold}!");
                                }
                            }
                        },
                        true, null, null, null, null);
                }
                else
                {
                    InformationManager.AddQuickInformation(new TextObject($"I'm here!"), 1000,
                        adoptedHero.CharacterObject, "event:/ui/mission/horns/move");
                }

                // Bodyguard
                if (settings.OnPlayerSide && agent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
                {
                    var behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
                    (behaviorGroup.GetBehavior<FollowAgentBehavior>() ?? behaviorGroup.AddBehavior<FollowAgentBehavior>()).SetTargetAgent(Agent.Main);
                    behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
                }
                
                RemoveAgentsBehavior.Get().Add(adoptedHero);
                // missionAgentHandler.SimulateAgent(agent);
            }
            else
            {
                PartyBase party = null;
                if (settings.OnPlayerSide && Mission.Current?.PlayerTeam != null)
                {
                    party = PartyBase.MainParty;
                }
                else if(!settings.OnPlayerSide && Mission.Current?.PlayerEnemyTeam != null)
                {
                    party = Mission.Current.PlayerEnemyTeam?.TeamAgents
                        ?.Select(a => a.Origin?.BattleCombatant as PartyBase)
                        .Where(p => p != null).SelectRandom();
                }

                if (party == null)
                {
                    onFailure($"Could not find a party for you to join!");
                    return;
                }
                
                BLTMissionResultListener.Get().AddListeners(adoptedHero, onMissionOver: _ =>
                {
                    if (Mission.Current.MissionResult != null)
                    {
                        if (settings.OnPlayerSide == Mission.Current.MissionResult.PlayerVictory)
                        {
                            // User gets their gold back also
                            adoptedHero.ChangeHeroGold(settings.WinGold + settings.GoldCost);
                            RewardManager.SendChat($@"{userName} Victory! You won {settings.WinGold} gold, you now have {adoptedHero.Gold}!");
                        }
                        else if(settings.LoseGold > 0)
                        {
                            adoptedHero.ChangeHeroGold(-settings.LoseGold);
                            RewardManager.SendChat($@"{userName} Defeat! You lost {settings.LoseGold + settings.GoldCost} gold, you now have {adoptedHero.Gold}!");
                        }
                    }
                });

                Mission.Current.SpawnTroop(
                    new PartyAgentOrigin(party, adoptedHero.CharacterObject),
                    isPlayerSide: settings.OnPlayerSide,
                    hasFormation: true,
                    spawnWithHorse: adoptedHero.CharacterObject.HasMount() && Mission.Current.Mode != MissionMode.Stealth,
                    isReinforcement: true,
                    enforceSpawningOnInitialPoint: false,
                    formationTroopCount: 1,
                    formationTroopIndex: 8,
                    isAlarmed: true,
                    wieldInitialWeapons: true);
            }

            var messages = settings.OnPlayerSide
                ? new List<string>
                {
                    "Don't worry, I've got your back!",
                    "I'm here!",
                    "Which one should I stab?",
                    "Once more unto the breach!",
                    "It's nothing personal!",
                    "Freeeeeedddooooooommmm!",
                }
                : new List<string>
                {
                    "Defend yourself!",
                    "Time for you to die!",
                    "You killed my father, prepare to die!",
                    "En garde!",
                    "Its stabbing time!",
                    "It's nothing personal!",
                };
            if (InSiegeMission() && settings.OnPlayerSide) messages.Add($"Don't send me up the siege tower, its confusing!");
            InformationManager.AddQuickInformation(new TextObject(!string.IsNullOrEmpty(args) ? args : messages.SelectRandom()), 1000,
                adoptedHero.CharacterObject, "event:/ui/mission/horns/attack");

            adoptedHero.Gold -= settings.GoldCost;
            onSuccess($"You have joined the battle!");
        }

        private static bool HeroIsSpawned(Hero hero) =>
            (CampaignMission.Current.Location?.ContainsCharacter(hero) ?? false)
            || (Mission.Current?.Agents.Any(a => a.Character == hero.CharacterObject) ?? false);

        private static bool InHideOutMission() => Mission.Current?.Mode == MissionMode.Stealth;
        private static bool InFieldBattleMission() => Mission.Current?.IsFieldBattle ?? false;

        private static bool InSiegeMission() => !(Mission.Current?.IsFieldBattle ?? false)
                                                && Mission.Current?.GetMissionBehaviour<CampaignSiegeStateHandler>() != null;
        private static bool InArenaPracticeMission() => CampaignMission.Current?.Location?.StringId == "arena" 
                                                      && Mission.Current?.Mode == MissionMode.Battle;
        private static bool InArenaPracticeVisitingArea() => CampaignMission.Current?.Location?.StringId == "arena" 
                                                && Mission.Current?.Mode != MissionMode.Battle;

        private static bool InTournament() => Mission.Current?.GetMissionBehaviour<TournamentFightMissionController>() != null 
                                              && Mission.Current?.Mode == MissionMode.Battle;

        private static bool InFriendlyMission() => (Mission.Current?.IsFriendlyMission ?? false) && !InArenaPracticeMission();
        private static bool InConversation() => Mission.Current?.Mode == MissionMode.Conversation;
        private static bool InTrainingFieldMission() => Mission.Current?.GetMissionBehaviour<TrainingFieldMissionController>() != null;
        private static bool InVillageEncounter() => PlayerEncounter.LocationEncounter?.GetType() == typeof(VillageEncouter);
    }
    
    [UsedImplicitly]
    [Desc("Improve adopted heroes equipment")]
    internal class EquipHero : ActionAndHandlerBase
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

        private struct Settings
        {
            [Desc("Improve armor")]
            public bool Armor;
            [Desc("Improve melee weapons (one handled, two handed, polearm). The one the player has the highest skill in will be selected.")]
            public bool Melee;
            [Desc("Improve ranged weapons (bow, crossbow, throwing). The one the player has the highest skill in will be selected.")]
            public bool Ranged;
            [Desc("Improve the heroes horse (if they can ride a better one).")]
            public bool Horse;
            [Desc("Improve the heroes civilian equipment.")]
            public bool Civilian;
            [Desc("Allow improvement of adopted heroes who are also companions of the player.")]
            public bool AllowCompanionUpgrade;
            [Desc("Tier to upgrade to. Anything better than this tier will be left alone, viewer will be refunded if nothing could be upgraded.")]
            public int? Tier; // 0 to 5
            [Desc("Upgrade to the next tier from the current one, viewer will be refunded if nothing could be upgraded.")]
            public bool Upgrade;
            [Desc("Gold cost to the adopted hero")]
            public int GoldCost;
            [Desc("Whether to multiply the cost by the current tier")]
            public bool MultiplyCostByCurrentTier;
        }

        protected override Type ConfigType => typeof(Settings);
        
        public static int GetHeroEquipmentTier(Hero hero) =>
            // The Mode of the tiers of the equipment
            hero.BattleEquipment.YieldEquipmentSlots().Concat(hero.CivilianEquipment.YieldEquipmentSlots())
                .Select(s => s.element.Item)
                .Where(i => i != null)
                .Select(i => (int)i.Tier)
                .GroupBy(v => v)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

        protected override void ExecuteInternal(string userName, string args, object config,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            var settings = (Settings)config;
            var adoptedHero = AdoptAHero.GetAdoptedHero(userName);
            if (adoptedHero == null)
            {
                onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            if (!settings.AllowCompanionUpgrade && adoptedHero.IsPlayerCompanion)
            {
                onFailure($"You are a player companion, you cannot change your own equipment!");
                return;
            }
            if (Mission.Current != null)
            {
                onFailure($"You cannot upgrade equipment, as a mission is active!");
                return;
            }
            if (!settings.Tier.HasValue && !settings.Upgrade)
            {
                onFailure($"Configuration is invalid, either Tier or Upgrade must be specified");
                return;
            }
            int targetTier = settings.Upgrade 
                ? GetHeroEquipmentTier(adoptedHero) + 1 
                : settings.Tier.Value
                ;
            
            if (targetTier > 5)
            {
                onFailure($"You cannot upgrade any further!");
                return;
            }

            int cost = settings.MultiplyCostByCurrentTier
                ? settings.GoldCost * targetTier
                : settings.GoldCost;

            if (adoptedHero.Gold < cost)
            {
                onFailure($"You do not have enough gold: you need {cost}, and you only have {adoptedHero.Gold}!");
                return;
            }

            var itemsPurchased = UpgradeEquipment(adoptedHero, targetTier, settings.Melee, settings.Ranged, settings.Armor, settings.Horse, settings.Civilian);

            if (!itemsPurchased.Any())
            {
                onFailure($"Couldn't find any items to upgrade!");
                return;
            }
            
            string itemsStr = string.Join(", ", itemsPurchased.Select(i => i.Name.ToString()));
            
            adoptedHero.Gold -= cost;
            onSuccess($"You purchased these items: {itemsStr}!");
        }

        internal static void RemoveAllEquipment(Hero adoptedHero)
        {
            foreach (var slot in adoptedHero.BattleEquipment.YieldEquipmentSlots())
            {
                adoptedHero.BattleEquipment[slot.index] = EquipmentElement.Invalid;
            }
            foreach (var slot in adoptedHero.CivilianEquipment.YieldEquipmentSlots())
            {
                adoptedHero.CivilianEquipment[slot.index] = EquipmentElement.Invalid;
            }
        }
        
        internal static List<ItemObject> UpgradeEquipment(Hero adoptedHero, int targetTier, bool upgradeMelee, bool upgradeRanged, bool upgradeArmor, bool upgradeHorse, bool upgradeCivilian)
        {
            var itemsPurchased = new List<ItemObject>();

            static bool CanUseItem(ItemObject item, Hero hero)
            {
                var relevantSkill = item.RelevantSkill;
                return (relevantSkill == null || hero.GetSkillValue(relevantSkill) >= item.Difficulty)
                       && (!hero.IsFemale || !item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByFemale))
                       && (hero.IsFemale || !item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale));
            }

            static List<(EquipmentElement element, EquipmentIndex index)> GetMatchingItems(SkillObject skill,
                Equipment equipment, params ItemObject.ItemTypeEnum[] itemTypeEnums)
            {
                return equipment
                    .YieldWeaponSlots()
                    .Where(e => !e.element.IsEmpty)
                    .Where(e => itemTypeEnums.Contains(e.element.Item.Type))
                    .Where(e => e.element.Item.RelevantSkill == skill)
                    .ToList();
            }

            static void RemoveNonBestSkillItems(IEnumerable<SkillObject> skills, SkillObject bestSkill, Equipment equipment,
                params ItemObject.ItemTypeEnum[] itemTypeEnums)
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

            static void RemoveNonBestMatchingWeapons(SkillObject skillObject, Equipment equipment,
                params ItemObject.ItemTypeEnum[] itemTypeEnums)
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

            static ItemObject FindRandomTieredEquipment(SkillObject skill, int tier, Hero hero,
                Func<ItemObject, bool> filter = null, params ItemObject.ItemTypeEnum[] itemTypeEnums)
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

            static ItemObject UpgradeWeapon(SkillObject skill, SkillObject[] skillGroup,
                ItemObject.ItemTypeEnum[] itemTypeEnums, EquipmentIndex defaultEquipmentIndex, Hero hero, Equipment equipment,
                int tier, Func<ItemObject, bool> filter = null)
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

            static ItemObject UpgradeItemInSlot(EquipmentIndex equipmentIndex, ItemObject.ItemTypeEnum itemTypeEnum, int tier,
                Equipment equipment, Hero hero, Func<ItemObject, bool> filter = null)
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

            if (upgradeMelee)
            {
                // We want to be left with only one melee weapon of the appropriate skill, of the highest tier, then we will 
                // try and upgrade it
                var highestSkill = MeleeSkills.OrderByDescending(s => adoptedHero.GetSkillValue(s)).First();

                var newWeapon = UpgradeWeapon(highestSkill, MeleeSkills, MeleeItems, EquipmentIndex.Weapon0, adoptedHero,
                    adoptedHero.BattleEquipment, targetTier);
                if (newWeapon != null)
                {
                    itemsPurchased.Add(newWeapon);
                }

                var shieldSlots = adoptedHero.BattleEquipment
                    .YieldWeaponSlots()
                    .Where(e => e.element.Item?.Type == ItemObject.ItemTypeEnum.Shield)
                    .ToList();

                if (highestSkill == DefaultSkills.OneHanded)
                {
                    var (element, index) =
                        !shieldSlots.Any() ? FindEmptyWeaponSlot(adoptedHero.BattleEquipment) : shieldSlots.First();
                    if (index == EquipmentIndex.None)
                        index = EquipmentIndex.Weapon1;

                    if (element.Item == null || element.Item.Tier < (ItemObject.ItemTiers) targetTier)
                    {
                        var shield = FindRandomTieredEquipment(DefaultSkills.OneHanded, targetTier, adoptedHero,
                            null, ItemObject.ItemTypeEnum.Shield);
                        if (shield != null)
                        {
                            adoptedHero.BattleEquipment[index] = new EquipmentElement(shield);
                            itemsPurchased.Add(shield);
                        }
                    }
                }
            }

            if (upgradeRanged)
            {
                // We want to be left with only one weapon of the appropriate skill, of the highest tier, then we will 
                // try and upgrade it
                var highestSkill = RangedSkills.OrderByDescending(s => adoptedHero.GetSkillValue(s)).First();

                var weapon = UpgradeWeapon(highestSkill, RangedSkills, RangedItems, EquipmentIndex.Weapon3, adoptedHero,
                    adoptedHero.BattleEquipment, targetTier);

                if (weapon?.Type == ItemObject.ItemTypeEnum.Thrown)
                {
                    // add more to free slots
                    var (_, index) = FindEmptyWeaponSlot(adoptedHero.BattleEquipment);
                    if (index != EquipmentIndex.None)
                    {
                        adoptedHero.BattleEquipment[index] = new EquipmentElement(weapon);
                    }
                }
                else if (weapon?.Type is ItemObject.ItemTypeEnum.Bow or ItemObject.ItemTypeEnum.Crossbow)
                {
                    var ammoType = ItemObject.GetAmmoTypeForItemType(weapon.Type);
                    var arrowSlots = adoptedHero.BattleEquipment
                        .YieldWeaponSlots()
                        .Where(e => e.element.Item?.Type == ammoType)
                        .ToList();
                    var (slot, index) = !arrowSlots.Any() ? FindEmptyWeaponSlot(adoptedHero.BattleEquipment) : arrowSlots.First();
                    if (index == EquipmentIndex.None)
                        index = EquipmentIndex.Weapon3;
                    if (slot.Item == null || slot.Item.Tier < (ItemObject.ItemTiers) targetTier)
                    {
                        var ammo = FindRandomTieredEquipment(null, targetTier, adoptedHero, null, ammoType);
                        if (ammo != null)
                        {
                            adoptedHero.BattleEquipment[index] = new EquipmentElement(ammo);
                            itemsPurchased.Add(ammo);
                        }
                    }
                }
            }

            if (upgradeArmor)
            {
                foreach (var (index, itemType) in ArmorIndexType)
                {
                    var newItem = UpgradeItemInSlot(index, itemType, targetTier, adoptedHero.BattleEquipment, adoptedHero);
                    if (newItem != null) itemsPurchased.Add(newItem);
                }
            }

            if (upgradeHorse)
            {
                var newHorse = UpgradeItemInSlot(EquipmentIndex.Horse, ItemObject.ItemTypeEnum.Horse, targetTier,
                    adoptedHero.BattleEquipment, adoptedHero);
                if (newHorse != null) itemsPurchased.Add(newHorse);
                var newHarness = UpgradeItemInSlot(EquipmentIndex.HorseHarness, ItemObject.ItemTypeEnum.HorseHarness,
                    targetTier, adoptedHero.BattleEquipment, adoptedHero);
                if (newHarness != null) itemsPurchased.Add(newHarness);
            }

            if (upgradeCivilian)
            {
                foreach (var (index, itemType) in ArmorIndexType)
                {
                    var newItem = UpgradeItemInSlot(index, itemType, targetTier, adoptedHero.CivilianEquipment, adoptedHero,
                        o => o.IsCivilian);
                    if (newItem != null) itemsPurchased.Add(newItem);
                }

                var upgradeSlot = adoptedHero.CivilianEquipment.YieldWeaponSlots().FirstOrDefault(s => !s.element.IsEmpty);
                if (upgradeSlot.element.IsEmpty)
                    upgradeSlot = FindEmptyWeaponSlot(adoptedHero.CivilianEquipment);

                UpgradeItemInSlot(upgradeSlot.index, ItemObject.ItemTypeEnum.OneHandedWeapon, targetTier,
                    adoptedHero.CivilianEquipment, adoptedHero);
            }

            return itemsPurchased;
        }
    }
}