using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Allows changing the 'class' of an adopted hero, which affects the equipment and where XP is applied")]
    internal class SetHeroClass : HeroActionHandlerBase
    {
        private class Settings : IDocumentable
        {
            [Category("Costs"), 
             Description("If this is free when the hero does not have a class already (this can only happen if Adopt " +
                         "does NOT specify a default class)"), 
             PropertyOrder(0), UsedImplicitly]
            public bool FirstEquipIsFree { get; set; } = true;
            
            [Category("Costs"), Description("Gold cost for Tier 1 class change"), PropertyOrder(1), UsedImplicitly]
            public int CostTier1 { get; set; } = 5000;

            [Category("Costs"), Description("Gold cost for Tier 2 class change"), PropertyOrder(2), UsedImplicitly]
            public int CostTier2 { get; set; } = 10000;

            [Category("Costs"), Description("Gold cost for Tier 3 class change"), PropertyOrder(3), UsedImplicitly]
            public int CostTier3 { get; set; } = 20000;

            [Category("Costs"), Description("Gold cost for Tier 4 class change"), PropertyOrder(4), UsedImplicitly]
            public int CostTier4 { get; set; } = 40000;

            [Category("Costs"), Description("Gold cost for Tier 5 class change"), PropertyOrder(5), UsedImplicitly]
            public int CostTier5 { get; set; } = 80000;

            [Category("Costs"), Description("Gold cost for Tier 6 class change"), PropertyOrder(6), UsedImplicitly]
            public int CostTier6 { get; set; } = 160000;
            
            [Category("General"), 
             Description("Whether to immediately update equipment after changing class"), 
             PropertyOrder(7), UsedImplicitly]
            public bool UpdateEquipment { get; set; }

            public int GetTierCost(int tier)
            {
                return tier switch
                {
                    0 => CostTier1,
                    1 => CostTier2,
                    2 => CostTier3,
                    3 => CostTier4,
                    4 => CostTier5,
                    5 => CostTier6,
                    _ => CostTier6
                };
            }
            
            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                generator.PropertyValuePair("Tier costs", 
                    $"1={CostTier1}{Naming.Gold}, 2={CostTier2}{Naming.Gold}, 3={CostTier3}{Naming.Gold}, " +
                    $"4={CostTier4}{Naming.Gold}, 5={CostTier5}{Naming.Gold}, 5={CostTier5}{Naming.Gold}, " +
                    $"6={CostTier6}{Naming.Gold}");
                if (FirstEquipIsFree)
                {
                    generator.P($"Free if you do not already have a class");
                }
                if (UpdateEquipment)
                {
                    generator.P($"Updates equipment to match new class");
                }
            }
        }

        protected override Type ConfigType => typeof(Settings);

        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config, 
            Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Settings) config;
            
            if (Mission.Current != null)
            {
                onFailure($"You cannot change class, as a mission is active!");
                return;
            }
            
            var newClass = BLTAdoptAHeroModule.HeroClassConfig.FindClass(context.Args);
            if (newClass == null)
            {
                onFailure($"Provide class name {string.Join(" / ", BLTAdoptAHeroModule.HeroClassConfig.ClassNames)}");
                return;
            }

            int equipmentTier = BLTAdoptAHeroCampaignBehavior.Current.GetEquipmentTier(adoptedHero);

            int cost = settings.FirstEquipIsFree && adoptedHero.GetClass() == null || equipmentTier < 0
                ? 0
                : settings.GetTierCost(equipmentTier);

            int heroGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(adoptedHero);
            if (heroGold < cost)
            {
                onFailure(Naming.NotEnoughGold(cost, heroGold));
                return;
            }
            
            BLTAdoptAHeroCampaignBehavior.Current.SetClass(adoptedHero, newClass);

            if (cost > 0)
            {
                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(adoptedHero, -cost);
            }

            if (settings.UpdateEquipment && equipmentTier >= 0)
            {
                EquipHero.UpgradeEquipment(adoptedHero, equipmentTier, newClass, replaceSameTier: false);
                BLTAdoptAHeroCampaignBehavior.Current.SetEquipmentClass(adoptedHero, newClass);
            }
            onSuccess($"changed class to {newClass.Name}");
        }
    }
}