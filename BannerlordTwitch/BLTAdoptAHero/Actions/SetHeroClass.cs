using System;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=pVblweVj}Set Hero Class"),
     LocDescription("{=7VWKYp1U}Allows changing the 'class' of an adopted hero, which affects the equipment and where XP is applied"),
     UsedImplicitly]
    internal class SetHeroClass : HeroActionHandlerBase
    {
        private class Settings : IDocumentable
        {
            [LocDisplayName("{=iLpDEiTq}First Equip Is Free"),
             LocCategory("Costs", "{=r7sc3Tvg}Costs"), 
             LocDescription("{=pRETwmGv}If this is free when the hero does not have a class already (this can only happen if Adopt does NOT specify a default class)"), 
             PropertyOrder(0), UsedImplicitly]
            public bool FirstEquipIsFree { get; set; } = true;
            
            [LocDisplayName("{=N2OnDXOs}Cost Tier 1"),
             LocCategory("Costs", "{=r7sc3Tvg}Costs"), 
             LocDescription("{=tuhth4ld}Gold cost for Tier 1 class change"), 
             PropertyOrder(1), UsedImplicitly]
            public int CostTier1 { get; set; } = 5000;

            [LocDisplayName("{=jIQ5VYSh}Cost Tier 2"),
             LocCategory("Costs", "{=r7sc3Tvg}Costs"), 
             LocDescription("{=V5CGr4lf}Gold cost for Tier 2 class change"), 
             PropertyOrder(2), UsedImplicitly]
            public int CostTier2 { get; set; } = 10000;

            [LocDisplayName("{=lWOrthSS}Cost Tier 3"),
             LocCategory("Costs", "{=r7sc3Tvg}Costs"), 
             LocDescription("{=NlXa6kJS}Gold cost for Tier 3 class change"), 
             PropertyOrder(3), UsedImplicitly]
            public int CostTier3 { get; set; } = 20000;

            [LocDisplayName("{=wuKWOAwt}Cost Tier 4"),
             LocCategory("Costs", "{=r7sc3Tvg}Costs"), 
             LocDescription("{=0X1TRYbP}Gold cost for Tier 4 class change"), 
             PropertyOrder(4), UsedImplicitly]
            public int CostTier4 { get; set; } = 40000;

            [LocDisplayName("{=ZBzqCAyW}Cost Tier 5"),
             LocCategory("Costs", "{=r7sc3Tvg}Costs"), 
             LocDescription("{=yelsar2J}Gold cost for Tier 5 class change"), 
             PropertyOrder(5), UsedImplicitly]
            public int CostTier5 { get; set; } = 80000;

            [LocDisplayName("{=ZLdpLkbg}Cost Tier 6"),
             LocCategory("Costs", "{=r7sc3Tvg}Costs"), 
             LocDescription("{=eYcOsu9h}Gold cost for Tier 6 class change"), 
             PropertyOrder(6), UsedImplicitly]
            public int CostTier6 { get; set; } = 160000;
            
            [LocDisplayName("{=rpkghdTp}Update Equipment"),
             LocCategory("General", "{=C5T5nnix}General"), 
             LocDescription("{=cWQKOYEx}Whether to immediately update equipment after changing class"), 
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
                generator.PropertyValuePair("{=2WAi7pZN}Tier costs".Translate(), 
                    $"1={CostTier1}{Naming.Gold}, 2={CostTier2}{Naming.Gold}, 3={CostTier3}{Naming.Gold}, " +
                    $"4={CostTier4}{Naming.Gold}, 5={CostTier5}{Naming.Gold}, 5={CostTier5}{Naming.Gold}, " +
                    $"6={CostTier6}{Naming.Gold}");
                if (FirstEquipIsFree)
                {
                    generator.P("{=lW1KcoWD}Free if you do not already have a class".Translate());
                }
                if (UpdateEquipment)
                {
                    generator.P("{=qOR4RSqt}Updates equipment to match new class".Translate());
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
                onFailure("{=70vJ1jxZ}You cannot change class, as a mission is active!".Translate());
                return;
            }
            
            var newClass = BLTAdoptAHeroModule.HeroClassConfig.FindClass(context.Args);
            if (newClass == null)
            {
                onFailure("{=prmHmdGE}Provide class name".Translate() +
                          $" {string.Join(" / ", BLTAdoptAHeroModule.HeroClassConfig.ClassNames)}");
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
            onSuccess("{=giUCnkf2}changed class to {ClassName}".Translate(("ClassName", newClass.Name.ToString())));
        }
    }
}