using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Add and improve adopted heroes retinue")]
    public class Retinue : ActionHandlerBase
    {
        private class Settings
        {
            [Description("Retinue Upgrade Settings"), PropertyOrder(1), ExpandableObject]
            public BLTAdoptAHeroCampaignBehavior.RetinueSettings Retinue { get; set; } = new();
        }

        protected override Type ConfigType => typeof(Settings);
        
        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Settings)config;
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
                return;
            }
            
            if (Mission.Current != null)
            {
                onFailure($"You cannot upgrade retinue, as a mission is active!");
                return;
            }

            (bool success, string status) = BLTAdoptAHeroCampaignBehavior.Get().UpgradeRetinue(adoptedHero, settings.Retinue);
            if (success)
            {
                onSuccess(status);
            }
            else
            {
                onFailure(status);
            }
        }
    }
}