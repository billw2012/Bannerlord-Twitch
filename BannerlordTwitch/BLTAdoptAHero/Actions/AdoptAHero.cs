using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Allows viewer to 'adopt' a hero in game -- the hero name will change to the viewers, and they can control it with further commands")]
    public class AdoptAHero : IRewardHandler, ICommandHandler
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
            [UsedImplicitly]
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

        private static string GetFullName(string name) => $"{name} {BLTAdoptAHeroModule.Tag}";

        internal static Hero GetAdoptedHero(string name)
        {
            var tagObject = new TextObject(BLTAdoptAHeroModule.Tag);
            var nameObject = new TextObject(name);
            return Campaign.Current?
                .AliveHeroes?
                .FirstOrDefault(h => h.Name?.Contains(tagObject) == true 
                                     && h.FirstName?.Contains(nameObject) == true 
                                     && h.FirstName?.ToString() == name);
        }

        Type IRewardHandler.RewardConfigType => typeof(Settings);
        void IRewardHandler.Enqueue(ReplyContext context, object config)
        {
            var hero = GetAdoptedHero(context.UserName);
            if (hero?.IsAlive == true)
            {
                ActionManager.NotifyCancelled(context, "You have already adopted a hero!");
                return;
            }
            var settings = (Settings)config;
            (bool success, string message) = ExecuteInternal(hero, context.Args, context.UserName, settings);
            if (success)
            {
                ActionManager.NotifyComplete(context, message);
            }
            else
            {
                ActionManager.NotifyCancelled(context, message);
            }
        }
        
        Type ICommandHandler.HandlerConfigType => typeof(Settings);
        void ICommandHandler.Execute(ReplyContext context, object config)
        {
            var hero = GetAdoptedHero(context.UserName);
            if (hero?.IsAlive == true)
            {
                ActionManager.SendReply(context, "You have already adopted a hero!");
                return;
            }

            var settings = (Settings)config;
            if (settings.MinSubscribedMonths > 0 && context.SubscribedMonthCount < settings.MinSubscribedMonths)
            {
                ActionManager.SendReply(context, $"You must be subscribed for at least {settings.MinSubscribedMonths} months to adopt a hero with this command!");
                return;
            }
            if(!context.IsSubscriber && settings.SubscriberOnly)
            {
                ActionManager.SendReply(context, $"You must be subscribed to adopt a hero with this command!");
                return;
            }
                
            var (_, message) = ExecuteInternal(hero, context.Args, context.UserName, settings);
            ActionManager.SendReply(context, message);
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

            if (hero?.IsAlive == false)
            {
                int count = Campaign.Current.DeadAndDisabledHeroes.Count(h =>
                    h.FirstName.Contains(userName) && h.FirstName.ToString() == userName);
                hero.FirstName = new TextObject(hero.FirstName + $" ({count})"); 
                Campaign.Current.EncyclopediaManager.BookmarksTracker.RemoveBookmarkFromItem(hero);
            }

            args = args?.Trim();
            var randomHero = string.IsNullOrEmpty(args)
                ? GetAvailableHeroes(settings).SelectRandom() 
                : Campaign.Current.AliveHeroes.FirstOrDefault(h => h.Name.Contains(args) && h.Name.ToString() == args);
            if (randomHero == null)
            {
                return (false, $"You can't adopt a hero: no available hero matching the requirements was found!");
            }
            
            string oldName = randomHero.Name.ToString();
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
            
            if(!Campaign.Current.EncyclopediaManager.BookmarksTracker.IsBookmarked(randomHero))
            {
                Campaign.Current.EncyclopediaManager.BookmarksTracker.AddBookmarkToItem(randomHero);
            }
            return (true, $"{oldName} is now known as {randomHero.Name}, they have {randomHero.Gold} gold!");
        }
    }
}