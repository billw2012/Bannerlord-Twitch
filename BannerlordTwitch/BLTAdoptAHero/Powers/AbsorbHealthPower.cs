using System;
using System.ComponentModel;
using BannerlordTwitch;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

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
        [Category("Power Config"),
         Description("What fraction of damage done to absorb as health"), PropertyOrder(1), UsedImplicitly]
        public float FractionOfDamageToAbsorb { get; set; } = 0.1f;
        
        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers) 
            => OnActivation(hero, handlers);

        protected override void OnActivation(Hero hero, PowerHandler.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null) 
            => handlers.OnDoDamage += OnDoDamage;

        private void OnDoDamage(Hero hero, Agent agent, Hero victimHero, Agent victimAgent, BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams)
        {
            agent.Health = Math.Min(agent.HealthLimit, 
                agent.Health + blowParams.blow.InflictedDamage * FractionOfDamageToAbsorb);
        }

        public override string ToString() => $"{Name}: {ToStringInternal()}";

        private string ToStringInternal()
            => $"Absorb {FractionOfDamageToAbsorb * 100:0.0}% of damage dealt as HP";

        public void GenerateDocumentation(IDocumentationGenerator generator) => generator.P(ToStringInternal());
    }
}
