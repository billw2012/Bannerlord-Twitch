using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.UI;
using BLTAdoptAHero.Annotations;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    /// <summary>
    /// Derive from this to implement Active Hero powers that work in missions, with a fixed duration. You can still
    /// also implement the passive power when you derive from this class.
    /// </summary>
    [CategoryOrder("Power Config", 2)]
    public abstract class DurationMissionHeroPowerDefBase : HeroPowerDefBase, IHeroPowerActive
    {
        #region User Editable
        [Category("Power Config"), 
         Description("Duration the power will last for (when used as an active power), in seconds"), 
         UIRangeAttribute(0, 300, 5),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(0), UsedImplicitly]
        public float PowerDurationSeconds { get; set; } = 30f;

        [Category("Power Config"), Description("Effects to apply to the agent while the power is active"), 
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(1), UsedImplicitly]
        public ObservableCollection<ParticleEffectDef> Pfx { get; set; } = new();
        #endregion
        
        #region IHeroPowerActive
        (bool canActivate, string failReason) IHeroPowerActive.CanActivate(Hero hero)
        {
            if (Mission.Current == null)
            {
                return (false, "No mission is active!");
            }

            if (!Mission.Current.IsLoadingFinished
                || Mission.Current.CurrentState != Mission.State.Continuing
                || Mission.Current?.GetMissionBehaviour<TournamentFightMissionController>() != null &&
                Mission.Current.Mode != MissionMode.Battle)
            {
                return (false, "Mission has not started yet!");
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
            => BLTHeroPowersMissionBehavior.PowerHandler?.HasHandlers(hero, this) == true;

        void IHeroPowerActive.Activate(Hero hero, Action expiryCallback)
        {
            expiry[hero] = MBCommon.GetTime(MBCommon.TimeType.Mission) + PowerDurationSeconds;
            
            var agent = hero.GetAgent();
            var pfx = agent == null ? null : new AgentPfx(agent, Pfx);
            
            pfx?.Start();
            BLTHeroPowersMissionBehavior.PowerHandler.ConfigureHandlers(hero, this, handlers =>
            {
                var deactivationHandler = new DeactivationHandler();
                handlers.OnSlowTick += (_, _) =>
                {
                    if (MBCommon.GetTime(MBCommon.TimeType.Mission) > expiry[hero])
                    {
                        BLTHeroPowersMissionBehavior.PowerHandler.ClearHandlers(hero, this);
                        pfx?.Stop();
                        expiryCallback();
                        deactivationHandler.Deactivate(hero);
                    }
                };
                handlers.OnGotKilled += (_, _, _, _, _, _) =>
                {
                    // Expire immediately
                    BLTHeroPowersMissionBehavior.PowerHandler.ClearHandlers(hero, this);
                    expiry[hero] = 0;
                    pfx?.Stop();
                    expiryCallback();
                    deactivationHandler.Deactivate(hero);
                };
                handlers.OnMissionOver += _ =>
                {
                    // It will be called multiple times, but its not costly
                    expiry.Clear();
                };
                OnActivation(hero, handlers, agent, deactivationHandler);
            });
        }

        (float duration, float remaining) IHeroPowerActive.DurationRemaining(Hero hero)
        {
            if (!expiry.TryGetValue(hero, out float expiryVal))
            {
                return (0, 0);
            }

            return (PowerDurationSeconds, 
                Math.Max(0, Math.Min(PowerDurationSeconds, expiryVal - MBCommon.GetTime(MBCommon.TimeType.Mission))));
        }

        #endregion

        #region Implementation Details
        private Dictionary<Hero, float> expiry = new();

        protected class DeactivationHandler
        {
            public event Action<Hero> OnDeactivate;
            public void Deactivate(Hero hero)
            {
                OnDeactivate?.Invoke(hero);
            }
        }

        [YamlIgnore, Browsable(false)]
        protected virtual bool RequiresHeroAgent => false;
      
        protected abstract void OnActivation(Hero hero, PowerHandler.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null);
        #endregion

        #region ICloneable
        public override object Clone()
        {
            var newObj = (DurationMissionHeroPowerDefBase)base.Clone();
            newObj.Pfx = new(Pfx.Select(pfx => (ParticleEffectDef)pfx.Clone()));
            newObj.expiry = new();
            return newObj;
        }
        #endregion
    }
}