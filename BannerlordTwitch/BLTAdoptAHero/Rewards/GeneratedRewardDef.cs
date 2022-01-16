using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using PropertyChanged;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    [CategoryOrder("Reward Type", 1), 
     CategoryOrder("Reward Tier", 2),
     CategoryOrder("Custom Item", 3)]
    public class GeneratedRewardDef : INotifyPropertyChanged, ICloneable
    {
        #region User Editable
        #region Reward Type
        [LocDisplayName("{=7yNTThry}Weapon Weight"),
         LocCategory("Reward Type", "{=x6pmNHJT}Reward Type"), 
         LocDescription("{=WTWuy57X}Relative proportion of rewards that will be weapons. This includes all one handed, two handed, ranged and ammo."), 
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         Document, PropertyOrder(1), UsedImplicitly]
        public float WeaponWeight { get; set; } = 1f;

        [LocDisplayName("{=Al3Px9lL}Armor Weight"),
         LocCategory("Reward Type", "{=x6pmNHJT}Reward Type"), 
         LocDescription("{=WSXhTI0r}Relative proportion of rewards that will be armor"), 
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         Document, PropertyOrder(2), UsedImplicitly]
        public float ArmorWeight { get; set; } = 1f;

        [LocDisplayName("{=S6t7iUki}Mount Weight"),
         LocCategory("Reward Type", "{=x6pmNHJT}Reward Type"), 
         LocDescription("{=4L1hJS6v}Relative proportion of rewards that will be mounts"), 
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         Document, PropertyOrder(3), UsedImplicitly]
        public float MountWeight { get; set; } = 0.1f;
        
        [LocDisplayName("{=h4CEbGun}Reward Type Chances"),
         LocCategory("Reward Type", "{=x6pmNHJT}Reward Type"),
         LocDescription("{=u9zCErHS}The % chance for each item type"),
         DependsOn(nameof(WeaponWeight), nameof(ArmorWeight), nameof(MountWeight)),
         YamlIgnore, ReadOnly(true),
         PropertyOrder(4), UsedImplicitly] 
        public string RewardTypeChances
        {
            get
            {
                var typeWeights = new[]
                {
                    (name: "{=hpJN9wNo}Weapon".Translate(), weight: WeaponWeight),
                    (name: "{=YQdQrFG7}Armor".Translate(), weight: ArmorWeight),
                    (name: "{=HGZVsIjn}Mount".Translate(), weight: MountWeight),
                };
                float totalWeight = typeWeights.Sum(t => t.weight);
                return string.Join(", ",
                    typeWeights.Where(t => t.weight > 0)
                        .Select(t => $"{t.name}: {t.weight * 100f / totalWeight:0}%"));
            }
        }
        #endregion

        #region Reward Tier
        [LocDisplayName("{=pqDOv12p}Tier 1 Weight"),
         LocCategory("Reward Tier", "{=Mmd0UleC}Reward Tier"), 
         LocDescription("{=aAZvoxk9}Relative proportion of rewards that will be Tier 1"),
         Range(0, 5), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         Document, UsedImplicitly, PropertyOrder(1)]
        public float Tier1Weight { get; set; }

        [LocDisplayName("{=ZDgMrJHH}Tier 2 Weight"),
         LocCategory("Reward Tier", "{=Mmd0UleC}Reward Tier"), 
         LocDescription("{=rvjbuCH4}Relative proportion of rewards that will be Tier 2"),
         Range(0, 5), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         Document, PropertyOrder(2), UsedImplicitly]
        public float Tier2Weight { get; set; }

        [LocDisplayName("{=8UhZyGRj}Tier 3 Weight"),
         LocCategory("Reward Tier", "{=Mmd0UleC}Reward Tier"), 
         LocDescription("{=CJwXH8cz}Relative proportion of rewards that will be Tier 3"),
         Range(0, 5), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         Document, UsedImplicitly, PropertyOrder(3)]
        public float Tier3Weight { get; set; }

        [LocDisplayName("{=v4wqC1mI}Tier 4 Weight"),
         LocCategory("Reward Tier", "{=Mmd0UleC}Reward Tier"), 
         LocDescription("{=NoKi0iIC}Relative proportion of rewards that will be Tier 4"),
         Range(0, 5), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         Document, PropertyOrder(4), UsedImplicitly]
        public float Tier4Weight { get; set; }

        [LocDisplayName("{=vXimyLH7}Tier 5 Weight"),
         LocCategory("Reward Tier", "{=Mmd0UleC}Reward Tier"), 
         LocDescription("{=rsGLtLyB}Relative proportion of rewards that will be Tier 5"),
         Range(0, 5), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         Document, PropertyOrder(5), UsedImplicitly]
        public float Tier5Weight { get; set; } = 3f;

        [LocDisplayName("{=HWHFIsUH}Tier 6 Weight"),
         LocCategory("Reward Tier", "{=Mmd0UleC}Reward Tier"), 
         LocDescription("{=991D0fok}Relative proportion of rewards that will be Tier 6"),
         Range(0, 5), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         Document, PropertyOrder(6), UsedImplicitly]
        public float Tier6Weight { get; set; } = 2f;

        [LocDisplayName("{=R5TCrBo8}Custom Weight"),
         LocCategory("Reward Tier", "{=Mmd0UleC}Reward Tier"),
         LocDescription("{=XL94fx52}Relative proportion of rewards that will be Custom (Tier 6 with modifiers as per the Custom Reward settings below)"), 
         Range(0, 5), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         Document, PropertyOrder(7), UsedImplicitly]
        public float CustomWeight { get; set; } = 1f;
        
        [LocDisplayName("{=zoBVNJBB}Reward Tier Chances"),
         LocCategory("Reward Tier", "{=Mmd0UleC}Reward Tier"),
         LocDescription("{=ANPeZyy0}The % chance for each reward tier"),
         DependsOn(nameof(Tier1Weight), nameof(Tier2Weight), nameof(Tier3Weight), 
             nameof(Tier4Weight), nameof(Tier5Weight), nameof(Tier6Weight), nameof(CustomWeight)),
         YamlIgnore, ReadOnly(true), 
         PropertyOrder(8), UsedImplicitly] 
        public string RewardTierChances
        {
            get
            {
                float totalWeight = TierWeights.Select(o => o.weight).Sum();
                return string.Join(", ",
                    TierWeights.Where(t => t.weight > 0)
                        .Select(t => 
                            (t.tier == 6 
                                ? "{=LKgozyEk}Custom".Translate() 
                                : "{=JpB20FtS}Tier {Tier}".Translate(("Tier", t.tier + 1))) 
                            + $": {t.weight * 100f / totalWeight:0}%"));
            }
        }

        [Browsable(false), YamlIgnore]
        public IEnumerable<(int tier, float weight)> TierWeights
        {
            get
            {
                yield return (tier: 0, weight: Tier1Weight);
                yield return (tier: 1, weight: Tier2Weight);
                yield return (tier: 2, weight: Tier3Weight);
                yield return (tier: 3, weight: Tier4Weight);
                yield return (tier: 4, weight: Tier5Weight);
                yield return (tier: 5, weight: Tier6Weight);
                yield return (tier: 6, weight: CustomWeight);
            }
        }
        #endregion

        #region Custom Item
        [LocDisplayName("{=PBWEbYLT}Custom Item Name"),
         LocCategory("Custom Item", "{=zVqTKyQG}Custom Item"), 
         LocDescription("{=vqNeCCNy}Name format for custom item, {ITEMNAME} is the placeholder for the base item name."),
         PropertyOrder(1), UsedImplicitly]
        public string CustomItemName { get; set; } = "{=W47g8bCB}Reward {ITEMNAME}";

        [LocDisplayName("{=THpMPPhd}Custom Item Power"),
         LocCategory("Custom Item", "{=zVqTKyQG}Custom Item"), 
         LocDescription("{=anUzQaU7}Custom item power multipler, applies on top of the global multiplier"),
         Range(0, 5), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         PropertyOrder(2), UsedImplicitly]
        public float CustomItemPower { get; set; } = 1f;
        #endregion
        #endregion

        #region Public Interface

        public override string ToString()
        {
            return $"{RewardTypeChances} {RewardTierChances}"
                + (CustomWeight > 0 ? $" {CustomItemName} ({CustomItemPower}x)" : "");
        }

        [Browsable(false), YamlIgnore]
        private IEnumerable<(RewardHelpers.RewardType type, float weight)> RewardTypeWeights {
            get
            {
                yield return (type: RewardHelpers.RewardType.Weapon, weight: WeaponWeight);
                yield return (type: RewardHelpers.RewardType.Armor, weight: ArmorWeight);
                yield return (type: RewardHelpers.RewardType.Mount, weight: MountWeight);
            }
        }

        public (ItemObject item, ItemModifier modifier, EquipmentIndex slot) Generate(Hero hero)
        {
            var heroClass = BLTAdoptAHeroCampaignBehavior.Current.GetClass(hero);

            // Randomize the reward tier order, by random weighting
            var tiers = TierWeights
                .OrderRandomWeighted(tier => tier.weight)
                .ToList();
            //int tier = RewardTierWeights.SelectRandomWeighted(t => t.weight).tier;
            bool shouldUseHorse = EquipHero.HeroShouldUseHorse(hero, heroClass);

            (ItemObject item, ItemModifier modifier, EquipmentIndex slot) 
                GenerateRandomWeightedReward(bool allowDuplicates)
            {
                return RewardTypeWeights
                    // Exclude mount when it shouldn't be used by the hero or they already have a tournament reward
                    // horse
                    .Where(p => shouldUseHorse || p.type != RewardHelpers.RewardType.Mount)
                    // Randomize the reward type order, by random weighting
                    .OrderRandomWeighted(type => type.weight)
                    .SelectMany(type => 
                        tiers.Select(tier 
                            => RewardHelpers.GenerateRewardType(type.type, tier.tier, hero, heroClass, 
                                allowDuplicates, BLTAdoptAHeroModule.CommonConfig.CustomRewardModifiers,
                                CustomItemName, CustomItemPower)))
                    .FirstOrDefault(i => i != default);
            }

            var reward = GenerateRandomWeightedReward(allowDuplicates: false);

            // If we couldn't find a unique one that the hero can use, then generate a non-unique one, they can
            // sell it if they want
            if (reward == default)
            {
                reward = GenerateRandomWeightedReward(allowDuplicates: true);
            }

            return reward;
        }
        #endregion
        
        #region IClonable
        public object Clone() => CloneHelpers.CloneProperties(this);
        #endregion
        
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}