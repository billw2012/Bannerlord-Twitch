using System;
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class AddHealthPower : HeroPowerDefBase, IHeroPowerPassive
    {
        [Category("Power Config"), Description("How much HP to add"), PropertyOrder(1)]
        public float? HealthToAdd { get; set; }
        [Category("Power Config"), Description("How much to multiply base HP by"), PropertyOrder(1)]
        public float? HealthToMultiply { get; set; }
        
        public AddHealthPower()
        {
            Type = new ("C4213666-2176-42B4-8DBB-BFE0182BCCE1");
        }

        public void OnAdded(Hero hero) {}
        public void OnRemoved(Hero hero) {}
        public void OnBattleStart(Hero hero) {}
        public void OnBattleTick(Hero hero, Agent agent) {}
        public void OnBattleEnd(Hero hero) {}

        public void OnAgentBuild(Hero hero, Agent agent)
        {
            if (HealthToMultiply.HasValue)
            {
                agent.BaseHealthLimit *= HealthToMultiply.Value;
                agent.HealthLimit *= HealthToMultiply.Value;
                agent.Health *= HealthToMultiply.Value;
            }

            if (HealthToAdd.HasValue)
            {
                agent.BaseHealthLimit += HealthToAdd.Value;
                agent.HealthLimit += HealthToAdd.Value;
                agent.Health += HealthToAdd.Value;
            }
        }
        
        public void OnAgentKilled(Hero hero, Agent agent, Hero killerHero, Agent killerAgent) {}
        public void OnDoDamage(Hero hero, Agent agent, Hero victimHero, Agent victimAgent, ref AttackCollisionData attackCollisionData) {}
        public void OnTakeDamage(Hero hero, Agent agent, Hero attackerHero, Agent attackerAgent, ref AttackCollisionData attackCollisionData) {}
        
        public override string ToString()
        {
            var parts = new List<string>();
            if (HealthToMultiply.HasValue && HealthToMultiply.Value != 1)
            {
                parts.Add($"x{HealthToMultiply.Value:0.0} ({HealthToMultiply.Value * 100:0.0}%) health");
            }
            if (HealthToAdd.HasValue && HealthToAdd.Value != 0)
            {
                parts.Add(HealthToAdd > 0 ? $"+{HealthToAdd.Value:0.0} health" : $"{HealthToAdd.Value:0.0} health");
            }
            return $"{Name}: {string.Join(", ", parts)}";
        }
    }
}