using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    public class PassivePowerGroupItem : PowerGroupItemBase
    {
        [PropertyOrder(0),
         ItemsSource(typeof(HeroPowerDefBase.ItemSourcePassive)), PropertyOrder(1), UsedImplicitly]
        public Guid PowerID { get; set; }

        [ReadOnly(true), YamlIgnore]
        public IHeroPowerPassive Power => PowerConfig?.GetPower(PowerID) as IHeroPowerPassive;

        public override string ToString() => $"[{Power?.ToString() ?? "(no power)"}] {base.ToString()}";
    }
    
    public class PassivePowerGroup : ILoaded, IDocumentable, ICloneable
    {
        #region User Editable
        [Description("The name of the power: how the power will be described in messages"), 
         PropertyOrder(1), UsedImplicitly]
        public string Name { get; set; } = "Enter Name Here";
        
        [PropertyOrder(2), 
         Description("The various effects in the power. These can also have customized unlock requirements, so you " +
                     "can have classes that get stronger (or weaker!) over time (or by any other measure)."), 
         UsedImplicitly]
        public List<PassivePowerGroupItem> Powers { get; set; } = new();
        #endregion
        
        #region Implementation Detail
        [YamlIgnore, Browsable(false)] 
        private GlobalHeroPowerConfig PowerConfig { get; set; }
        #endregion

        #region Public Interface
        [YamlIgnore, Browsable(false)]
        public IEnumerable<PassivePowerGroupItem> ValidPowers => Powers.Where(p => p.Power != null);
        public IEnumerable<IHeroPowerPassive> GetUnlockedPowers(Hero hero) 
            => ValidPowers.Where(p => p.IsUnlocked(hero)).Select(p => p.Power);

        public PassivePowerGroup()
        {
            // For when these are created via the configure tool
            PowerConfig = ConfigureContext.CurrentlyEditedSettings == null 
                ? null : GlobalHeroPowerConfig.Get(ConfigureContext.CurrentlyEditedSettings);
        }
                
        public void OnHeroJoinedBattle(Hero hero)
        {
            if (PowerConfig.DisablePowersInTournaments && MissionHelpers.InTournament())
            {
                return;
            }

            foreach(var power in GetUnlockedPowers(hero))
            {
                BLTHeroPowersMissionBehavior.PowerHandler.ConfigureHandlers(
                    hero, power as HeroPowerDefBase, handlers => power.OnHeroJoinedBattle(hero, handlers));
            }
        }
                
        public override string ToString() 
            => $"{Name} {string.Join(" ", Powers.Select(p => p.ToString()))}";
        #endregion

        #region ICloneable
        public object Clone()
        {
            var clone = CloneHelpers.CloneProperties(this);
            clone.Powers = CloneHelpers.CloneCollection(Powers).ToList();
            clone.PowerConfig = PowerConfig; 
            return clone;
        }
        #endregion

        #region ILoaded
        public void OnLoaded(Settings settings)
        {
            PowerConfig = GlobalHeroPowerConfig.Get(settings);   
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
                    
            // generator.Table("power", () =>
            // {
            //     generator.TR(() => generator.TD("Name").TD(Name));
            //     foreach ((var power, int i) in Powers.Select((power, i) => (power, i)))
            //     {
            //         generator.TR(() => generator.TD($"Effect {i + 1}").TD(power.ToString().SplitCamelCase()));
            //     }
            // });
        }
        #endregion
    }
}