using System;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Allows changing the 'class' of an adopted hero, which affects the equipment and where XP is applied")]
    internal class SetHeroClass : HeroActionHandlerBase
    {
        // Hero class is custom defined by the set of skills they favour.
        
        // These skills then drive the allocation of XP, focus, attributes, and equipment.
        
        // The HeroData should contain a deep copy of the class definition in-case settings are changed later.
        
        // !class can optionally automatically re-roll equipment if the class changes.
        
        // Also add re-roll flag to EquipHero that will only re-roll the current equipment,
        // then create !reequip command / reward with it.

        private class Settings
        {
            [Category("General"), Description("Gold cost to change class"), PropertyOrder(1)]
            public int GoldCost { get; set; }
            
            [Category("General"), Description("Whether to immediately update equipment after changing class"), PropertyOrder(2)]
            public bool UpdateEquipment { get; set; }
            
            // [Category("General"), Description("Whether to immediately re-distribute focus points after changing class"), PropertyOrder(3)]
            // public bool RedistributeFocusPoints { get; set; }
        }

        protected override Type ConfigType => typeof(Settings);

        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
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
                onFailure($"Provide class name {string.Join("/", BLTAdoptAHeroModule.HeroClassConfig.ClassNames)}");
                return;
            }

            int heroGold = BLTAdoptAHeroCampaignBehavior.Get().GetHeroGold(adoptedHero);
            if (heroGold < settings.GoldCost)
            {
                onFailure(Naming.NotEnoughGold(settings.GoldCost, heroGold));
                return;
            }
            
            BLTAdoptAHeroCampaignBehavior.Get().SetClass(adoptedHero, newClass);

            if (settings.GoldCost > 0)
            {
                BLTAdoptAHeroCampaignBehavior.Get().ChangeHeroGold(adoptedHero, -settings.GoldCost);
            }

            if (settings.UpdateEquipment)
            {
                EquipHero.UpgradeEquipment(adoptedHero,
                    BLTAdoptAHeroCampaignBehavior.Get().GetEquipmentTier(adoptedHero), 
                    newClass, keepBetter: true);
                BLTAdoptAHeroCampaignBehavior.Get().SetEquipmentClass(adoptedHero, newClass);
            }
            onSuccess($"changed class to {newClass.Name}");
        }
    }
}