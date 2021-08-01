using System.Collections.Generic;
using System.ComponentModel;
using BannerlordTwitch;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class AddHealthPower : HeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        [Category("Power Config"), Description("How much to multiply base HP by"), PropertyOrder(1), UsedImplicitly]
        public float HealthToMultiply { get; set; } = 1f;

        [Category("Power Config"), Description("How much HP to add"), PropertyOrder(2), UsedImplicitly]
        public float HealthToAdd { get; set; }

        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers)
            => handlers.OnAgentBuild += OnAgentBuild;

        private void OnAgentBuild(Hero hero, Agent agent)
        {
            agent.BaseHealthLimit *= HealthToMultiply;
            agent.HealthLimit *= HealthToMultiply;
            agent.Health *= HealthToMultiply;

            agent.BaseHealthLimit += HealthToAdd;
            agent.HealthLimit += HealthToAdd;
            agent.Health += HealthToAdd;
        }

        public override string ToString() => $"{Name}: {ToStringInternal()}";
        
        private string ToStringInternal()
        {
            var parts = new List<string>();
            if (HealthToMultiply != 1)
            {
                parts.Add($"{HealthToMultiply * 100:0.0}%");
            }
            if (HealthToAdd != 0)
            {
                parts.Add(HealthToAdd > 0 ? $"+{HealthToAdd:0.0}" : $"{HealthToAdd:0.0}");
            }
            return $"{string.Join(", ", parts)} HP";
        }

        public void GenerateDocumentation(IDocumentationGenerator generator) => generator.P(ToStringInternal());
    }
}
