using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using Helpers;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.Towns;
using TaleWorlds.Core;
using TaleWorlds.Library;
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

        [CategoryOrder("General", 0)]
        [CategoryOrder("Limits", 1)]
        [CategoryOrder("Initialization", 1)]
        private class Settings
        {
            [Category("General"), Description("Create a new hero instead of adopting an existing one"), PropertyOrder(0)]
            public bool CreateNew { get; set; }
            [Category("Limits"), Description("Allow noble heroes (if CreateNew is false)"), PropertyOrder(1)]
            public bool AllowNoble { get; set; }
            [Category("Limits"), Description("Allow wanderer heroes (if CreateNew is false)"), PropertyOrder(2)]
            public bool AllowWanderer { get; set; }
            [Category("Limits"), Description("Allow companions (not tested, if CreateNew is false)"), PropertyOrder(3)]
            public bool AllowPlayerCompanion { get; set; }
            [Category("Limits"), Description("Only allow heroes from same faction as player"), PropertyOrder(4)]
            public bool OnlySameFaction { get; set; }
            [Category("Limits"), Description("Only allow viewer to adopt another hero if theirs is dead"), PropertyOrder(5)]
            public bool AllowNewAdoptionOnDeath { get; set; }
            [Category("Limits"), Description("Only subscribers can adopt"), PropertyOrder(6)]
            public bool SubscriberOnly { get; set; }
            [Category("Limits"), Description("Only viewers who have been subscribers for at least this many months can adopt, ignored if not specified"), DefaultValue(null), PropertyOrder(7)]
            public int? MinSubscribedMonths { get; set; }
            [Category("Initialization"), Description("Gold the adopted hero will start with"), DefaultValue(null), PropertyOrder(1)]
            public int StartingGold { get; set; }

            public class SkillDef
            {
                [Description("The skill or skill group"), PropertyOrder(1)]
                public Skills Skill { get; set; }
                [Description("The min level it should be (actual value will be randomly selected between min and max, valid values are 0 to 300)"), PropertyOrder(2)]
                public int MinLevel { get; set; }
                [Description("The max level it should be (actual value will be randomly selected between min and max, valid values are 0 to 300)"), PropertyOrder(3)]
                public int MaxLevel { get; set; }

                public override string ToString()
                {
                    return $"{Skill} {MinLevel} - {MaxLevel}";
                }
            }
            
            [Category("Initialization"), Description("Starting skills, if empty then default skills of the adopted hero will be left in tact"), DefaultValue(null), PropertyOrder(1)]
            public List<SkillDef> StartingSkills { get; set; }
            
            [Category("Initialization"), Description("Equipment tier the adopted hero will start with, if you don't specify then they get the heroes existing equipment"), DefaultValue(null), PropertyOrder(2)]
            public int? StartingEquipmentTier { get; set; }
            [Category("Initialization"), Description("Whether the hero will start with a horse, only applies if StartingEquipmentTier is specified"), PropertyOrder(3)]
            public bool StartWithHorse { get; set; }
            [Category("Initialization"), Description("Whether the hero will start with armor, only applies if StartingEquipmentTier is specified"), PropertyOrder(4)]
            public bool StartWithArmor { get; set; }
        }

        Type IRewardHandler.RewardConfigType => typeof(Settings);
        void IRewardHandler.Enqueue(ReplyContext context, object config)
        {
            var hero = BLTAdoptAHeroCampaignBehavior.GetAdoptedHero(context.UserName);
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
            var hero = BLTAdoptAHeroCampaignBehavior.GetAdoptedHero(context.UserName);
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

        private static (bool success, string message) ExecuteInternal(Hero existingHero, string args, string userName, Settings settings)
        {
            if (Campaign.Current == null)
            {
                return (false, AdoptAHero.NotStartedMessage);
            }
            if (existingHero?.IsAlive == false && !settings.AllowNewAdoptionOnDeath)
            {
                return (false, $"Your hero died, and you may not adopt another!");
            }

            if (existingHero?.IsAlive == false)
            {
                int count = Campaign.Current.DeadAndDisabledHeroes.Count(h =>
                    h.FirstName.Contains(userName) && h.FirstName.ToString() == userName);
                existingHero.FirstName = new TextObject(existingHero.FirstName + $" ({count})"); 
                Campaign.Current.EncyclopediaManager.BookmarksTracker.RemoveBookmarkFromItem(existingHero);
            }

            args = args?.Trim();

            Hero newHero;
            if (settings.CreateNew)
            {
                newHero = HeroCreator.CreateHeroAtOccupation(Occupation.Wanderer);
            }
            else
            {
                newHero = string.IsNullOrEmpty(args)
                    ? BLTAdoptAHeroCampaignBehavior.GetAvailableHeroes(h =>
                        (settings.AllowPlayerCompanion && h.IsPlayerCompanion
                         || settings.AllowNoble && h.IsNoble
                         || settings.AllowWanderer && h.IsWanderer)
                        && (!settings.OnlySameFaction 
                            || Clan.PlayerClan?.MapFaction != null 
                            && Clan.PlayerClan?.MapFaction == h.Clan?.MapFaction))
                        .SelectRandom()
                    : Campaign.Current.AliveHeroes.FirstOrDefault(h =>
                        h.Name.Contains(args) && h.Name.ToString() == args);
            }
            if (newHero == null)
            {
                return (false, $"You can't adopt a hero: no available hero matching the requirements was found!");
            }

            if (settings.StartingSkills?.Any() == true)
            {
                newHero.HeroDeveloper.ClearHero();

                // foreach (var skill in DefaultSkills.GetAllSkills())
                // {
                //     newHero.HeroDeveloper.SetInitialSkillLevel(skill, 0);
                // }
                // newHero.HeroDeveloper.SetInitialLevel(Math.Max(settings.StartingLevel ?? 0, 1));
                // int xp = Math.Max(settings.StartingLevel ?? 0, 10000);
                foreach (var skill in settings.StartingSkills)
                {
                    var actualSkills = SkillGroup.GetSkills(skill.Skill);
                    newHero.HeroDeveloper.SetInitialSkillLevel(actualSkills.SelectRandom(), 
                        MBMath.ClampInt(
                            MBRandom.RandomInt(
                                Math.Min(skill.MinLevel, skill.MaxLevel), 
                                Math.Max(skill.MinLevel, skill.MaxLevel)
                                ), 0, 300)
                        );
                }
                
                //newHero.HeroDeveloper.SetInitialSkillLevel(SkillGroup.RangedSkills.SelectRandom(), newHero.HeroDeveloper.TotalXp / 3);
                //newHero.HeroDeveloper.SetInitialSkillLevel(SkillGroup.MovementSkills.SelectRandom(), newHero.HeroDeveloper.TotalXp / 3);
                HeroHelper.DetermineInitialLevel(newHero);
                CharacterDevelopmentCampaignBehaivor.DevelopCharacterStats(newHero);
            }

            string oldName = newHero.Name.ToString();
            newHero.FirstName = new TextObject(userName);
            newHero.Name = new TextObject(BLTAdoptAHeroCampaignBehavior.GetFullName(userName));
            
            BLTAdoptAHeroCampaignBehavior.Get().SetHeroGold(newHero, settings.StartingGold);

            if (settings.StartingEquipmentTier.HasValue)
            {
                EquipHero.RemoveAllEquipment(newHero);
                EquipHero.UpgradeEquipment(newHero, settings.StartingEquipmentTier.Value, true, true,
                    settings.StartWithArmor, settings.StartWithHorse, true);
            }
            
            if(!Campaign.Current.EncyclopediaManager.BookmarksTracker.IsBookmarked(newHero))
            {
                Campaign.Current.EncyclopediaManager.BookmarksTracker.AddBookmarkToItem(newHero);
            }
            return (true, $"{oldName} is now known as {newHero.Name}, they have {settings.StartingGold} gold!");
        }
    }
}
