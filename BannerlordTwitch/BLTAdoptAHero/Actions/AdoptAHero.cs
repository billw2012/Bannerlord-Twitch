using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Actions.Util;
using BLTAdoptAHero.Annotations;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.Towns;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    [JetBrains.Annotations.UsedImplicitly]
    [Description("Allows viewer to 'adopt' a hero in game -- the hero name will change to the viewers name, and they can control it with further commands")]
    public class AdoptAHero : IRewardHandler, ICommandHandler
    {
        public const string NoHeroMessage = "Couldn't find your hero, did you adopt one yet?";

        [CategoryOrder("General", 0)]
        [CategoryOrder("Limits", 1)]
        [CategoryOrder("Initialization", 1)]
        private class Settings : IDocumentable
        {
            [Category("General"), 
             Description("Create a new hero instead of adopting an existing one (they will be a wanderer at a " +
                         "random tavern)"), 
             PropertyOrder(1), UsedImplicitly]
            public bool CreateNew { get; set; }

            [Category("Limits"), Description("Allow noble heroes (if CreateNew is false)"), 
             PropertyOrder(1), UsedImplicitly]
            public bool AllowNoble { get; set; } = true;
            [Category("Limits"), Description("Allow wanderer heroes (if CreateNew is false)"), 
             PropertyOrder(2), UsedImplicitly]
            public bool AllowWanderer { get; set; } = true;
            [Category("Limits"), Description("Allow companions (not tested, if CreateNew is false)"), 
             PropertyOrder(3), UsedImplicitly]
            public bool AllowPlayerCompanion { get; set; }
            [Category("Limits"), Description("Only allow heroes from same faction as player"), 
             PropertyOrder(4), UsedImplicitly]
            public bool OnlySameFaction { get; set; }

            [Category("Limits"), 
             Description("What fraction of assets will be inherited when a new character is adopted after an old one " +
                         "died (0 to 1)"), PropertyOrder(6), UsedImplicitly]
            public float Inheritance { get; set; } = 0.25f;
            
            [Category("Limits"), Description("How many custom items can be inherited"), 
             PropertyOrder(7), UsedImplicitly]
            public int MaxInheritedCustomItems { get; set; } = 2;
            
            [Category("Limits"), Description("Only subscribers can adopt"), PropertyOrder(7), UsedImplicitly]
            public bool SubscriberOnly { get; set; }

            [Category("Limits"),
             Description("Only viewers who have been subscribers for at least this many months can adopt, " +
                         "ignored if not specified"),
             PropertyOrder(8), UsedImplicitly]
            public int? MinSubscribedMonths { get; set; }
            [Category("Initialization"), 
             Description("Gold the adopted hero will start with"), PropertyOrder(1), UsedImplicitly, 
             Document]
            public int StartingGold { get; set; }

            [Category("Initialization"), Description("Override the heroes age"), 
             PropertyOrder(2), UsedImplicitly]
            public bool OverrideAge { get; set; }
            
            [Category("Initialization"), Description("Random range of age when overriding it"), 
             PropertyOrder(3), ExpandableObject, UsedImplicitly]
            public RangeFloat StartingAgeRange { get; set; } = new(18, 35);

            [Category("Initialization"),
             Description("Starting skills, if empty then default skills of the adopted hero will be left in tact"),
             PropertyOrder(4), UsedImplicitly]
            public List<SkillRangeDef> StartingSkills { get; set; } = new();

            [YamlIgnore, Browsable(false)]
            public IEnumerable<SkillRangeDef> ValidStartingSkills 
                => StartingSkills?.Where(s => s.Skill != SkillsEnum.None);
            
            [Category("Initialization"), 
             Description("Equipment tier the adopted hero will start with, if you don't specify then they get the " +
                         "heroes existing equipment"), PropertyOrder(5), UsedImplicitly]
            public int? StartingEquipmentTier { get; set; }
            
            [Category("Initialization"), Description("Starting class of the hero"), 
             PropertyOrder(6), ItemsSource(typeof(HeroClassDef.ItemSource)), UsedImplicitly]
            public Guid StartingClass { get; set; }

            [Category("Initialization"), 
             Description("Whether the hero will spawn in hero party (Only work with Join Player Companion activated)"), 
             PropertyOrder(7), UsedImplicitly]
            public bool SpawnInParty { get; set; }
            
            [Category("Initialization"), Description("Whether the hero will be a companion"), 
             PropertyOrder(8), UsedImplicitly]
            public bool JoinPlayerCompanion { get; set; }

            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                if (SubscriberOnly)
                {
                    generator.Value("<strong>Subscriber Only</strong>");
                }
                if (CreateNew)
                {
                    generator.Value("Newly created wanderer");
                }
                else
                {
                    var allowed = new List<string>();
                    if (AllowNoble) allowed.Add("Noble");
                    if (AllowWanderer) allowed.Add("Wanderer");
                    if (AllowPlayerCompanion) allowed.Add("Companions");
                    generator.PropertyValuePair("Allowed", string.Join(", ", allowed));
                }
                if (SpawnInParty && JoinPlayerCompanion) generator.Value("Become a new player companion, in streamers party");
                if (!SpawnInParty && JoinPlayerCompanion) generator.Value("Become a new player companion");
                
                if (OnlySameFaction) generator.Value("Same faction only");

                if (OverrideAge)
                {
                    generator.PropertyValuePair("Starting Age Range",
                        StartingAgeRange.IsFixed
                            ? $"{StartingAgeRange.Min}" 
                            : $"{StartingAgeRange.Min} to {StartingAgeRange.Max}"
                        );
                }                
                
                generator.PropertyValuePair("Starting Gold", $"{StartingGold}");
                generator.PropertyValuePair("Inheritance", 
                    $"{Inheritance * 100:0.0}% of gold spent on equipment and retinue, " +
                    (MaxInheritedCustomItems == 0 ? "no" : $"up to {MaxInheritedCustomItems}") +
                    " custom items");

                if (ValidStartingSkills.Any())
                {
                    generator.PropertyValuePair("Starting Skills", () =>
                        generator.Table("starting-skills", () =>
                        {
                            generator.TR(() =>
                                generator.TH("Skill").TH("Level")
                            );
                            foreach (var s in ValidStartingSkills)
                            {
                                generator.TR(() =>
                                {
                                    generator.TD(s.Skill.ToString().SplitCamelCase());
                                    generator.TD(s.IsFixed
                                        ? $"{s.MinLevel}"
                                        : $"{s.MinLevel} to {s.MaxLevel}");
                                });
                            }
                        }));
                }

                if (StartingEquipmentTier.HasValue)
                {
                    generator.PropertyValuePair("Starting Equipment Tier", $"{StartingEquipmentTier.Value}");
                }

                if (StartingClass != Guid.Empty)
                {
                    var classDef = BLTAdoptAHeroModule.HeroClassConfig.GetClass(StartingClass);
                    if (classDef != null)
                    {
                        generator.PropertyValuePair("Starting Class", 
                            () => generator.LinkToAnchor(classDef.Name, classDef.Name));
                    }
                }
            }
        }

        Type IRewardHandler.RewardConfigType => typeof(Settings);
        void IRewardHandler.Enqueue(ReplyContext context, object config)
        {
            var hero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
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
            if (BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName) != null)
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

        private static (bool success, string message) ExecuteInternal(string _, string userName, Settings settings)
        {
            Hero newHero = null;
            // Create or find a hero for adopting
            if (settings.CreateNew)
            {
                var character = CharacterObject.Templates
                    .Where(x => x.Occupation == Occupation.Wanderer)
                    .SelectRandom();
                if (character != null)
                {
                    newHero = HeroCreator.CreateSpecialHero(character);
                    newHero.ChangeState(Hero.CharacterStates.Active);
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

            if (settings.OverrideAge)
            {
                newHero.SetBirthDay(CampaignTime.YearsFromNow(-Math.Max(Campaign.Current.Models.AgeModel.HeroComesOfAge, settings.StartingAgeRange.RandomInRange())));
            }
            
            // Place hero where we want them
            if (settings.JoinPlayerCompanion && settings.SpawnInParty)
            {
                var mainParty = MobileParty.MainParty;
                if (mainParty != null && mainParty.Party.NumberOfAllMembers < mainParty.Party.PartySizeLimit)
                {
                    AddHeroToPartyAction.Apply(newHero, mainParty);
                    Log.Info($"Placed new hero {newHero.Name} in hero party");
                }
                else
                {
                    return mainParty == null 
                        ? (false, "You can't adopt a hero: main hero party don't exist yet!") 
                        : (false, "You can't adopt a hero: main hero party is full!");
                }
            }
            else if(settings.CreateNew)
            {
                var targetSettlement = Settlement.All.Where(s => s.IsTown).SelectRandom();
                EnterSettlementAction.ApplyForCharacterOnly(newHero, targetSettlement);
                Log.Info($"Placed new hero {newHero.Name} at {targetSettlement.Name}");
            }

            if (settings.ValidStartingSkills?.Any() == true)
            {
                newHero.HeroDeveloper.ClearHero();

                foreach (var skill in settings.ValidStartingSkills)
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
                Campaign.Current?.GetCampaignBehavior<CharacterDevelopmentCampaignBehavior>()?.DevelopCharacterStats(newHero);
#endif
            }

            // A wanderer MUST have at least 1 skill point, or they get killed on load 
            if (newHero.GetSkillValue(HeroHelpers.AllSkillObjects.First()) == 0)
            {
                newHero.HeroDeveloper.SetInitialSkillLevel(HeroHelpers.AllSkillObjects.First(), 1);
            }

            if (settings.JoinPlayerCompanion)
            {
                AddCompanionAction.Apply(Hero.MainHero.Clan, newHero);
            }

            HeroClassDef classDef = null;
            if (settings.StartingClass != default)
            {
                classDef = BLTAdoptAHeroModule.HeroClassConfig.GetClass(settings.StartingClass);
                if (classDef == null)
                {
                    Log.Error($"AdoptAHero: StartingClass not found, please re-select it in settings");
                }
                else
                {
                    BLTAdoptAHeroCampaignBehavior.Current.SetClass(newHero, classDef);
                }
            }

            // Setup skills first, THEN name, as skill changes can generate feed messages for adopted characters
            string oldName = newHero.Name.ToString();
            BLTAdoptAHeroCampaignBehavior.Current.InitAdoptedHero(newHero, userName);
            
            // Inherit items before equipping, so we can use them DURING equipping
            var inheritedItems = BLTAdoptAHeroCampaignBehavior.Current.InheritCustomItems(newHero, settings.MaxInheritedCustomItems);
            if (settings.StartingEquipmentTier.HasValue)
            {
                EquipHero.RemoveAllEquipment(newHero);
                if (settings.StartingEquipmentTier.Value > 0)
                {
                    EquipHero.UpgradeEquipment(newHero, settings.StartingEquipmentTier.Value - 1, 
                        classDef, replaceSameTier: false);
                }
                BLTAdoptAHeroCampaignBehavior.Current.SetEquipmentTier(newHero, settings.StartingEquipmentTier.Value - 1);
                BLTAdoptAHeroCampaignBehavior.Current.SetEquipmentClass(newHero, classDef);
            }

            if(!Campaign.Current.EncyclopediaManager.BookmarksTracker.IsBookmarked(newHero))
            {
                Campaign.Current.EncyclopediaManager.BookmarksTracker.AddBookmarkToItem(newHero);
            }
            
            BLTAdoptAHeroCampaignBehavior.Current.SetHeroGold(newHero, settings.StartingGold);

            int inheritedGold = BLTAdoptAHeroCampaignBehavior.Current.InheritGold(newHero, settings.Inheritance);
            int newGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(newHero);

            var inherited = inheritedItems.Select(i => i.GetModifiedItemName().ToString()).ToList();
            if (inheritedGold != 0)
            {
                inherited.Add($"{inheritedGold}{Naming.Gold}");
            }

            Log.ShowInformation($"{oldName} is now known as {newHero.Name}!", newHero.CharacterObject, Log.Sound.Horns2);

            return inherited.Any() 
                ? (true, $"{oldName} is now known as {newHero.Name}, they have {newGold}{Naming.Gold} (inheriting {string.Join(", ", inherited)})!") 
                : (true, $"{oldName} is now known as {newHero.Name}, they have {newGold}{Naming.Gold}!");
        }
    }
}
