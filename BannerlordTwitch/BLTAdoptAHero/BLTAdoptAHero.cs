using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox;
using SandBox.Source.Missions.Handlers;
using StoryMode.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.Towns;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using YamlDotNet.Serialization;

#pragma warning disable 649

namespace BLTAdoptAHero
{
    internal static class HeroExtensions
    {
        internal static bool IsAdopted(this Hero hero) => hero.Name.Contains(BLTAdoptAHeroModule.Tag);
    }
    
    internal static class AgentExtensions
    {
        internal static bool IsAdopted(this Agent agent) => (agent.Character as CharacterObject)?.HeroObject?.IsAdopted() ?? false;
    }
    
    [UsedImplicitly]
    public class BLTAdoptAHeroModule : MBSubModuleBase
    {
        public const string Name = "BLTAdoptAHero";
        public const string Ver = "1.0.1";

        public BLTAdoptAHeroModule()
        {
            RewardManager.RegisterAll(typeof(BLTAdoptAHeroModule).Assembly);
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
                if ((victim?.IsAdopted() ?? false) || (killer?.IsAdopted() ?? false))
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

    [UsedImplicitly]
    [Description("Allows viewer to 'adopt' a hero in game -- the hero name will change to the viewers, and they can control it with further commands")]
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
            [Description("Allow noble heroes"), PropertyOrder(1)]
            public bool AllowNoble { get; set; }
            [Description("Allow wanderer heroes"), PropertyOrder(2)]
            public bool AllowWanderer { get; set; }
            [Description("Allow companions (not tested)"), PropertyOrder(3)]
            public bool AllowPlayerCompanion { get; set; }
            [Description("Only allow heroes from same faction as player"), PropertyOrder(4)]
            public bool OnlySameFaction { get; set; }
            [Description("Only allow viewer to adopt another hero if theirs is dead"), PropertyOrder(5)]
            public bool AllowNewAdoptionOnDeath { get; set; }
            [Description("Only subscribers can adopt"), PropertyOrder(6)]
            public bool SubscriberOnly { get; set; }
            [Description("Only viewers who have been subscribers for at least this many months can adopt, ignored if not specified"), DefaultValue(null), PropertyOrder(7)]
            public int? MinSubscribedMonths { get; set; }
            [Description("Gold the adopted hero will start with, if you don't specify then they get the heroes existing gold"), DefaultValue(null), PropertyOrder(8)]
            public int? StartingGold { get; set; }
            [Description("Equipment tier the adopted hero will start with, if you don't specify then they get the heroes existing equipment"), DefaultValue(null), PropertyOrder(9)]
            public int? StartingEquipmentTier { get; set; }
            [Description("Whether the hero will start with a horse, only applies if StartingEquipmentTier is specified"), PropertyOrder(10)]
            public bool StartWithHorse { get; set; }
            [Description("Whether the hero will start with armor, only applies if StartingEquipmentTier is specified"), PropertyOrder(11)]
            public bool StartWithArmor { get; set; }

            [Description("Starting equipment definition")]
            public class StartingEquipmentDef
            {
                public int Tier { get; set; }
                public bool Horse { get; set; }
                public bool Armor { get; set; }
            }

            [Description("Starting equipment"), PropertyOrder(12), DefaultValue(null)]
            public StartingEquipmentDef StartingEquipment { get; set; }
        }

        private static IEnumerable<Hero> GetAvailableHeroes(Settings settings)
        {
            var tagText = new TextObject(BLTAdoptAHeroModule.Tag);
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

        internal static string GetFullName(string name) => $"{name} {BLTAdoptAHeroModule.Tag}";

        internal static Hero GetAdoptedHero(string name)
        {
            var tagObject = new TextObject(BLTAdoptAHeroModule.Tag);
            var nameObject = new TextObject(name);
            return Campaign.Current?
                .AliveHeroes?
                .FirstOrDefault(h => (h.Name?.Contains(tagObject) ?? false) 
                                     && (h.FirstName?.Contains(nameObject) ?? false) 
                                     && h.FirstName?.ToString() == name);
        }

        Type IAction.ActionConfigType => typeof(Settings);
        void IAction.Enqueue(ReplyContext context, object config)
        {
            var hero = GetAdoptedHero(context.UserName);
            if (hero?.IsAlive == true)
            {
                RewardManager.NotifyCancelled(context, "You have already adopted a hero!");
                return;
            }
            var settings = (Settings)config;
            var (success, message) = ExecuteInternal(hero, context.Args, context.UserName, settings);
            if (success)
            {
                RewardManager.NotifyComplete(context, message);
            }
            else
            {
                RewardManager.NotifyCancelled(context, message);
            }
        }
        
        Type ICommandHandler.HandlerConfigType => typeof(Settings);
        void ICommandHandler.Execute(ReplyContext context, object config)
        {
            var hero = GetAdoptedHero(context.UserName);
            if (hero?.IsAlive == true)
            {
                RewardManager.SendReply(context, "You have already adopted a hero!");
                return;
            }

            var settings = (Settings)config;
            if (settings.MinSubscribedMonths > 0 && context.SubscribedMonthCount < settings.MinSubscribedMonths)
            {
                RewardManager.SendReply(context, $"You must be subscribed for at least {settings.MinSubscribedMonths} months to adopt a hero with this command!");
                return;
            }
            if(!context.IsSubscriber && settings.SubscriberOnly)
            {
                RewardManager.SendReply(context, $"You must be subscribed to adopt a hero with this command!");
                return;
            }
                
            var (_, message) = ExecuteInternal(hero, context.Args, context.UserName, settings);
            RewardManager.SendReply(context, message);
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
    [Description("Will write various hero stats to chat")]
    internal class HeroInfoCommand : ICommandHandler
    {
        private class Settings
        {
            [Description("Show general info: gold, health, location, age"), PropertyOrder(1)]
            public bool ShowGeneral { get; set; }
            [Description("Shows skills (and focuse values) above the specified MinSkillToShow value"), PropertyOrder(2)]
            public bool ShowTopSkills { get; set; }
            [Description("If ShowTopSkills is specified, this defines what skills are shown"), PropertyOrder(3)]
            public int MinSkillToShow { get; set; }
            [Description("Shows all hero attributes"), PropertyOrder(4)]
            public bool ShowAttributes { get; set; }
            [Description("Shows the battle and civilian equipment of the hero"), PropertyOrder(5)]
            public bool ShowEquipment { get; set; }
        }
        
        // One Handed, Two Handed, Polearm, Bow, Crossbow, Throwing, Riding, Athletics, Smithing
        // Scouting, Tactics, Roguery, Charm, Leadership, Trade, Steward, Medicine, Engineering

        Type ICommandHandler.HandlerConfigType => typeof(Settings);
        void ICommandHandler.Execute(ReplyContext context, object config)
        {
            var settings = config as Settings ?? new Settings();
            var adoptedHero = AdoptAHero.GetAdoptedHero(context.UserName);
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

            RewardManager.SendReply(context, infoStrings.ToArray());
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
    [Description("Gives gold to the adopted hero")]
    internal class AddGoldToHero : IAction
    {
        private class Settings
        {
            [Description("How much gold to give the adopted hero")]
            public int Amount { get; set; }
        }

        Type IAction.ActionConfigType => typeof(Settings);
        void IAction.Enqueue(ReplyContext context, object config)
        {
            var settings = (Settings)config;
            var adoptedHero = AdoptAHero.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                RewardManager.NotifyCancelled(context, Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }

            adoptedHero.Gold += settings.Amount;
            RewardManager.NotifyComplete(context, $"+{settings.Amount} gold, you now have {adoptedHero.Gold}!");
        }
    }

    [Flags]
    internal enum Skills
    {
        None,
        All,
        
        Melee,
        OneHanded, TwoHanded, Polearm,
        
        Ranged,
        Bow, Throwing, Crossbow,
        
        Movement,
        Riding, Athletics,
        
        Support,
        Scouting, Trade, Steward, Medicine, Engineering,
        
        Personal,
        Crafting, Tactics, Roguery, Charm, Leadership,
    }

    internal static class SkillGroup
    {
        public static Skills[] ExpandSkills(Skills skills)
        {
            switch (skills)
            {
                case Skills.Melee: return new[] { Skills.OneHanded, Skills.TwoHanded, Skills.Polearm };
                case Skills.Ranged: return new[] { Skills.Bow , Skills.Throwing , Skills.Crossbow };
                case Skills.Movement: return new[] { Skills.Riding , Skills.Athletics };
                case Skills.Support: return new[] { Skills.Scouting , Skills.Trade , Skills.Steward , Skills.Medicine , Skills.Engineering };
                case Skills.Personal: return new[] { Skills.Crafting ,Skills.Tactics , Skills.Roguery , Skills.Charm ,  Skills.Leadership };
                case Skills.All: return new[] {Skills.OneHanded , Skills.TwoHanded , Skills.Polearm , Skills.Bow , Skills.Throwing ,
                    Skills.Crossbow , Skills.Riding , Skills.Athletics , Skills.Crafting , Skills.Tactics , 
                    Skills.Scouting , Skills.Roguery , Skills.Charm , Skills.Trade , Skills.Steward ,
                    Skills.Medicine , Skills.Engineering , Skills.Leadership};
                case Skills.None: return new Skills[] { };
                default:
                    return new[] { skills };
            }
        }

        public static string[] SkillsToStrings(Skills skills)
        {
            return ExpandSkills(skills).Select(s => s.ToString()).ToArray();
        }
    }

    internal abstract class ImproveAdoptedHero : ActionAndHandlerBase
    {
        protected class SettingsBase
        {
            [Description("Lower bound of amount to improve"), PropertyOrder(11)]
            public int AmountLow { get; set; }
            [Description("Upper bound of amount to improve"), PropertyOrder(12)]
            public int AmountHigh { get; set; }
            [Description("Gold that will be taken from the hero"), PropertyOrder(13)]
            public int GoldCost { get; set; }
        }

        // protected override Type ConfigType => typeof(SettingsBase);

        protected override void ExecuteInternal(ReplyContext context, object config,
            Action<string> onSuccess,
            Action<string> onFailure) 
        {
            var settings = (SettingsBase)config;
            var adoptedHero = AdoptAHero.GetAdoptedHero(context.UserName);
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
            var (success, description) = Improve(context.UserName, adoptedHero, amount, settings);
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

        private static string[] EnumFlagsToArray<T>(T flags) =>
            flags.ToString().Split(',').Select(s => s.Trim()).ToArray();
        private static string[][] SkillGroups =
        {
            SkillGroup.SkillsToStrings(Skills.Melee),
            SkillGroup.SkillsToStrings(Skills.Ranged),
            SkillGroup.SkillsToStrings(Skills.Support),
            SkillGroup.SkillsToStrings(Skills.Movement),
            SkillGroup.SkillsToStrings(Skills.Personal),
        };
        
        protected static SkillObject GetSkill(Hero hero, Skills skills, bool random, bool auto, Func<SkillObject, bool> predicate = null)
        {
            IEnumerable<SkillObject> GetSkills(IEnumerable<string> sk) => sk
                .Select(sn => DefaultSkills.GetAllSkills().FirstOrDefault(so =>
                    string.Equals(so.StringId, sn, StringComparison.CurrentCultureIgnoreCase)))
                .Where(predicate ?? (s => true));
                
            IEnumerable<SkillObject> selectedSkills;
            if (auto)
            {
                // We will select automatically which skill from groups
                selectedSkills = SkillGroups
                    .Select(GetSkills)
                    .Where(g => g.Any())
                    .SelectRandom();
            }
            else
            {
                selectedSkills = GetSkills(SkillGroup.SkillsToStrings(skills));
            }

            return random 
                ? selectedSkills?.SelectRandom() 
                : selectedSkills?.OrderByDescending(hero.GetSkillValue).FirstOrDefault();
        }
    }

    [UsedImplicitly]
    [Description("Improve adopted heroes skills")]
    internal class SkillXP : ImproveAdoptedHero
    {
        protected class SkillXPSettings : SettingsBase
        {
            [Description("What to improve"), PropertyOrder(1)]
            public Skills Skills { get; set; }
            [Description("Improve a random skill from the Skills specified, rather than the best one"), PropertyOrder(2)]
            public bool Random { get; set; }
            [Description("If this is specified then the best skill from a random skill group will be improved, Skills list is ignored. Groups are melee (One Handed, Two Handed, Polearm), ranged (Bow, Crossbow, Throwing), support (Smithing, Scouting, Trade, Steward, Engineering), movement (Riding, Athletics), personal (Tactics, Roguery, Charm, Leadership)"), PropertyOrder(3)]
            public bool Auto { get; set; }
        }
        
        protected override Type ConfigType => typeof(SkillXPSettings);

        protected override (bool success, string description) Improve(string userName,
            Hero adoptedHero, int amount, SettingsBase baseSettings)
        {
            var settings = (SkillXPSettings) baseSettings;
            return ImproveSkill(adoptedHero, amount, settings.Skills, settings.Random, settings.Auto);
        }

        public static (bool success, string description) ImproveSkill(Hero hero, int amount, Skills skills, bool random, bool auto)
        {
            var skill = GetSkill(hero, skills, random, auto);
            if (skill == null) return (false, $"{skills} is not a valid skill name");
            float prevSkill = hero.HeroDeveloper.GetPropertyValue(skill);
            int prevLevel = hero.GetSkillValue(skill);
            hero.HeroDeveloper.AddSkillXp(skill, amount);
            // Force this immediately instead of waiting for the daily campaign tick
            CharacterDevelopmentCampaignBehaivor.DevelopCharacterStats(hero);
            float realGainedXp = hero.HeroDeveloper.GetPropertyValue(skill) - prevSkill;
            int newLevel = hero.GetSkillValue(skill);
            int gainedLevels = newLevel - prevLevel;
            return realGainedXp < 1f
                ? (false, $"{skill.Name} capped, get more focus points")
                : gainedLevels > 0
                    ? (true, $"+{gainedLevels} lvl in {skill.Name} ({newLevel})")
                    : (true,
                        $"+{realGainedXp:0}xp in {skill.Name} ({hero.GetSkillValue(skill)}xp)");
        }
    }

    [UsedImplicitly]
    [Description("Add focus points to heroes skills")]
    internal class FocusPoints : ImproveAdoptedHero
    {
        protected class FocusPointsSettings : SettingsBase
        {
            [Description("What skill to add focus to"), PropertyOrder(1)]
            public Skills Skills { get; set; }
            [Description("Add focus to a random skill, from the Skills specified, rather than the best one."), PropertyOrder(2)]
            public bool Random { get; set; }
            [Description("If this is specified then the best skill from a random skill group will have focus added, <code>Skills</code> list is ignored. Groups are melee (One Handed, Two Handed, Polearm), ranged (Bow, Crossbow, Throwing), support (Smithing, Scouting, Trade, Steward, Engineering), movement (Riding, Athletics), personal (Tactics, Roguery, Charm, Leadership)"), PropertyOrder(3)]
            public bool Auto { get; set; }
        }
        
        protected override Type ConfigType => typeof(FocusPointsSettings);
        
        protected override (bool success, string description) Improve(string userName,
            Hero adoptedHero, int amount, SettingsBase baseSettings)
        {
            var settings = (FocusPointsSettings) baseSettings;

            return FocusSkill(adoptedHero, amount, settings.Skills, settings.Random, settings.Auto);
        }

        public static (bool success, string description) FocusSkill(Hero adoptedHero, int amount, Skills skills, bool random, bool auto)
        {
            var skill = GetSkill(adoptedHero, skills, random, auto, s => adoptedHero.HeroDeveloper.GetFocus(s) < 5);

            if (skill == null)
            {
                return (false, $"Couldn't find a valid skill to add focus points to!");
            }

            amount = Math.Min(amount, 5 - adoptedHero.HeroDeveloper.GetFocus(skill));
            adoptedHero.HeroDeveloper.AddFocus(skill, amount, checkUnspentFocusPoints: false);
            return (true,
                $"You have gained {amount} focus point{(amount > 1 ? "s" : "")} in {skill}, you now have {adoptedHero.HeroDeveloper.GetFocus(skill)}!");
        }
    }
    
    public enum CharacterAttributes
    {
        Vigor = 0,
        Control = 1,
        Endurance = 2,
        Cunning = 3,
        Social = 4,
        Intelligence = 5,
        Random = 6,
    }

    [UsedImplicitly]
    [Description("Improve adopted heroes attribute points")]
    internal class AttributePoints : ImproveAdoptedHero
    {
        protected class AttributePointsSettings : SettingsBase
        {
            [Description("Which attribute to improve (specify one only)"), PropertyOrder(1)]
            public CharacterAttributes Attribute { get; set; } = CharacterAttributes.Random;
        }
        
        protected override Type ConfigType => typeof(AttributePointsSettings);
        
        protected override (bool success, string description) Improve(string userName,
            Hero adoptedHero, int amount, SettingsBase baseSettings)
        {
            var settings = (AttributePointsSettings) baseSettings;

            return IncreaseAttribute(adoptedHero, amount, settings.Attribute);
        }

        private static (bool success, string description) IncreaseAttribute(Hero adoptedHero, int amount, CharacterAttributes attribToIncrease)
        {
            // Get attributes that can be buffed
            var improvableAttributes = AdoptAHero.CharAttributes
                .Select(c => c.val)
                .Where(a => adoptedHero.GetAttributeValue(a) < 10)
                .ToList();

            if (!improvableAttributes.Any())
            {
                return (false, $"Couldn't improve any attributes, they are all at max level!");
            }

            var attribute = attribToIncrease != CharacterAttributes.Random
                ? (CharacterAttributesEnum) attribToIncrease
                : improvableAttributes.SelectRandom();

            if (!improvableAttributes.Contains(attribute))
            {
                return (false, $"Couldn't improve {attribute} attributes, it is already at max level!");
            }

            amount = Math.Min(amount, 10 - adoptedHero.GetAttributeValue(attribute));
            adoptedHero.HeroDeveloper.AddAttribute(attribute, amount, checkUnspentPoints: false);
            return (true, $"You have gained {amount} point{(amount > 1 ? "s" : "")} in {attribute}, you now have {adoptedHero.GetAttributeValue(attribute)}!");
        }
    }

    [UsedImplicitly]
    [Description("Spawns the adopted hero into the current active mission")]
    internal class SummonHero : ActionAndHandlerBase
    {
        [CategoryOrder("Allowed Missions", 0)]
        [CategoryOrder("General", 1)]
        [CategoryOrder("Effects", 2)]
        private class Settings
        {
            [Category("Allowed Missions"), Description("Can summon for normal field battles between parties"), PropertyOrder(1)]
            public bool AllowFieldBattle { get; set; }
            [Category("Allowed Missions"), Description("Can summon in village battles"), PropertyOrder(2)]
            public bool AllowVillageBattle { get; set; }
            [Category("Allowed Missions"), Description("Can summon in sieges"), PropertyOrder(3)]
            public bool AllowSiegeBattle { get; set; }
            [Category("Allowed Missions"), Description("This includes walking about village/town/dungeon/keep"), PropertyOrder(4)]
            public bool AllowFriendlyMission { get; set; }
            [Category("Allowed Missions"), Description("Can summon in the practice arena"), PropertyOrder(5)]
            public bool AllowArena { get; set; }
            [Category("Allowed Missions"), Description("NOT IMPLEMENTED YET Can summon in tournaments"), PropertyOrder(6)]
            public bool AllowTournament { get; set; }
            [Category("Allowed Missions"), Description("Can summon in the hideout missions"), PropertyOrder(7)]
            public bool AllowHideOut { get; set; }
            [Category("General"), Description("Whether the hero is on the player or enemy side"), PropertyOrder(1)]
            public bool OnPlayerSide { get; set; }
            [Category("General"), Description("Maximum number of summons that can be active at the same time (i.e. max alive adopted heroes that can be in the mission)"), PropertyOrder(2)]
            public int? MaxSimultaneousSummons { get; set; }
            [Category("General"), Description("Whether the summoned hero is allowed to die"), PropertyOrder(3)]
            public bool AllowDeath { get; set; }
            [Category("General"), Description("Whether the summoned hero will always start with full health"), PropertyOrder(3)]
            public bool StartWithFullHealth { get; set; }
            [Category("General"), Description("Gold cost to summon"), PropertyOrder(1)]
            public int GoldCost { get; set; }
            [Category("Effects"), Description("Gold won if the heroes side wins"), PropertyOrder(2)]
            public int WinGold { get; set; }
            [Category("Effects"), Description("Gold lost if the heroes side loses"), PropertyOrder(3)]
            public int LoseGold { get; set; }
            [Category("Effects"), Description("Gold the hero gets for every kill"), PropertyOrder(4)]
            public int GoldPerKill { get; set; }
            [Category("Effects"), Description("XP the hero gets for every kill. It will be distributed using the Auto behavior of the SkillXP action: randomly between the top skills from each skill group (melee, ranged, movement, support, personal)."), PropertyOrder(5)]
            public int XPPerKill { get; set; }
            [Category("Effects"), Description("HP the hero gets for every kill"), PropertyOrder(6)]
            public int HealPerKill { get; set; }
            [Category("Effects"), Description("HP the hero gets every second they are alive in the mission"), PropertyOrder(7)]
            public float HealPerSecond { get; set; }
            [Category("Effects"), Description("Multiplier applied to (positive) effects for subscribers"), PropertyOrder(8)]
            public float SubBoost { get; set; } = 1;
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

        private class BLTMissionBehavior : AutoMissionBehavior<BLTMissionBehavior>
        {
            public delegate void MissionOverDelegate();
            public delegate void MissionModeChangeDelegate(MissionMode oldMode, MissionMode newMode, bool atStart);
            public delegate void MissionResetDelegate();
            public delegate void GotAKillDelegate(Agent killed, AgentState agentState);
            public delegate void GotKilledDelegate(Agent killer, AgentState agentState);
            public delegate void MissionTickDelegate(float dt);
            
            public class Listeners
            {
                public Hero hero;
                public Agent agent;
                public MissionOverDelegate onMissionOver;
                public MissionModeChangeDelegate onModeChange;
                public MissionResetDelegate onMissionReset;
                public GotAKillDelegate onGotAKill;
                public GotKilledDelegate onGotKilled;
                public MissionTickDelegate onMissionTick;
                public MissionTickDelegate onSlowTick;
            }

            private readonly List<Listeners> listeners = new();

            public void AddListeners(Hero hero, Agent agent,
                    MissionOverDelegate onMissionOver = null,
                    MissionModeChangeDelegate onModeChange = null,
                    MissionResetDelegate onMissionReset = null,
                    GotAKillDelegate onGotAKill = null,
                    GotKilledDelegate onGotKilled = null,
                    MissionTickDelegate onMissionTick = null,
                    MissionTickDelegate onSlowTick = null
                )
            {
                RemoveListeners(hero);
                listeners.Add(new Listeners
                {
                    hero = hero,
                    agent = agent,
                    onMissionOver = onMissionOver,
                    onModeChange = onModeChange,
                    onMissionReset = onMissionReset,
                    onGotAKill = onGotAKill,
                    onGotKilled = onGotKilled,
                    onMissionTick = onMissionTick,
                    onSlowTick = onSlowTick,
                });
            }

            public void RemoveListeners(Hero hero)
            {
                listeners.RemoveAll(l => l.hero == hero);
            }

            public override void OnAgentRemoved(Agent killedAgent, Agent killerAgent, AgentState agentState, KillingBlow blow)
            {
                ForAgent(killedAgent, l => l.onGotKilled?.Invoke(killerAgent, agentState));
                ForAgent(killerAgent, l => l.onGotAKill?.Invoke(killedAgent, agentState));
                base.OnAgentRemoved(killedAgent, killerAgent, agentState, blow);
            }

            protected override void OnEndMission()
            {
                ForAll(listeners => listeners.onMissionOver?.Invoke());
                base.OnEndMission();
            }

            private const float SlowTickDuration = 2;
            private float slowTick = 0;
            
            public override void OnMissionTick(float dt)
            {
                slowTick += dt;
                if (slowTick > 2)
                {
                    slowTick -= 2;
                    ForAll(listeners => listeners.onSlowTick?.Invoke(2));
                }
                ForAll(listeners => listeners.onMissionTick?.Invoke(dt));
                base.OnMissionTick(dt);
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
                ForAll(l => l.onModeChange?.Invoke(oldMissionMode, Mission.Current.Mode, atStart));
                base.OnMissionModeChange(oldMissionMode, atStart);
            }

            private Hero FindHero(Agent agent) => listeners.FirstOrDefault(l => l.agent == agent)?.hero;
            
            private void ForAll(Action<Listeners> action)
            {
                foreach (var listener in listeners)
                {
                    action(listener);
                }
            }

            private void ForAgent(Agent agent, Action<Listeners> action)
            {
                foreach (var listener in listeners.Where(l => l.agent == agent))
                {
                    action(listener);
                }
            }
        }

        private class BLTRemoveAgentsBehavior : AutoMissionBehavior<BLTRemoveAgentsBehavior>
        {
            private readonly List<Hero> heroesAdded = new();
 
            public void Add(Hero hero)
            {
                heroesAdded.Add(hero);
            }

            private void RemoveHeroes()
            {
                foreach (var hero in heroesAdded)
                {
                    LocationComplex.Current?.RemoveCharacterIfExists(hero);
                    if(CampaignMission.Current?.Location?.ContainsCharacter(hero) ?? false)
                        CampaignMission.Current.Location.RemoveCharacter(hero);
                }
                heroesAdded.Clear();
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
        }

        protected override void ExecuteInternal(ReplyContext context, object config,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            static string KillStateVerb(AgentState state) =>
                state switch
                {
                    AgentState.Routed => "routed",
                    AgentState.Unconscious => "knocked out",
                    AgentState.Killed => "killed",
                    AgentState.Deleted => "deleted",
                    _ => "fondled"
                };

            var settings = (Settings) config;

            var adoptedHero = AdoptAHero.GetAdoptedHero(context.UserName);
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

            Agent agent;
            
            if (CampaignMission.Current.Location != null)
            {
                var locationCharacter = LocationCharacter.CreateBodyguardHero(adoptedHero,
                    MobileParty.MainParty,
                    SandBoxManager.Instance.AgentBehaviorManager.AddBodyguardBehaviors);
                
                var missionAgentHandler = Mission.Current.GetMissionBehaviour<MissionAgentHandler>();
                var worldFrame = missionAgentHandler.Mission.MainAgent.GetWorldFrame();
                worldFrame.Origin.SetVec2(worldFrame.Origin.AsVec2 + (worldFrame.Rotation.f * 10f + worldFrame.Rotation.s).AsVec2);
                
                CampaignMission.Current.Location.AddCharacter(locationCharacter);
                agent = SpawnWanderingAgent(missionAgentHandler, locationCharacter, worldFrame.ToGroundMatrixFrame(), false, true); 

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
                                    RewardManager.SendReply(context, $@"You won {settings.WinGold} gold!");
                                }
                                else if(settings.LoseGold > 0)
                                {
                                    Hero.MainHero.ChangeHeroGold(settings.LoseGold);
                                    adoptedHero.ChangeHeroGold(-settings.LoseGold);
                                    RewardManager.SendReply(context, $@"You lost {settings.LoseGold + settings.GoldCost} gold!");
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
                
                BLTRemoveAgentsBehavior.Current.Add(adoptedHero);
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

                agent = Mission.Current.SpawnTroop(
                    new PartyAgentOrigin(party, adoptedHero.CharacterObject, alwaysWounded: !settings.AllowDeath),
                    isPlayerSide: settings.OnPlayerSide,
                    hasFormation: true,
                    spawnWithHorse: adoptedHero.CharacterObject.HasMount() && Mission.Current.Mode != MissionMode.Stealth,
                    isReinforcement: true,
                    enforceSpawningOnInitialPoint: false,
                    formationTroopCount: 1,
                    formationTroopIndex: 8,
                    isAlarmed: true,
                    wieldInitialWeapons: true);


                float actualBoost = context.IsSubscriber ? settings.SubBoost : 1;
                BLTMissionBehavior.Current.AddListeners(adoptedHero, agent, 
                    onSlowTick: dt =>
                    {
                        if (settings.HealPerSecond != 0)
                        {
                            agent.Health = Math.Min(agent.HealthLimit, agent.Health + settings.HealPerSecond * dt * actualBoost);
                        }
                    },
                    onMissionOver: () => 
                    {
                        if (Mission.Current.MissionResult != null)
                        {
                            if (settings.OnPlayerSide == Mission.Current.MissionResult.PlayerVictory)
                            {
                                // User gets their gold back also
                                adoptedHero.ChangeHeroGold((int) (settings.WinGold * actualBoost + settings.GoldCost));
                                RewardManager.SendReply(context, $@"You won {settings.WinGold} gold!");
                            }
                            else if(settings.LoseGold > 0)
                            {
                                adoptedHero.ChangeHeroGold(-settings.LoseGold);
                                RewardManager.SendReply(context, $@"You lost {settings.LoseGold + settings.GoldCost} gold!");
                            }
                        }
                    },
                    onGotAKill: (killed, state) =>
                    {
                        if (settings.GoldPerKill != 0)
                        {
                            int gold = (int) (settings.GoldPerKill * actualBoost);
                            adoptedHero.ChangeHeroGold(gold);
                            Log.LogFeedBattle($"{adoptedHero.FirstName}: +{gold} gold");
                        }

                        if (settings.HealPerKill != 0)
                        {
                            float prevHealth = agent.Health;
                            agent.Health = Math.Min(agent.HealthLimit, agent.Health + settings.HealPerKill * actualBoost);
                            float healthDiff = agent.Health - prevHealth;
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

                        if (killed != null)
                        {
                            Log.LogFeedBattle($"{adoptedHero.FirstName} {KillStateVerb(state)} {killed.Name}");
                        }
                    },
                    onGotKilled: (killer, state) =>
                    {
                        Log.LogFeedBattle(killer != null
                            ? $"{adoptedHero.FirstName} was {KillStateVerb(state)} by {killer.Name}"
                            : $"{adoptedHero.FirstName} was {KillStateVerb(state)}");
                    }
                    );
            }
            
            if (settings.StartWithFullHealth)
            {
                agent.Health = agent.HealthLimit;
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
                    "It's stabbing time! For you.",
                    "It's nothing personal!",
                };
            if (InSiegeMission() && settings.OnPlayerSide) messages.Add($"Don't send me up the siege tower, its confusing!");
            InformationManager.AddQuickInformation(new TextObject(!string.IsNullOrEmpty(context.Args) ? context.Args : messages.SelectRandom()), 1000,
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
    [Description("Improve adopted heroes equipment")]
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
            [Description("Improve armor"), PropertyOrder(1)]
            public bool Armor { get; set; }
            [Description("Improve melee weapons (one handled, two handed, polearm). The one the player has the highest skill in will be selected."), PropertyOrder(2)]
            public bool Melee { get; set; }
            [Description("Improve ranged weapons (bow, crossbow, throwing). The one the player has the highest skill in will be selected."), PropertyOrder(3)]
            public bool Ranged { get; set; }
            [Description("Improve the heroes horse (if they can ride a better one)."), PropertyOrder(4)]
            public bool Horse { get; set; }
            [Description("Improve the heroes civilian equipment."), PropertyOrder(5)]
            public bool Civilian { get; set; }
            [Description("Allow improvement of adopted heroes who are also companions of the player."), PropertyOrder(6)]
            public bool AllowCompanionUpgrade { get; set; }
            [Description("Tier to upgrade to (0 to 5). Anything better than this tier will be left alone, viewer will be refunded if nothing could be upgraded. Not compatible with Upgrade."), PropertyOrder(7)]
            public int? Tier { get; set; } // 0 to 5
            [Description("Upgrade to the next tier from the current one, viewer will be refunded if nothing could be upgraded. Not compatible with Tier."), PropertyOrder(8)]
            public bool Upgrade { get; set; }
            [Description("Gold cost to the adopted hero"), PropertyOrder(9)]
            public int GoldCost { get; set; }
            [Description("Whether to multiply the cost by the current tier"), PropertyOrder(10)]
            public bool MultiplyCostByCurrentTier { get; set; }
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

        protected override void ExecuteInternal(ReplyContext context, object config,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            var settings = (Settings)config;
            var adoptedHero = AdoptAHero.GetAdoptedHero(context.UserName);
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
            
            //string itemsStr = string.Join(", ", itemsPurchased.Select(i => i.Name.ToString()));
            // $"You purchased these items: {itemsStr}!"
            
            adoptedHero.Gold -= cost;
            onSuccess($"Equip Tier {targetTier}");
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
                    adoptedHero.BattleEquipment, adoptedHero, h => h.HorseComponent?.IsMount ?? false );
                if (newHorse != null) itemsPurchased.Add(newHorse);
                
                var horse = adoptedHero.BattleEquipment[EquipmentIndex.Horse];
                if (!horse.IsEmpty)
                {
                    // ItemObject.All.Where(i => (i.HorseComponent?.IsMount ?? false) && i.StringId.Contains("camel")).ToArray()
                    bool isCamel = horse.Item.StringId.Contains("camel");
                    var newHarness = UpgradeItemInSlot(EquipmentIndex.HorseHarness,
                        ItemObject.ItemTypeEnum.HorseHarness,
                        targetTier, adoptedHero.BattleEquipment, adoptedHero,
                        h => isCamel && h.StringId.Contains("camel") || !isCamel && !h.StringId.Contains("camel"));
                    if (newHarness != null) itemsPurchased.Add(newHarness);
                }
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