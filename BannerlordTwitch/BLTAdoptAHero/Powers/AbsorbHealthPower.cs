using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BannerlordTwitch;
using BannerlordTwitch.UI;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    // NOTES:
    // Any HeroPowerDefBase instance can only be active once for any hero, so unique key for
    // registering an active one is Hero + Power Instance. 
    // Active Power should have Agent life time in Battle
    // Passive Power has Battle life time
    // The specific implementation can define the lifetime though, using events/callbacks/however its going to be implemented
    
    [Description("Absorbs a fraction of damage you do to enemies as health"), UsedImplicitly]
    public class AbsorbHealthPower : DurationMissionHeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        #region User Editable
        
        [Browsable(false)]
        public float FractionOfDamageToAbsorb { get; set; } = 0.1f;
        
        [Category("Power Config"),
         Description("What percentage of damage done to absorb as health"),
         UIRange(0, 100, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         YamlIgnore, PropertyOrder(1), UsedImplicitly]
        public float DamageToAbsorbPercent { get => FractionOfDamageToAbsorb * 100; set => FractionOfDamageToAbsorb = value / 100f; }
        #endregion
        
        #region IHeroPowerPassive
        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers) 
            => OnActivation(hero, handlers);
        #endregion

        #region Implementation Details
        protected override void OnActivation(Hero hero, PowerHandler.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null) 
            => handlers.OnDoDamage += OnDoDamage;

        private void OnDoDamage(Hero hero, Agent agent, Hero victimHero, Agent victimAgent, BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams)
        {
            agent.Health = Math.Min(agent.HealthLimit, 
                agent.Health + blowParams.blow.InflictedDamage * FractionOfDamageToAbsorb);
        }
        #endregion

        #region Public Interface
        public override string ToString() => $"{base.ToString()}: {Description}";

        [YamlIgnore]
        public string Description
            => $"Absorb {FractionOfDamageToAbsorb * 100:0.0}% of damage dealt as HP";
        #endregion

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator) => generator.P(Description);
        #endregion
    }
}
