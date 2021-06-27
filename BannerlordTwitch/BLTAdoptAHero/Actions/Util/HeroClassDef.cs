using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    public class HeroClassDef : IHeroPowerPassive
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
         ItemsSource(typeof(HeroPowerDefBase.ItemSourcePassive)), PropertyOrder(9), UsedImplicitly]
        public Guid PassivePower1 { get; set; }

        [Description("Passive hero power: this will always apply to the hero (i.e. a permanent buff)"), 
         ItemsSource(typeof(HeroPowerDefBase.ItemSourcePassive)), PropertyOrder(10), UsedImplicitly]
        public Guid PassivePower2 { get; set; }
        
        [Description("Passive hero power: this will always apply to the hero (i.e. a permanent buff)"), 
         ItemsSource(typeof(HeroPowerDefBase.ItemSourcePassive)), PropertyOrder(11), UsedImplicitly]
        public Guid PassivePower3 { get; set; }
        
        [Browsable(false), YamlIgnore, UsedImplicitly]
        public IEnumerable<IHeroPowerPassive> PassivePowers 
            => new []
            {
                BLTAdoptAHeroModule.HeroPowerConfig.GetPower(PassivePower1) as IHeroPowerPassive, 
                BLTAdoptAHeroModule.HeroPowerConfig.GetPower(PassivePower2) as IHeroPowerPassive, 
                BLTAdoptAHeroModule.HeroPowerConfig.GetPower(PassivePower3) as IHeroPowerPassive
            }.Where(p => p != null);

        [Description("Active hero power: this power will be triggered only when the UseHeroPower action is used by " +
                     "the viewer, via reward or command (i.e. a temporary buff)"), 
         ItemsSource(typeof(HeroPowerDefBase.ItemSourceActive)), PropertyOrder(12), UsedImplicitly]
        public Guid ActivePower1 { get; set; }
        [Description("Active hero power: this power will be triggered only when the UseHeroPower action is used by " +
                     "the viewer, via reward or command (i.e. a temporary buff)"), 
         ItemsSource(typeof(HeroPowerDefBase.ItemSourceActive)), PropertyOrder(13), UsedImplicitly]
        public Guid ActivePower2 { get; set; }
        [Description("Active hero power: this power will be triggered only when the UseHeroPower action is used by " +
                     "the viewer, via reward or command (i.e. a temporary buff)"), 
         ItemsSource(typeof(HeroPowerDefBase.ItemSourceActive)), PropertyOrder(14), UsedImplicitly]
        public Guid ActivePower3 { get; set; }
        
        [Browsable(false), YamlIgnore]
        public IEnumerable<IHeroPowerActive> ActivePowers 
            => new []
            {
                BLTAdoptAHeroModule.HeroPowerConfig.GetPower(ActivePower1) as IHeroPowerActive, 
                BLTAdoptAHeroModule.HeroPowerConfig.GetPower(ActivePower2) as IHeroPowerActive, 
                BLTAdoptAHeroModule.HeroPowerConfig.GetPower(ActivePower3) as IHeroPowerActive
            }.Where(p => p != null);
        #endregion

        [YamlIgnore, Browsable(false)]
        public IEnumerable<EquipmentType> Slots { get { yield return Slot1; yield return Slot2; yield return Slot3; yield return Slot4; } }
        
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

        public override string ToString() => $"{Name} : {string.Join(", ", SlotItems.Select(s => s.ToString()))}" 
                                             + (UseHorse ? " (Use Horse)" : "")
                                             + (UseCamel ? " (Use Camel)" : "");
        
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

        #region IHeroPowerPassive
        private void ApplyPassivePower(Action<IHeroPowerPassive> fn)
        {
            if (BLTAdoptAHeroModule.HeroPowerConfig.DisablePowersInTournaments
                && MissionHelpers.InTournament())
            {
                return;
            }
            foreach(var power in PassivePowers)
            {
                fn(power);
            }
        }
       
        void IHeroPowerPassive.OnAdded(Hero hero) => ApplyPassivePower(power => power.OnAdded(hero));
        void IHeroPowerPassive.OnRemoved(Hero hero) => ApplyPassivePower(power => power.OnRemoved(hero));

        void IHeroPowerPassive.OnBattleStart(Hero hero) => ApplyPassivePower(power => power.OnBattleStart(hero));
        void IHeroPowerPassive.OnBattleTick(Hero hero, Agent agent) => ApplyPassivePower(power => power.OnBattleTick(hero, agent));
        void IHeroPowerPassive.OnBattleEnd(Hero hero) => ApplyPassivePower(power => power.OnBattleEnd(hero));
        void IHeroPowerPassive.OnAgentBuild(Hero hero, Agent agent) => ApplyPassivePower(power => power.OnAgentBuild(hero, agent));
        void IHeroPowerPassive.OnAgentKilled(Hero hero, Agent agent, Hero killerHero, Agent killerAgent) => ApplyPassivePower(power => power.OnAgentKilled(hero, agent, killerHero, killerAgent));
        void IHeroPowerPassive.OnDoDamage(Hero hero, Agent agent, Hero victimHero, Agent victimAgent, ref AttackCollisionData attackCollisionData) 
        {
            if (BLTAdoptAHeroModule.HeroPowerConfig.DisablePowersInTournaments && MissionHelpers.InTournament())
            {
                return;
            }

            // Can't use ref parameter in a lambda so have to do the apply loop directly
            foreach(var power in PassivePowers)
            {
                power.OnDoDamage(hero, agent, victimHero, victimAgent, ref attackCollisionData);
            }
        }
        void IHeroPowerPassive.OnTakeDamage(Hero hero, Agent agent, Hero attackerHero, Agent attackerAgent, ref AttackCollisionData attackCollisionData) 
        {
            if (BLTAdoptAHeroModule.HeroPowerConfig.DisablePowersInTournaments && MissionHelpers.InTournament())
            {
                return;
            }

            // Can't use ref parameter in a lambda so have to do the apply loop directly
            foreach(var power in PassivePowers)
            {
                power.OnTakeDamage(hero, agent, attackerHero, attackerAgent, ref attackCollisionData);
            }
        }

        #endregion
    }
}