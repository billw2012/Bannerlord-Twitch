using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using BLTAdoptAHero.Powers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    public class HeroClassDef
    {
        [ReadOnly(true), UsedImplicitly]
        public Guid ID { get => ObjectIDRegistry.Get(this); set => ObjectIDRegistry.Set(this, value); }

        #region User Editable Properties
        [Description("Name of the class that shall be passed to SetHeroClass actions"), PropertyOrder(1)]
        public string Name { get; set; } = "Enter Name Here";

        [Description("Which formation to add summoned units to"), PropertyOrder(2), ItemsSource(typeof(SummonHero.FormationItemSource))]
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
         PropertyOrder(9), ExpandableObject]
        public PassivePowerGroup PassivePower { get; set; } = new();

        [Description("Active hero power: this power will be triggered only when the UseHeroPower action is used by " +
                     "the viewer, via reward or command (i.e. a temporary buff)"), PropertyOrder(10), ExpandableObject]
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

        public class PassivePowerGroup
        {
            [Description("The name of the power: how the power will be described in messages"), PropertyOrder(1), UsedImplicitly]
            public string Name { get; set; } = "Passive Power";

            [ItemsSource(typeof(HeroPowerDefBase.ItemSourcePassive)), PropertyOrder(1), UsedImplicitly]
            public Guid Power1 { get; set; }
            [ItemsSource(typeof(HeroPowerDefBase.ItemSourcePassive)), PropertyOrder(2), UsedImplicitly]
            public Guid Power2 { get; set; }
            [ItemsSource(typeof(HeroPowerDefBase.ItemSourcePassive)), PropertyOrder(3), UsedImplicitly]
            public Guid Power3 { get; set; }

            [Browsable(false), YamlIgnore]
            private IEnumerable<IHeroPowerPassive> Powers 
            => new []
            {
                BLTAdoptAHeroModule.HeroPowerConfig.GetPower(Power1) as IHeroPowerPassive, 
                BLTAdoptAHeroModule.HeroPowerConfig.GetPower(Power2) as IHeroPowerPassive, 
                BLTAdoptAHeroModule.HeroPowerConfig.GetPower(Power3) as IHeroPowerPassive,
            }.Where(p => p != null);
            
            public void OnHeroJoinedBattle(Hero hero)
            {
                if (BLTAdoptAHeroModule.HeroPowerConfig.DisablePowersInTournaments && MissionHelpers.InTournament())
                {
                    return;
                }
                foreach(var power in Powers)
                {
                    BLTHeroPowersMissionBehavior.Current.ConfigureHandlers(
                        hero, power as HeroPowerDefBase, handlers => power.OnHeroJoinedBattle(hero, handlers));
                }
            }
        }

        public class ActivePowerGroup
        {
            [Description("The name of the power: how the power will be described in messages"), PropertyOrder(1),
             UsedImplicitly]
            public string Name { get; set; } = "Active Power";

            [ItemsSource(typeof(HeroPowerDefBase.ItemSourceActive)), PropertyOrder(1), UsedImplicitly]
            public Guid Power1 { get; set; }
            [ItemsSource(typeof(HeroPowerDefBase.ItemSourceActive)), PropertyOrder(2), UsedImplicitly]
            public Guid Power2 { get; set; }
            [ItemsSource(typeof(HeroPowerDefBase.ItemSourceActive)), PropertyOrder(3), UsedImplicitly]
            public Guid Power3 { get; set; }

            [Browsable(false), YamlIgnore]
            private IEnumerable<IHeroPowerActive> Powers 
            => new []
            {
                BLTAdoptAHeroModule.HeroPowerConfig.GetPower(Power1) as IHeroPowerActive, 
                BLTAdoptAHeroModule.HeroPowerConfig.GetPower(Power2) as IHeroPowerActive, 
                BLTAdoptAHeroModule.HeroPowerConfig.GetPower(Power3) as IHeroPowerActive
            }.Where(p => p != null);
            
            public bool IsActive(Hero hero) => Powers.Any(power => power.IsActive(hero));

            public (bool canActivate, string failReason) CanActivate(Hero hero)
            {
                if (BLTAdoptAHeroModule.HeroPowerConfig.DisablePowersInTournaments && MissionHelpers.InTournament())
                {
                    return (false, "Not allowed in tournaments!");
                }

                var activePowers = Powers.ToList();

                if (!activePowers.Any())
                {
                    return (false, "You have no powers!");
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
                if (BLTAdoptAHeroModule.HeroPowerConfig.DisablePowersInTournaments && MissionHelpers.InTournament())
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
                        }
                    });
                }
                return (true, $"{Name} activated!");
            }

            public float DurationFractionRemaining(Hero hero)
            {
                if (!Powers.Any()) 
                    return 0;
                return Powers.Max(active => active.DurationFractionRemaining(hero));
            }
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
    }
}