using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=tLSFX9Xc}Retinue"),
     LocDescription("{=bhC3VcmU}Add and improve adopted heroes retinue"), 
     UsedImplicitly]
    public class Retinue : ActionHandlerBase
    {
        private class Settings : IDocumentable
        {
            [LocDisplayName("{=tLSFX9Xc}Retinue"),
             LocDescription("{=iNoFrKsN}Retinue Upgrade Settings"), 
             PropertyOrder(1), ExpandableObject, Expand, UsedImplicitly]
            public BLTAdoptAHeroCampaignBehavior.RetinueSettings Retinue { get; set; } = new();

            [LocDisplayName("{=nIsuuFMC}All By Default"),
             LocDescription("{=mJSGvWlR}Whether this action should attempt to buy/upgrade as many times as possible when called with no parameter."), 
             PropertyOrder(2), UsedImplicitly]
            public bool AllByDefault { get; set; } = true;
            
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
                onFailure("{=mCcpMwrN}You cannot upgrade retinue, as a mission is active!".Translate());
                return;
            }

            int numToUpgrade = settings.AllByDefault ? int.MaxValue : 1;
            if (!string.IsNullOrEmpty(context.Args))
            {
                if (string.Compare(context.Args, 
                    "{=hHekZwYB}all".Translate(), StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    numToUpgrade = int.MaxValue;
                }
                else if (!int.TryParse(context.Args, out numToUpgrade) || numToUpgrade <= 0)
                {
                    onFailure(context.ArgsErrorMessage("{=NexXxYvj}(number, or all)".Translate()));
                    return;
                }
            }

            (bool success, string status) = BLTAdoptAHeroCampaignBehavior.Current
                .UpgradeRetinue(adoptedHero, settings.Retinue, numToUpgrade);
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