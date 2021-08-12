using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    public class ActivePowerGroupItem : PowerGroupItemBase
    {
        [PropertyOrder(0),
         ItemsSource(typeof(HeroPowerDefBase.ItemSourceActive)), UsedImplicitly]
        public Guid PowerID { get; set; }

        [Browsable(false), YamlIgnore]
        public IHeroPowerActive Power => PowerConfig?.GetPower(PowerID) as IHeroPowerActive;
        
        public override string ToString() => $"[{Power?.ToString() ?? "(no power)"}] {base.ToString()}";
    }
    
    public class ActivePowerGroup : IDocumentable, ICloneable
    {
        #region User Editable
        [PropertyOrder(1), Description("The name of the power: how the power will be described in messages"), 
         UsedImplicitly]
        public string Name { get; set; } = "Enter Name Here";

        [PropertyOrder(2), 
         Description("The various effects in the power. These can also have customized unlock requirements, so you " +
                     "can have classes that get stronger (or weaker!) over time (or by any other measure)."), 
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         UsedImplicitly]
        public ObservableCollection<ActivePowerGroupItem> Powers { get; set; } = new();

        [PropertyOrder(3), Description("Particles/sound effects to play when this power group is activated"),
         ExpandableObject, UsedImplicitly]
        public OneShotEffect ActivateEffect { get; set; }

        [PropertyOrder(4), Description("Particles/sound effects to play when this power group is deactivated"),
         ExpandableObject, UsedImplicitly]
        public OneShotEffect DeactivateEffect { get; set; }
        #endregion
        
        #region Implementation Detail
        [YamlIgnore, Browsable(false)] 
        private GlobalHeroPowerConfig PowerConfig { get; set; }
        #endregion
        
        #region Public Interface
        [YamlIgnore, Browsable(false)]
        public IEnumerable<ActivePowerGroupItem> ValidPowers => Powers.Where(p => p.Power != null);
        public IEnumerable<IHeroPowerActive> GetUnlockedPowers(Hero hero) 
            => ValidPowers.Where(p => p.IsUnlocked(hero)).Select(p => p.Power);
        
        public bool IsActive(Hero hero) => Powers.Any(power => power.Power.IsActive(hero));

        public ActivePowerGroup()
        {
            // For when these are created via the configure tool
            PowerConfig = ConfigureContext.CurrentlyEditedSettings == null 
                ? null : GlobalHeroPowerConfig.Get(ConfigureContext.CurrentlyEditedSettings);
        }
        
        public (bool canActivate, string failReason) CanActivate(Hero hero)
        {
            if (PowerConfig.DisablePowersInTournaments && MissionHelpers.InTournament())
            {
                return (false, "Not allowed in tournaments!");
            }

            var unlockedPowers = GetUnlockedPowers(hero).ToList();

            if (!unlockedPowers.Any())
            {
                return (false, "No powers!");
            }

            (bool _, string failReason) = unlockedPowers
                .Select(power => power.CanActivate(hero))
                .FirstOrDefault(r => !r.canActivate);
            return failReason != null 
                ? (false, failReason)
                : (true, null);
        }

        public (bool allowed, string message) Activate(Hero hero, ReplyContext context)
        {
            if (PowerConfig.DisablePowersInTournaments && MissionHelpers.InTournament())
            {
                return (false, $"Powers not allowed in tournaments!");
            }

            var activatedPowers = GetUnlockedPowers(hero).ToList();
            foreach(var power in activatedPowers)
            {
                power.Activate(hero, () =>
                {
                    if (activatedPowers.All(p => !p.IsActive(hero)))
                    {
                        ActionManager.SendReply(context, $"{Name} expired!");
                        DeactivateEffect.Trigger(hero);
                    }
                });
            }
            ActivateEffect.Trigger(hero);
            return (true, $"{Name} activated!");
        }

        public (float duration, float remaining) DurationRemaining(Hero hero)
        {
            if (!ValidPowers.Any()) 
                return (1, 0);
            var remaining = ValidPowers
                .Select(active => active.Power.DurationRemaining(hero))
                .ToList();
            return (
                duration: remaining.Max(r => r.duration),
                remaining: remaining.Max(r => r.remaining)
            );
        }

        public override string ToString() => $"{Name} {string.Join(" ", Powers.Select(p => p.ToString()))}";
        #endregion

        #region ICloneable
        public object Clone()
        {
            var clone = CloneHelpers.CloneProperties(this);
            clone.Powers = new(CloneHelpers.CloneCollection(Powers));
            clone.PowerConfig = PowerConfig;
            return clone;
        }
        #endregion

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.P("power-title", Name);
            foreach (var power in Powers)
            {
                if (power is IDocumentable docPower)
                {
                    docPower.GenerateDocumentation(generator);
                }
                else
                {
                    generator.P(power.ToString());
                }
            }
        }
        #endregion
    }
}