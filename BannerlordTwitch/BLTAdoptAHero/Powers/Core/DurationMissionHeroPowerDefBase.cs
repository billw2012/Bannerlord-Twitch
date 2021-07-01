using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch.Helpers;
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

        [Category("Power Config"), Description("Effects to apply to the agent while the power is active"), PropertyOrder(1), UsedImplicitly]
        public List<ParticleEffectDef> Pfx { get; set; } = new();
        
        (bool canActivate, string failReason) IHeroPowerActive.CanActivate(Hero hero)
        {
            if (Mission.Current == null)
            {
                return (false, "No mission is active!");
            }

            if (RequiresHeroAgent && hero.GetAgent() == null)
            {
                return (false, "Your hero is not alive!");
            }
            return ((IHeroPowerActive) this).IsActive(hero) 
                ? (false, "Already active!") 
                : (true, null);
        }

        bool IHeroPowerActive.IsActive(Hero hero) 
            => BLTHeroPowersMissionBehavior.Current?.HasHandlers(hero, this) == true;

        private readonly Dictionary<Hero, float> expiry = new();

        protected class DeactivationHandler
        {
            public event Action<Hero> OnDeactivate;
            public void Deactivate(Hero hero)
            {
                OnDeactivate?.Invoke(hero);
            }
        }

        void IHeroPowerActive.Activate(Hero hero, Action expiryCallback)
        {
            expiry[hero] = MBCommon.GetTime(MBCommon.TimeType.Mission) + PowerDurationSeconds;
            
            var agent = hero.GetAgent();
            var pfx = agent == null ? null : new AgentPfx(agent, Pfx);
            
            pfx?.Start();
            BLTHeroPowersMissionBehavior.Current.ConfigureHandlers(hero, this, handlers =>
            {
                var deactivationHandler = new DeactivationHandler();
                handlers.OnSlowTick += (_, _) =>
                {
                    if (MBCommon.GetTime(MBCommon.TimeType.Mission) > expiry[hero])
                    {
                        BLTHeroPowersMissionBehavior.Current.ClearHandlers(hero, this);
                        pfx?.Stop();
                        expiryCallback();
                        deactivationHandler.Deactivate(hero);
                    }
                };
                handlers.OnGotKilled += (_, _, _, _, _, _) =>
                {
                    // Expire immediately
                    BLTHeroPowersMissionBehavior.Current.ClearHandlers(hero, this);
                    expiry[hero] = 0;
                    pfx?.Stop();
                    expiryCallback();
                    deactivationHandler.Deactivate(hero);
                };
                OnActivation(hero, handlers, agent, deactivationHandler);
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

        protected abstract void OnActivation(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null);

        protected virtual bool RequiresHeroAgent => false;
    }
}