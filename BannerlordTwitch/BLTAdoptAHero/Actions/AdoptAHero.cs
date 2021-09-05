using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
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
    [LocDisplayName("{=NkZXnSQI}Adopt A Hero"),
     LocDescription("{=fd7G5N0Q}Allows viewer to 'adopt' a hero in game -- the hero name will change to the viewers name, and they can control it with further commands"),
     UsedImplicitly]
    public class AdoptAHero : IRewardHandler, ICommandHandler
    {
        public const string NoHeroMessage = "Couldn't find your hero, did you adopt one yet?";

        [CategoryOrder("General", 0), 
         CategoryOrder("Limits", 1), 
         CategoryOrder("Initialization", 1)]
        private class Settings : IDocumentable
        {
            [LocDisplayName("{=TLrDxhlh}Create New"), 
             LocCategory("General", "{=C5T5nnix}General"), 
             LocDescription("{=F1KDzuZZ}Create a new hero instead of adopting an existing one (they will be a wanderer at a random tavern)"), 
             PropertyOrder(1), UsedImplicitly]
            public bool CreateNew { get; set; }

            [LocDisplayName("{=nPIcT2s7}Allow Noble"), 
             LocCategory("Limits", "{=1lHWj3nT}Limits"), 
             LocDescription("{=XvZN7OOY}Allow noble heroes (if CreateNew is false)"), 
             PropertyOrder(1), UsedImplicitly]
            public bool AllowNoble { get; set; } = true;
            [LocDisplayName("{=VVFsa8LQ}Allow Wanderer"), 
             LocCategory("Limits", "{=1lHWj3nT}Limits"), 
             LocDescription("{=9lE2KSvC}Allow wanderer heroes (if CreateNew is false)"), 
             PropertyOrder(2), UsedImplicitly]
            public bool AllowWanderer { get; set; } = true;
            [LocDisplayName("{=A8G9ctbn}Allow Player Companion"), 
             LocCategory("Limits", "{=1lHWj3nT}Limits"), 
             LocDescription("{=6EjGRMkt}Allow companions (not tested, if CreateNew is false)"), 
             PropertyOrder(3), UsedImplicitly]
            public bool AllowPlayerCompanion { get; set; }
            [LocDisplayName("{=B2z7T1xQ}Only Same Faction"), 
             LocCategory("Limits", "{=1lHWj3nT}Limits"), 
             LocDescription("{=QvQGwFyl}Only allow heroes from same faction as player"), 
             PropertyOrder(4), UsedImplicitly]
            public bool OnlySameFaction { get; set; }

            [LocDisplayName("{=dvbkxJQz}Inheritance"), 
             LocCategory("Limits", "{=1lHWj3nT}Limits"), 
             LocDescription("{=KLJtpEjg}What fraction of assets will be inherited when a new character is adopted after an old one died (0 to 1)"),
             UIRangeAttribute(0, 1, 0.05f),
             Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
             PropertyOrder(6), UsedImplicitly]
            public float Inheritance { get; set; } = 0.25f;
            
            [LocDisplayName("{=Bi19tTPj}Maximum Inherited Custom Items"), 
             LocCategory("Limits", "{=1lHWj3nT}Limits"), 
             LocDescription("{=tFolfAOn}How many custom items can be inherited"), 
             Range(0, Int32.MaxValue),
             PropertyOrder(7), UsedImplicitly]
            public int MaxInheritedCustomItems { get; set; } = 2;
            
            [LocDisplayName("{=O4DGlP9Z}Subscriber Only"), 
             LocCategory("Limits", "{=1lHWj3nT}Limits"), 
             LocDescription("{=TBNkHsLC}Only subscribers can adopt"), 
             PropertyOrder(7), UsedImplicitly]
            public bool SubscriberOnly { get; set; }

            [LocDisplayName("{=dO41CKIU}Minimum Subscribed Months"), 
             LocCategory("Limits", "{=1lHWj3nT}Limits"),
             LocDescription("{=BVZwDqR0}Only viewers who have been subscribers for at least this many months can adopt, ignored if not specified"),
             PropertyOrder(8), UsedImplicitly]
            public int? MinSubscribedMonths { get; set; }

            [LocDisplayName("{=iOmYBC7I}Starting Gold"), 
             LocCategory("Initialization", "{=DRNO9OAl}Initialization"), 
             LocDescription("{=pZMkJLix}Gold the adopted hero will start with"), 
             PropertyOrder(1), UsedImplicitly, 
             Document]
            public int StartingGold { get; set; }

            [LocDisplayName("{=ZXwbvbbq}Override Age"), 
             LocCategory("Initialization", "{=DRNO9OAl}Initialization"),
             LocDescription("{=gxQgrAey}Override the heroes age"), 
             PropertyOrder(2), UsedImplicitly]
            public bool OverrideAge { get; set; }
            
            [LocDisplayName("{=NEBQgHiX}Starting Age Range"), 
             LocCategory("Initialization", "{=DRNO9OAl}Initialization"), 
             LocDescription("{=TYqEBuLW}Random range of age when overriding it"), 
             PropertyOrder(3), UsedImplicitly]
            public RangeFloat StartingAgeRange { get; set; } = new(18, 35);

            [LocDisplayName("{=9IOFHQjS}Starting Skills"), 
             LocCategory("Initialization", "{=DRNO9OAl}Initialization"),
             LocDescription("{=C4rV4f2F}Starting skills, if empty then default skills of the adopted hero will be left in tact"),
             Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
             PropertyOrder(4), UsedImplicitly]
            public ObservableCollection<SkillRangeDef> StartingSkills { get; set; } = new();

            [YamlIgnore, Browsable(false)]
            public IEnumerable<SkillRangeDef> ValidStartingSkills 
                => StartingSkills?.Where(s => s.Skill != SkillsEnum.None);
            
            [LocDisplayName("{=IAKQCRa1}Starting Equipment Tier"), 
             LocCategory("Initialization", "{=DRNO9OAl}Initialization"), 
             LocDescription("{=mQwjHXfC}Equipment tier the adopted hero will start with, if you don't specify then they get the heroes existing equipment"),
             Range(0, 6),
             PropertyOrder(5), UsedImplicitly]
            public int? StartingEquipmentTier { get; set; }
            
            [LocDisplayName("{=0vGFJdO1}Starting Class"), 
             LocCategory("Initialization", "{=DRNO9OAl}Initialization"), 
             LocDescription("{=zgjyFL6i}Starting class of the hero"), 
             PropertyOrder(6), ItemsSource(typeof(HeroClassDef.ItemSource)), UsedImplicitly]
            public Guid StartingClass { get; set; }

            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                if (SubscriberOnly)
                {
                    generator.Value("<strong>" +
                                    "{=4zAUTiSP}Subscriber Only".Translate() +
                                    "</strong>");
                }
                if (CreateNew)
                {
                    generator.Value("{=cJQCU33B}Newly created wanderer".Translate());
                }
                else
                {
                    var allowed = new List<string>();
                    if (AllowNoble) allowed.Add("{=fP84ES0X}Noble".Translate());
                    if (AllowWanderer) allowed.Add("{=ozRaAx6L}Wanderer".Translate());
                    if (AllowPlayerCompanion) allowed.Add("{=YucejFfO}Companions".Translate());
                    generator.PropertyValuePair("{=UNtHwNhx}Allowed".Translate(), string.Join(", ", allowed));
                }

                if (OnlySameFaction) generator.Value("{=6W0OJKkA}Same faction only".Translate());

                if (OverrideAge)
                {
                    generator.PropertyValuePair("{=pDP8b5HR}Starting Age Range".Translate(),
                        StartingAgeRange.IsFixed
                            ? $"{StartingAgeRange.Min}" 
                            : $"{StartingAgeRange.Min} to {StartingAgeRange.Max}"
                        );
                }                
                
                generator.PropertyValuePair("{=FvhsCSd3}Starting Gold".Translate(), $"{StartingGold}");
                generator.PropertyValuePair("{=wP0lfTf3}Inheritance".Translate(), 
                    "{=mDK67efh}{Inheritance}% of gold spent on equipment and retinue"
                        .Translate(("Inheritance", (int)(Inheritance * 100))) +
                    ", " +
                    (MaxInheritedCustomItems == 0 
                        ? "{=76NtQIGB}no custom items".Translate() 
                        : "{=sEDQrZCp}up to {MaxInheritedCustomItems} custom items"
                            .Translate(("MaxInheritedCustomItems", MaxInheritedCustomItems)) 
                    ));

                if (ValidStartingSkills.Any())
                {
                    generator.PropertyValuePair("{=9IOFHQjS}Starting Skills".Translate(), () =>
                        generator.Table("starting-skills", () =>
                        {
                            generator.TR(() =>
                                generator.TH("{=OEMBeawy}Skill".Translate()).TH("{=iu0dtUP5}Level".Translate())
                            );
                            foreach (var s in ValidStartingSkills)
                            {
                                generator.TR(() =>
                                {
                                    generator.TD(s.Skill.GetDisplayName());
                                    generator.TD(s.IsFixed
                                        ? $"{s.MinLevel}"
                                        : "{=yVydxRHh}{From} to {To}".Translate(
                                            ("From", s.MinLevel), ("To", s.MaxLevel)));
                                });
                            }
                        }));
                }

                if (StartingEquipmentTier.HasValue)
                {
                    generator.PropertyValuePair("{=IAKQCRa1}Starting Equipment Tier".Translate(), $"{StartingEquipmentTier.Value}");
                }

                if (StartingClass != Guid.Empty)
                {
                    var classDef = BLTAdoptAHeroModule.HeroClassConfig.GetClass(StartingClass);
                    if (classDef != null)
                    {
                        generator.PropertyValuePair("{=0vGFJdO1}Starting Class".Translate(), 
                            () => generator.LinkToAnchor(classDef.Name.ToString(), classDef.Name.ToString()));
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
                ActionManager.NotifyCancelled(context, "{=mJfD7e2g}You have already adopted a hero!".Translate());
                return;
            }
            var settings = (Settings)config;
            (bool success, string message) = ExecuteInternal(context.UserName, settings);
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
                ActionManager.SendReply(context, "{=mJfD7e2g}You have already adopted a hero!".Translate());
                return;
            }
            
            var settings = (Settings)config;
            if (settings.MinSubscribedMonths > 0 && context.SubscribedMonthCount < settings.MinSubscribedMonths)
            {
                ActionManager.SendReply(context, 
                    "{=4K7Q7gR0}You must be subscribed for at least {MinSubscribedMonths} months to adopt a hero with this command!".Translate(("MinSubscribedMonths", settings.MinSubscribedMonths)));
                return;
            }
            if(!context.IsSubscriber && settings.SubscriberOnly)
            {
                ActionManager.SendReply(context, "{=0QeQPxYi}You must be subscribed to adopt a hero with this command!".Translate());
                return;
            }
                
            (_, string message) = ExecuteInternal(context.UserName, settings);
            ActionManager.SendReply(context, message);
        }

        private static (bool success, string message) ExecuteInternal(string userName, Settings settings)
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
                        (settings.AllowNoble || !h.IsNoble) 
                        && (settings.AllowWanderer || !h.IsWanderer)
                        && (settings.AllowPlayerCompanion || !h.IsPlayerCompanion)
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
                    return (false, "{=XlQUIIsg}No hero is available: player is not in a faction (disable Player Faction Only, or join a faction)!".Translate());
                }
            }
            
            if (newHero == null)
            {
                return (false, "{=E7wqQ2kg}You can't adopt a hero: no available hero matching the requirements was found!".Translate());
            }

            if (settings.OverrideAge)
            {
                newHero.SetBirthDay(CampaignTime.YearsFromNow(-Math.Max(Campaign.Current.Models.AgeModel.HeroComesOfAge, settings.StartingAgeRange.RandomInRange())));
            }
            
            // Place hero where we want them
            if(settings.CreateNew)
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

            if(Campaign.Current != null && !Campaign.Current.EncyclopediaManager.BookmarksTracker.IsBookmarked(newHero))
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

            Log.ShowInformation(
                "{=K7nuJVCN}{OldName} is now known as {NewName}!".Translate(("OldName", oldName), ("NewName", newHero.Name)), 
                newHero.CharacterObject, Log.Sound.Horns2);

            return inherited.Any() 
                ? (true, "{=PAc5S0GY}{OldName} is now known as {NewName}, they have {NewGold} (inheriting {Inherited})!"
                    .Translate(
                        ("OldName", oldName), 
                        ("NewName", newHero.Name), 
                        ("NewGold", newGold + Naming.Gold), 
                        ("Inherited", string.Join(", ", inherited)))) 
                : (true, "{=lANBKEFN}{OldName} is now known as {NewName}, they have {NewGold}!".Translate(
                    ("OldName", oldName), 
                    ("NewName", newHero.Name), 
                    ("NewGold", newGold + Naming.Gold)));
        }
    }
}
