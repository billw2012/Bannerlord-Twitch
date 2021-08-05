using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Add and improve adopted heroes retinue")]
    public class Retinue : ActionHandlerBase
    {
        private class Settings : IDocumentable
        {
            [Description("Retinue Upgrade Settings"), PropertyOrder(1), ExpandableObject, Expand, UsedImplicitly]
            public BLTAdoptAHeroCampaignBehavior.RetinueSettings Retinue { get; set; } = new();
            
            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                Retinue.GenerateDocumentation(generator);
            }
        }

        protected override Type ConfigType => typeof(Settings);
        
        protected override void ExecuteInternal(ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Settings)config;
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(AdoptAHero.NoHeroMessage);
                return;
            }
            
            if (Mission.Current != null)
            {
                onFailure($"You cannot upgrade retinue, as a mission is active!");
                return;
            }

            (bool success, string status) = BLTAdoptAHeroCampaignBehavior.Current.UpgradeRetinue(adoptedHero, settings.Retinue);
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