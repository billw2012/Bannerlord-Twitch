using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    [LocDisplayName("{=ZbxNmvem}Add Health Power"),
     LocDescription("{=NlJXiSkn}Adds fixed or relative amount of extra HP to the hero when they spawn"), 
     UsedImplicitly]
    public class AddHealthPower : HeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        #region User Editable
        [LocDisplayName("{=EQ16SH5A}Health Modifier Percent"),
         LocCategory("Power Config", "{=75UOuDM}Power Config"), 
         LocDescription("{=Dr2whvVV}Modifier to apply to base HP"),
         UIRange(0, 1000, 1f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(1), UsedImplicitly]
        public float HealthModifierPercent { get; set; } = 100f;

        [LocDisplayName("{=pnr7P9sj}Health To Add"),
         LocCategory("Power Config", "{=75UOuDM}Power Config"), 
         LocDescription("{=sprkIlxQ}How much HP to add (applied after Modifier)"), 
         PropertyOrder(2), UsedImplicitly]
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
        public override LocString Description
        {
            get
            {
                if (!IsEnabled) return "{=41sZdkDw}(disabled)";
                return (HealthModifierPercent != 100 ? $"{HealthModifierPercent:0}% " : "")
                       + (HealthToAdd > 0 ? "+" : "") + (HealthToAdd != 0 ? $"{HealthToAdd:0.0} " : "")
                       + Naming.HP;
            }
        }
        #endregion

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator) => generator.P(Description.ToString());
        #endregion
    }
}
