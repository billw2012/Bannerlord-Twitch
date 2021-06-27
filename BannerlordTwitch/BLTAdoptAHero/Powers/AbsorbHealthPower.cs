using System;
using System.ComponentModel;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class AbsorbHealthPower : HeroPowerDefBase, IHeroPowerPassive
    {
        [Category("Power Config"), Description("What fraction of damage done to absorb as health"), PropertyOrder(1)]
        public float FractionOfDamageToAbsorb { get; set; } = 0.1f;
        
        public AbsorbHealthPower()
        {
            Type = new ("E0A274DF-ADBB-4725-9EAE-59806BF9B5DC");
        }

        public void OnAdded(Hero hero) {}
        public void OnRemoved(Hero hero) {}
        public void OnBattleStart(Hero hero) {}
        public void OnBattleTick(Hero hero, Agent agent) {}
        public void OnBattleEnd(Hero hero) {}
        public void OnAgentBuild(Hero hero, Agent agent) {}
        public void OnAgentKilled(Hero hero, Agent agent, Hero killerHero, Agent killerAgent) {}

        public void OnDoDamage(Hero hero, Agent agent, Hero victimHero, Agent victimAgent,
            ref AttackCollisionData attackCollisionData)
        {
            agent.Health = Math.Min(agent.HealthLimit, agent.Health + attackCollisionData.InflictedDamage * FractionOfDamageToAbsorb);
        }
        public void OnTakeDamage(Hero hero, Agent agent, Hero attackerHero, Agent attackerAgent, ref AttackCollisionData attackCollisionData) {}

        public override string ToString() => $"{Name}: absorb x{FractionOfDamageToAbsorb:0.0} ({FractionOfDamageToAbsorb * 100:0.0}%) damage dealt as HP";
    }
}