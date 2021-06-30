using System;
using System.Collections.Generic;
using System.ComponentModel;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    public abstract class DurationMissionHeroPowerDefBase : HeroPowerDefBase, IHeroPowerActive
    {
        [Category("Power Config"), Description("Duration the power will last for (when used as an active power), in seconds"), PropertyOrder(0), UsedImplicitly]
        public float PowerDurationSeconds { get; set; } = 30f;

        (bool canActivate, string failReason) IHeroPowerActive.CanActivate(Hero hero)
        {
            if (Mission.Current == null)
            {
                return (false, "No mission is active!");
            }
            return ((IHeroPowerActive) this).IsActive(hero) 
                ? (false, "Already active!") 
                : (true, null);
        }

        bool IHeroPowerActive.IsActive(Hero hero) => BLTHeroPowersMissionBehavior.Current?.HasHandlers(hero, this) == true;

        private readonly Dictionary<Hero, float> expiry = new();

        void IHeroPowerActive.Activate(Hero hero, Action expiryCallback)
        {
            expiry[hero] = MBCommon.GetTime(MBCommon.TimeType.Mission) + PowerDurationSeconds;
            BLTHeroPowersMissionBehavior.Current.ConfigureHandlers(hero, this, handlers =>
            {
                handlers.OnSlowTick += (_, _) =>
                {
                    if (MBCommon.GetTime(MBCommon.TimeType.Mission) > expiry[hero])
                    {
                        BLTHeroPowersMissionBehavior.Current.ClearHandlers(hero, this);
                        expiryCallback();
                    }
                };
                handlers.OnGotKilled += (_, _, _, _, _, _) =>
                {
                    // Expire immediately
                    BLTHeroPowersMissionBehavior.Current.ClearHandlers(hero, this);
                    expiry[hero] = 0;
                    expiryCallback();
                };
                OnActivation(hero, handlers);
            });
        }

        float IHeroPowerActive.DurationFractionRemaining(Hero hero)
        {
            if (!expiry.TryGetValue(hero, out float expiryVal))
            {
                return 0;
            }

            return Math.Max(0, expiryVal - MBCommon.GetTime(MBCommon.TimeType.Mission)) / PowerDurationSeconds;
        }

        protected abstract void OnActivation(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers);
    }
}