using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
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
    [LocDisplayName("{=ph50k2xB}Class")]
    public sealed class HeroClassDef : IDocumentable, ICloneable
    {
        [ReadOnly(true), UsedImplicitly]
        public Guid ID { get; set; } = Guid.NewGuid();

        #region User Editable Properties
        [LocDisplayName("{=iqYaiOhP}Class Definition"),
         LocDescription("{=XkL6MNLl}Whether this class is enabled for use in the game or not"), 
         PropertyOrder(0), UsedImplicitly]
        public bool Enabled { get; set; } = true;
        
        [LocDisplayName("{=nfYBftnO}Name"),
         LocDescription("{=U9PyN7eb}Name of the class that shall be passed to SetHeroClass actions"), 
         InstanceName, 
         PropertyOrder(1), UsedImplicitly]
        public LocString Name { get; set; } = "{=aQgYs3mI}Enter Name Here";
        
        [LocDisplayName("{=rZRKLuGI}Description"),
         LocDescription("{=yRN3Jfwv}Description of the class (used in documentation)"), 
         PropertyOrder(1), UsedImplicitly]
        public LocString Description { get; set; } = string.Empty;

        [LocDisplayName("{=zy3uK94p}Formation"),
         LocDescription("{=1kEMFWLD}Which formation to add summoned units to"), 
         PropertyOrder(2), 
         ItemsSource(typeof(SummonHero.FormationItemSource)), UsedImplicitly]
        public string Formation { get; set; } = "LightCavalry";
        
        [LocDisplayName("{=48tkMBhb}Slot 1"),
         LocDescription("{=kpDE6URE}Item type to put in slot 1"), 
         ItemsSource(typeof(EquipmentTypeItemSource)),
         PropertyOrder(3), UsedImplicitly]
        public EquipmentType Slot1 { get; set; }
        
        [LocDisplayName("{=vXtJLZlw}Slot 2"),
         LocDescription("{=G5AN3s2D}Item type to put in slot 2"), 
         ItemsSource(typeof(EquipmentTypeItemSource)),
         PropertyOrder(4), UsedImplicitly]
        public EquipmentType Slot2 { get; set; }
        
        [LocDisplayName("{=B4LLuesN}Slot 3"),
         LocDescription("{=clxxPb9O}Item type to put in slot 3"), 
         ItemsSource(typeof(EquipmentTypeItemSource)),
         PropertyOrder(5), UsedImplicitly]
        public EquipmentType Slot3 { get; set; }
        
        [LocDisplayName("{=h9PK7GnU}Slot 4"),
         LocDescription("{=EUzbiqh5}Item type to put in slot 4"), 
         ItemsSource(typeof(EquipmentTypeItemSource)),
         PropertyOrder(6), UsedImplicitly]
        public EquipmentType Slot4 { get; set; }
        
        [LocDisplayName("{=MdYmGuin}Use Horse"),
         LocDescription("{=Q00NX1J9}Whether to allow horse (can be combined with Use Camel)"), 
         PropertyOrder(7), UsedImplicitly]
        public bool UseHorse { get; set; }
        
        [LocDisplayName("{=1YsQ2fC3}Use Camel"),
         LocDescription("{=Us7G1v2T}Whether to allow camel (can be combined with Use Horse"), 
         PropertyOrder(8), UsedImplicitly]
        public bool UseCamel { get; set; }
        
        [LocDisplayName("{=MvddFKo4}Passive Power"),
         LocDescription("{=F8a2nXYo}Passive hero power: this will always apply to the hero (i.e. a permanent buff)"), 
         PropertyOrder(9), ExpandableObject, Expand, UsedImplicitly]
        public PassivePowerGroup PassivePower { get; set; } = new() { Name = "{=MvddFKo4}Passive Power" };

        [LocDisplayName("{=wdCMNOGd}Active Power"),
         LocDescription("{=I4ASwveG}Active hero power: this power will be triggered only when the UseHeroPower action is used by the viewer, via reward or command (i.e. a temporary buff)"), 
         PropertyOrder(10), ExpandableObject, Expand, UsedImplicitly]
        public ActivePowerGroup ActivePower { get; set; } = new() { Name = "{=wdCMNOGd}Active Power" };
        #endregion

        #region Public Interface
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
        
        // For UI
        [YamlIgnore, Browsable(false)]
        public IEnumerable<string> SlotItemNames 
            => SlotItems.Select(s => s.GetDisplayName());
        // For UI
        [YamlIgnore, Browsable(false)]
        public string MountDescription 
            => (UseHorse ? "{=YzIcRgBV}Horse".Translate() : "") +
               (UseHorse && UseCamel ? "/" : "") +
               (UseCamel ? "{=HMclWXR8}Camel".Translate() : "");

        [YamlIgnore, Browsable(false)]
        public IEnumerable<EquipmentType> Weapons 
            => Slots.Where(s => s is not (EquipmentType.None or EquipmentType.Shield));
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<(EquipmentIndex index, EquipmentType type)> IndexedWeapons 
            => IndexedSlots.Where(s => s.type is not (EquipmentType.None or EquipmentType.Shield));

        [YamlIgnore, Browsable(false)]
        public bool Mounted => UseHorse || UseCamel;
        
        [YamlIgnore, Browsable(false)]
        public IEnumerable<SkillObject> WeaponSkills =>
            SkillGroup.GetSkillsForEquipmentType(SlotItems)
                .Where(s => s != SkillsEnum.None)
                .Select(SkillGroup.GetSkill)
                .Distinct();

        [YamlIgnore, Browsable(false)]
        public IEnumerable<SkillObject> Skills
        {
            get
            {
                foreach (var skill in WeaponSkills)
                {
                    yield return skill;
                }

                if (Mounted)
                {
                    yield return DefaultSkills.Riding;
                }

                // Everyone gets athletics
                yield return DefaultSkills.Athletics;
            }
        }

        public override string ToString() => 
            $"{Name} : {string.Join(", ", SlotItems.Select(s => s.ToString()))}"
            + (UseHorse ? " (" + "{=A1G6bq0G}Use Horse".Translate() + ")" : "")
            + (UseCamel ? " (" + "{=FpgyZk0F}Use Camel".Translate() + ")" : "");
        
        #endregion
        
        #region ICloneable
        public object Clone()
        {
            var newObj = CloneHelpers.CloneProperties(this);
            newObj.ID = Guid.NewGuid();
            return newObj;
        }
        #endregion

        #region ItemSource
        public class ItemSource : IItemsSource
        {
            public ItemCollection GetValues()
            {
                var col = new ItemCollection {{Guid.Empty, "{=GFc6JLJ9}(none)".Translate()}};

                var source = GlobalHeroClassConfig.Get(ConfigureContext.CurrentlyEditedSettings);
                if (source != null)
                {
                    foreach (var item in source.ClassDefs)
                    {
                        col.Add(item.ID, item.Name.ToString());
                    }
                }

                return col;
            }
        }
        #endregion

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.P(!LocString.IsNullOrEmpty(Description) 
                ? Description.ToString() 
                : "{=A79HrgZ0}Details".Translate());

            generator.Table("hero-class-tiers", () =>
            {
                generator.TR(() =>
                {
                    for (int i = 1; i <= 6; i++)
                    {
                        generator.TH("{=Rs1XNM9K}Tier {TierNumber}".Translate(("TierNumber", i)));
                    }
                });
                generator.TR(() =>
                {
                    for (int i = 1; i <= 6; i++)
                    {
                        EquipHero.UpgradeEquipment(Hero.MainHero, i, this, replaceSameTier: false, customKeepFilter: _ => false);
                        generator.TD(() =>
                            generator.Img(CharacterCode.CreateFrom(Hero.MainHero.CharacterObject),
                                $"{Name} Tier {i} Example")
                        );
                    }
                });
            });

            generator.Table("hero-class", () =>
            {
                generator.TR(() => generator
                    .TD("{=zy3uK94p}Formation".Translate())
                    .TD(SummonHero.FormationItemSource.GetFriendlyName(Formation)));
            });

            generator.Table("hero-class", () =>
            {
                generator.TR(() =>
                {
                    generator.TD("{=HyXJQJtW}Equipment".Translate());
                    foreach (var type in SlotItems)
                    {
                        generator.TD(() =>
                        {
                            generator.P(EquipmentTypeItemSource.GetFriendlyName(type));
                            var exampleItem = CampaignHelpers.AllItems.FirstOrDefault(item => item.IsEquipmentType(type));
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
                        generator.TD("{=iGdq37PI}Mount".Translate());
                        if (UseHorse)
                        {
                            generator.TD(() =>
                            {
                                generator.P("{=YzIcRgBV}Horse".Translate());
                                var exampleItem = CampaignHelpers.AllItems
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
                                generator.P("{=HMclWXR8}Camel".Translate());
                                var exampleItem = CampaignHelpers.AllItems
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
                    => generator.TD("{=MvddFKo4}Passive Power".Translate()).TD(() => PassivePower.GenerateDocumentation(generator)));
                generator.TR(() 
                    => generator.TD("{=wdCMNOGd}Active Power".Translate()).TD(() => ActivePower.GenerateDocumentation(generator)));
            });
        }
        #endregion
    }
}