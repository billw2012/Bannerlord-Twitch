using System;
using System.ComponentModel;
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
    
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class AbsorbHealthPower : DurationMissionHeroPowerDefBase, IHeroPowerPassive
    {
        [Category("Power Config"),
         Description("What fraction of damage done to absorb as health"), PropertyOrder(1), UsedImplicitly]
        public float FractionOfDamageToAbsorb { get; set; } = 0.1f;

        public AbsorbHealthPower()
        {
            Type = new ("E0A274DF-ADBB-4725-9EAE-59806BF9B5DC");
        }

        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers) 
            => OnActivation(hero, handlers);

        protected override void OnActivation(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null) 
            => handlers.OnDoDamage += OnDoDamage;

        private void OnDoDamage(Hero hero, Agent agent, Hero victimHero, Agent victimAgent, 
            AttackCollisionDataRef attackCollisionData)
        {
            agent.Health = Math.Min(agent.HealthLimit, 
                agent.Health + attackCollisionData.Data.InflictedDamage * FractionOfDamageToAbsorb);
        }

        public override string ToString() 
            => $"{Name}: absorb x{FractionOfDamageToAbsorb:0.0} ({FractionOfDamageToAbsorb * 100:0.0}%) damage dealt as HP";
    }
}
