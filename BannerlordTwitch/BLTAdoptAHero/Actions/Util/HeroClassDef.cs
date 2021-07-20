using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Actions.Util;
using BLTAdoptAHero.Powers;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    public class HeroClassDef : IConfig, IDocumentable
    {
        [ReadOnly(true), UsedImplicitly]
        public Guid ID { get => ObjectIDRegistry.Get(this); set => ObjectIDRegistry.Set(this, value); }

        #region User Editable Properties
        [Description("Name of the class that shall be passed to SetHeroClass actions"), PropertyOrder(1), UsedImplicitly]
        public string Name { get; set; } = "Enter Name Here";
        
        [Description("Description of the class (used in documentation)"), PropertyOrder(1), UsedImplicitly]
        public string Description { get; set; } = "";

        [Description("Which formation to add summoned units to"), PropertyOrder(2), ItemsSource(typeof(SummonHero.FormationItemSource)), UsedImplicitly]
        public string Formation { get; set; } = "LightCavalry";
        
        [Description("Item type to put in slot 1"), PropertyOrder(3), UsedImplicitly]
        public EquipmentType Slot1 { get; set; }
        
        [Description("Item type to put in slot 2"), PropertyOrder(4), UsedImplicitly]
        public EquipmentType Slot2 { get; set; }
        
        [Description("Item type to put in slot 3"), PropertyOrder(5), UsedImplicitly]
        public EquipmentType Slot3 { get; set; }
        
        [Description("Item type to put in slot 4"), PropertyOrder(6), UsedImplicitly]
        public EquipmentType Slot4 { get; set; }
        
        [Description("Whether to allow horse (can be combined with Use Camel)"), PropertyOrder(7), UsedImplicitly]
        public bool UseHorse { get; set; }
        
        [Description("Whether to allow camel (can be combined with Use Horse"), PropertyOrder(8), UsedImplicitly]
        public bool UseCamel { get; set; }
        
        [Description("Passive hero power: this will always apply to the hero (i.e. a permanent buff)"), 
         PropertyOrder(9), ExpandableObject, UsedImplicitly]
        public PassivePowerGroup PassivePower { get; set; } = new();

        [Description("Active hero power: this power will be triggered only when the UseHeroPower action is used by " +
                     "the viewer, via reward or command (i.e. a temporary buff)"), 
         PropertyOrder(10), ExpandableObject, UsedImplicitly]
        public ActivePowerGroup ActivePower { get; set; } = new();
        #endregion

        [YamlIgnore, Browsable(false)]
        public IEnumerable<EquipmentType> Slots 
        { get { yield return Slot1; yield return Slot2; yield return Slot3; yield return Slot4; } }
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<(EquipmentIndex index, EquipmentType type)> IndexedSlots 
        {
            get
            {
                yield return (EquipmentIndex.Weapon0, Slot1);
                yield return (EquipmentIndex.Weapon1, Slot2);
                yield return (EquipmentIndex.Weapon2, Slot3);
                yield return (EquipmentIndex.Weapon3, Slot4);
            } 
        }

        [YamlIgnore, Browsable(false)]
        public IEnumerable<EquipmentType> SlotItems 
            => Slots.Where(s => s is not EquipmentType.None);
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<EquipmentType> Weapons 
            => Slots.Where(s => s is not (EquipmentType.None or EquipmentType.Shield));
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<(EquipmentIndex index, EquipmentType type)> IndexedWeapons 
            => IndexedSlots.Where(s => s.type is not (EquipmentType.None or EquipmentType.Shield));

        [YamlIgnore, Browsable(false)]
        public bool Mounted => UseHorse || UseCamel;
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<SkillObject> Skills
        {
            get
            {
                foreach (var skill in SkillGroup.GetSkillsForEquipmentType(SlotItems)
                    .Where(s => s != SkillsEnum.None)
                    .Select(SkillGroup.GetSkill)
                    .Distinct())
                {
                    yield return skill;
                }

                if (Mounted)
                {
                    yield return DefaultSkills.Riding;
                }
                else
                {
                    yield return DefaultSkills.Athletics;
                }
            }
        }

        public override string ToString() => 
            $"{Name} : {string.Join(", ", SlotItems.Select(s => s.ToString()))}"
            + (UseHorse ? " (Use Horse)" : "")
            + (UseCamel ? " (Use Camel)" : "");

        public class PassivePowerGroup : IConfig, IDocumentable
        {
            [Description("The name of the power: how the power will be described in messages"), PropertyOrder(1), UsedImplicitly]
            public string Name { get; set; } = "Passive Power";

            [ItemsSource(typeof(HeroPowerDefBase.ItemSourcePassive)), PropertyOrder(1), UsedImplicitly]
            public Guid Power1 { get; set; }
            [ItemsSource(typeof(HeroPowerDefBase.ItemSourcePassive)), PropertyOrder(2), UsedImplicitly]
            public Guid Power2 { get; set; }
            [ItemsSource(typeof(HeroPowerDefBase.ItemSourcePassive)), PropertyOrder(3), UsedImplicitly]
            public Guid Power3 { get; set; }

            [YamlIgnore, Browsable(false)]
            private GlobalHeroPowerConfig PowerConfig { get; set; }

            [Browsable(false), YamlIgnore]
            private IEnumerable<IHeroPowerPassive> Powers {
                get
                {
                    if (PowerConfig.GetPower(Power1) is IHeroPowerPassive p1) yield return p1;
                    if (PowerConfig.GetPower(Power2) is IHeroPowerPassive p2) yield return p2;
                    if (PowerConfig.GetPower(Power3) is IHeroPowerPassive p3) yield return p3;
                }
            }
            
            public void OnHeroJoinedBattle(Hero hero)
            {
                if (PowerConfig.DisablePowersInTournaments && MissionHelpers.InTournament())
                {
                    return;
                }
                foreach(var power in Powers)
                {
                    BLTHeroPowersMissionBehavior.Current.ConfigureHandlers(
                        hero, power as HeroPowerDefBase, handlers => power.OnHeroJoinedBattle(hero, handlers));
                }
            }
            
            public override string ToString() => $"{Name} {string.Join(" ", Powers.Select(p => p.ToString()))}";

            #region IConfig
            public void OnLoaded(Settings settings)
            {
                PowerConfig = GlobalHeroPowerConfig.Get(settings);   
            }
            public void OnSaving() { }
            public void OnEditing() { }
            #endregion

            #region IDocumentable
            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                generator.P("power-title", Name);
                foreach (var power in Powers)
                {
                    if (power is IDocumentable docPower)
                    {
                        docPower.GenerateDocumentation(generator);
                    }
                    else
                    {
                        generator.P(power.ToString());
                    }
                }
                
                // generator.Table("power", () =>
                // {
                //     generator.TR(() => generator.TD("Name").TD(Name));
                //     foreach ((var power, int i) in Powers.Select((power, i) => (power, i)))
                //     {
                //         generator.TR(() => generator.TD($"Effect {i + 1}").TD(power.ToString().SplitCamelCase()));
                //     }
                // });
            }
            #endregion
        }

        public class ActivePowerGroup : IConfig, IDocumentable
        {
            [Description("The name of the power: how the power will be described in messages"), PropertyOrder(1), UsedImplicitly]
            public string Name { get; set; } = "Active Power";

            [ItemsSource(typeof(HeroPowerDefBase.ItemSourceActive)), PropertyOrder(1), UsedImplicitly]
            public Guid Power1 { get; set; }
            [ItemsSource(typeof(HeroPowerDefBase.ItemSourceActive)), PropertyOrder(2), UsedImplicitly]
            public Guid Power2 { get; set; }
            [ItemsSource(typeof(HeroPowerDefBase.ItemSourceActive)), PropertyOrder(3), UsedImplicitly]
            public Guid Power3 { get; set; }
            
            [Description("Particles/sound effects to play when this power group is activated"), PropertyOrder(4), ExpandableObject, UsedImplicitly]
            public OneShotEffect ActivateEffect { get; set; } = new();

            [Description("Particles/sound effects to play when this power group is deactivated"), PropertyOrder(5), ExpandableObject, UsedImplicitly]
            public OneShotEffect DeactivateEffect { get; set; } = new();


            [YamlIgnore, Browsable(false)]
            private GlobalHeroPowerConfig PowerConfig { get; set; }
            
            [Browsable(false), YamlIgnore]
            private IEnumerable<IHeroPowerActive> Powers {
                get
                {
                    if (PowerConfig.GetPower(Power1) is IHeroPowerActive p1) yield return p1;
                    if (PowerConfig.GetPower(Power2) is IHeroPowerActive p2) yield return p2;
                    if (PowerConfig.GetPower(Power3) is IHeroPowerActive p3) yield return p3;
                }
            }

            public bool IsActive(Hero hero) => Powers.Any(power => power.IsActive(hero));

            public (bool canActivate, string failReason) CanActivate(Hero hero)
            {
                if (PowerConfig.DisablePowersInTournaments && MissionHelpers.InTournament())
                {
                    return (false, "Not allowed in tournaments!");
                }

                var activePowers = Powers.ToList();

                if (!activePowers.Any())
                {
                    return (false, "No powers!");
                }

                (bool _, string failReason) = activePowers
                    .Select(power => power.CanActivate(hero))
                    .FirstOrDefault(r => !r.canActivate);
                return failReason != null 
                    ? (false, failReason)
                    : (true, null);
            }

            public (bool allowed, string message) Activate(Hero hero, ReplyContext context)
            {
                if (PowerConfig.DisablePowersInTournaments && MissionHelpers.InTournament())
                {
                    return (false, $"Powers not allowed in tournaments!");
                }
                
                foreach(var power in Powers)
                {
                    power.Activate(hero, () =>
                    {
                        if (Powers.All(p => !p.IsActive(hero)))
                        {
                            ActionManager.SendReply(context, $"{Name} expired!");
                            DeactivateEffect.Trigger(hero);
                        }
                    });
                }
                ActivateEffect.Trigger(hero);
                return (true, $"{Name} activated!");
            }

            public (float duration, float remaining) DurationRemaining(Hero hero)
            {
                if (!Powers.Any()) 
                    return (1, 0);
                var remaining = Powers
                    .Select(active => active.DurationRemaining(hero))
                    .ToList();
                return (
                    duration: remaining.Max(r => r.duration),
                    remaining: remaining.Max(r => r.remaining)
                    );
            }

            public override string ToString() => $"{Name} {string.Join(" ", Powers.Select(p => p.ToString()))}";
            
            #region IConfig
            public void OnLoaded(Settings settings)
            {
                PowerConfig = GlobalHeroPowerConfig.Get(settings);   
            }
            public void OnSaving() { }
            public void OnEditing() { }
            #endregion
            
            #region IDocumentable
            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                generator.P("power-title", Name);
                foreach (var power in Powers)
                {
                    if (power is IDocumentable docPower)
                    {
                        docPower.GenerateDocumentation(generator);
                    }
                    else
                    {
                        generator.P(power.ToString());
                    }
                }
            }
            #endregion
        }

        #region ItemSource
        public class ItemSource : IItemsSource
        {
            public static IEnumerable<HeroClassDef> All { get; set; }

            public ItemCollection GetValues()
            {
                var col = new ItemCollection();
                col.Add(Guid.Empty, "(none)");

                if (All != null)
                {
                    foreach (var item in All)
                    {
                        col.Add(item.ID, item.Name);
                    }
                }

                return col;
            }
        }
        #endregion

        #region IConfig
        public void OnLoaded(Settings settings)
        {
            PassivePower?.OnLoaded(settings);
            ActivePower?.OnLoaded(settings);
        }

        public void OnSaving()
        {
            PassivePower?.OnSaving();
            ActivePower?.OnSaving();
        }

        public void OnEditing()
        {
            PassivePower?.OnEditing();
            ActivePower?.OnEditing();
        }
        #endregion

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.P(!string.IsNullOrEmpty(Description) ? Description : "Details");

            generator.Table("hero-class-tiers", () =>
            {
                generator.TR(() =>
                {
                    for (int i = 1; i <= 6; i++)
                    {
                        generator.TH($"Tier {i}");
                    }
                });
                generator.TR(() =>
                {
                    for (int i = 1; i <= 6; i++)
                    {
                        EquipHero.UpgradeEquipment(Hero.MainHero, i, this, false);
                        generator.TD(() =>
                            generator.Img(CharacterCode.CreateFrom(Hero.MainHero.CharacterObject),
                                $"{Name} Tier {i} Example")
                        );
                    }
                });
            });

            generator.Table("hero-class", () =>
            {
                generator.TR(() => generator.TD("Formation").TD(Formation));
            });

            generator.Table("hero-class", () =>
            {
                generator.TR(() =>
                {
                    generator.TD("Equipment");
                    foreach (var type in SlotItems)
                    {
                        generator.TD(() =>
                        {
                            generator.P(type.ToString().SplitCamelCase());
                            var exampleItem = HeroHelpers.AllItems.FirstOrDefault(item => item.IsEquipmentType(type));
                            if (exampleItem != null)
                                generator.Img("equip-img", exampleItem);
                        });
                    }
                });
            });

            if (Mounted)
            {
                generator.Table("hero-class", () =>
                {
                    generator.TR(() =>
                    {
                        generator.TD("Mount");
                        if (UseHorse)
                        {
                            generator.TD(() =>
                            {
                                generator.P("Horse");
                                var exampleItem = HeroHelpers.AllItems
                                    .FirstOrDefault(item 
                                        => item.Type == ItemObject.ItemTypeEnum.Horse
                                           && item.HorseComponent.Monster.FamilyType == (int) EquipHero.MountFamilyType.horse);
                                if (exampleItem != null)
                                    generator.Img("equip-img", exampleItem);
                            });
                        }
                        if (UseCamel)
                        {
                            generator.TD(() =>
                            {
                                generator.P("Camel");
                                var exampleItem = HeroHelpers.AllItems
                                    .FirstOrDefault(item 
                                        => item.Type == ItemObject.ItemTypeEnum.Horse
                                           && item.HorseComponent.Monster.FamilyType == (int) EquipHero.MountFamilyType.camel);
                                if (exampleItem != null)
                                    generator.Img("equip-img", exampleItem);
                            });
                        }
                    });
                });
            }

            generator.Table("hero-class", () =>
            {
                generator.TR(() 
                    => generator.TD("Passive Power").TD(() => PassivePower.GenerateDocumentation(generator)));
                generator.TR(() 
                    => generator.TD("Active Power").TD(() => ActivePower.GenerateDocumentation(generator)));
            });
        }
        #endregion
    }
}