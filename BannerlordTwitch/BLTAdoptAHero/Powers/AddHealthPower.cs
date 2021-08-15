using System.Collections.Generic;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.UI;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class AddHealthPower : HeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        #region User Editable
        [Category("Power Config"), Description("Modifier to apply to base HP"),
         UIRange(0, 1000, 1f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(1), UsedImplicitly]
        public float HealthModifierPercent { get; set; } = 100f;

        [Category("Power Config"), Description("How much HP to add (applied after Modifier)"), PropertyOrder(2), UsedImplicitly]
        public float HealthToAdd { get; set; }
        #endregion

        #region IHeroPowerPassive
        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers)
            => handlers.OnAgentBuild += OnAgentBuild;
        #endregion

        #region Private Implementation
        private void OnAgentBuild(Agent agent)
        {
            agent.BaseHealthLimit *= HealthModifierPercent / 100f;
            agent.HealthLimit *= HealthModifierPercent / 100f;
            agent.Health *= HealthModifierPercent / 100f;

            agent.BaseHealthLimit += HealthToAdd;
            agent.HealthLimit += HealthToAdd;
            agent.Health += HealthToAdd;
        }
        #endregion

        #region Public Interface
        [YamlIgnore, Browsable(false)]
        public bool IsEnabled => HealthModifierPercent != 100 || HealthToAdd != 0;
        [YamlIgnore, Browsable(false)]
        public override string Description
        {
            get
            {
                if (!IsEnabled) return "(disabled)";
                return (HealthModifierPercent != 100 ? $"{HealthModifierPercent:0}% " : "")
                       + (HealthToAdd > 0 ? "+" : "") + (HealthToAdd != 0 ? $"{HealthToAdd:0.0} " : "")
                       + "HP";
            }
        }
        #endregion

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator) => generator.P(Description);
        #endregion
    }
}
