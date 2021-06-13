using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using Helpers;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.Towns;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Allows viewer to 'adopt' a hero in game -- the hero name will change to the viewers name, and they can control it with further commands")]
    public class AdoptAHero : IRewardHandler, ICommandHandler
    {


        internal const string NoHeroMessage = "Couldn't find your hero, did you adopt one yet?";
        internal const string NotStartedMessage = "The game isn't started yet";

        [CategoryOrder("General", 0)]
        [CategoryOrder("Limits", 1)]
        [CategoryOrder("Initialization", 1)]
        private class Settings
        {
            [Category("General"), Description("Create a new hero instead of adopting an existing one (they will be a wanderer at a random tavern)"), PropertyOrder(1)]
            public bool CreateNew { get; set; }

            [Category("Limits"), Description("Allow noble heroes (if CreateNew is false)"), PropertyOrder(1)]
            public bool AllowNoble { get; set; } = true;
            [Category("Limits"), Description("Allow wanderer heroes (if CreateNew is false)"), PropertyOrder(2)]
            public bool AllowWanderer { get; set; } = true;
            [Category("Limits"), Description("Allow companions (not tested, if CreateNew is false)"), PropertyOrder(3)]
            public bool AllowPlayerCompanion { get; set; }
            [Category("Limits"), Description("Only allow heroes from same faction as player"), PropertyOrder(4)]
            public bool OnlySameFaction { get; set; }
            [Category("Limits"), Description("Only allow viewer to adopt another hero if theirs is dead"), PropertyOrder(5)]
            public bool AllowNewAdoptionOnDeath { get; set; } = true;

            [Category("Limits"),
             Description("What fraction of assets will be inherited when a new character is " +
                         "adopted after an old one died (0 to 1)"),
             PropertyOrder(6)]
            public float Inheritance { get; set; } = 0.25f;
            [Category("Limits"), Description("Only subscribers can adopt"), PropertyOrder(7)]
            public bool SubscriberOnly { get; set; }
            [Category("Limits"),
             Description("Only viewers who have been subscribers for at least this many months can adopt, " +
                         "ignored if not specified"),
             DefaultValue(null), PropertyOrder(8)]
            public int? MinSubscribedMonths { get; set; }
            [Category("Initialization"), 
             Description("Gold the adopted hero will start with"), DefaultValue(null), PropertyOrder(1)]
            public int StartingGold { get; set; }

            [Category("Initialization"), 
             Description("Starting skills, if empty then default skills of the adopted hero will be left in tact"),
             DefaultValue(null), PropertyOrder(1)]
            public List<SkillRangeDef> StartingSkills { get; set; }
            
            [Category("Initialization"), 
             Description("Equipment tier the adopted hero will start with, if you don't specify then they get the " +
                         "heroes existing equipment"), DefaultValue(null), PropertyOrder(2)]
            public int? StartingEquipmentTier { get; set; }

            [Category("Initialization"),
             Description("Whether the hero will start with a melee weapon, only applies if StartingEquipmentTier is " +
                         "specified"), PropertyOrder(3)]
            public bool StartWithMeleeWeapon { get; set; } = true;
            
            [Category("Initialization"),
             Description("Whether the hero will start with a ranged weapon, only applies if StartingEquipmentTier is " +
                         "specified"), PropertyOrder(3)]
            public bool StartWithRangedWeapon { get; set; } = true;
            
            [Category("Initialization"), Description("Whether the hero will start with a horse, only applies if StartingEquipmentTier is specified"), PropertyOrder(3)]
            public bool StartWithHorse { get; set; } = true;
            
            [Category("Initialization"), Description("Whether the hero will start with armor, only applies if StartingEquipmentTier is specified"), PropertyOrder(4)]
            public bool StartWithArmor { get; set; } = true;
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
            (bool success, string message) = ExecuteInternal(context.Args, context.UserName, settings);
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
            if (BLTAdoptAHeroCampaignBehavior.GetAdoptedHero(context.UserName) != null)
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
                ActionManager.SendReply(context, "You must be subscribed to adopt a hero with this command!");
                return;
            }
                
            (_, string message) = ExecuteInternal(context.Args, context.UserName, settings);
            ActionManager.SendReply(context, message);
        }

        private static (bool success, string message) ExecuteInternal(string args, string userName, Settings settings)
        {
            if (Campaign.Current == null)
            {
                return (false, NotStartedMessage);
            }

            var deadHero = BLTAdoptAHeroCampaignBehavior.GetDeadHero(userName);
            if (deadHero != null && !settings.AllowNewAdoptionOnDeath)
            {
                return (false, "Your hero died, and you may not adopt another!");
            }
            else if(deadHero != null)
            {
                BLTAdoptAHeroCampaignBehavior.RetireHero(deadHero);
            }

            Hero newHero = null;
            if (settings.CreateNew)
            {
                var character = CharacterObject.Templates
                    .Where(x => x.Occupation == Occupation.Wanderer)
                    .SelectRandom();
                if (character != null)
                {
                    newHero = HeroCreator.CreateSpecialHero(character);
                    newHero.ChangeState(Hero.CharacterStates.Active);
                    var targetSettlement = Settlement.All.Where(s => s.IsTown).SelectRandom();
                    EnterSettlementAction.ApplyForCharacterOnly(newHero, targetSettlement);
                    Log.Info($"Placed new hero {newHero.Name} at {targetSettlement.Name}");
                }
            }
            else
            {
                newHero = BLTAdoptAHeroCampaignBehavior.GetAvailableHeroes(h =>
                        // Filter by allowed types
                        (settings.AllowPlayerCompanion && h.IsPlayerCompanion
                         || settings.AllowNoble && h.IsNoble
                         || settings.AllowWanderer && h.IsWanderer)
                        // Select correct clan faction
                        && (!settings.OnlySameFaction
                            || Clan.PlayerClan?.MapFaction != null
                            && Clan.PlayerClan?.MapFaction == h.Clan?.MapFaction)
                        // Disallow rebel clans as they may get deleted if the rebellion fails
                        && h.Clan?.IsRebelClan != true
                    )
                    .SelectRandom();
                if (newHero == null && settings.OnlySameFaction && Clan.PlayerClan?.MapFaction?.StringId == "player_faction")
                {
                    return (false, "No hero is available: player is not in a faction (disable Player Faction Only, or join a faction)!");
                }
            }
            
            if (newHero == null)
            {
                return (false, "You can't adopt a hero: no available hero matching the requirements was found!");
            }
            
            if (settings.StartingSkills?.Any() == true)
            {
                newHero.HeroDeveloper.ClearHero();

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
                
                HeroHelper.DetermineInitialLevel(newHero);
#if e159 || e1510
                CharacterDevelopmentCampaignBehaivor.DevelopCharacterStats(newHero);
#else
                CharacterDevelopmentCampaignBehavior.DevelopCharacterStats(newHero);
#endif
            }

            string oldName = newHero.Name.ToString();
            BLTAdoptAHeroCampaignBehavior.SetHeroAdoptedName(newHero, userName);
            
            if (settings.StartingEquipmentTier.HasValue)
            {
                EquipHero.RemoveAllEquipment(newHero);
                if (settings.StartingEquipmentTier.Value > 0)
                {
                    EquipHero.UpgradeEquipment(newHero, settings.StartingEquipmentTier.Value - 1, null, keepBetter: false);
                }
                BLTAdoptAHeroCampaignBehavior.Current.SetEquipmentTier(newHero, settings.StartingEquipmentTier.Value - 1);
            }
            else
            {
                BLTAdoptAHeroCampaignBehavior.Current.SetEquipmentTier(newHero, EquipHero.GetHeroEquipmentTier(newHero));
            }
            
            if(!Campaign.Current.EncyclopediaManager.BookmarksTracker.IsBookmarked(newHero))
            {
                Campaign.Current.EncyclopediaManager.BookmarksTracker.AddBookmarkToItem(newHero);
            }
            
            BLTAdoptAHeroCampaignBehavior.Current.SetHeroGold(newHero, settings.StartingGold);

            Log.ShowInformation($"{oldName} is now known as {newHero.Name}!", newHero.CharacterObject, Log.Sound.Horns2);
            int inherited = BLTAdoptAHeroCampaignBehavior.Current.InheritGold(newHero, settings.Inheritance);
            int newGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(newHero);
            return inherited > 0 
                ? (true, $"{oldName} is now known as {newHero.Name}, they have {newGold}{Naming.Gold} (inheriting {inherited}{Naming.Gold})!") 
                : (true, $"{oldName} is now known as {newHero.Name}, they have {newGold}{Naming.Gold}!");
        }
    }
}
