using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class StatModifyPower : DurationMissionHeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        [Category("Power Config"), Description("What hero stat to modify"), 
         PropertyOrder(1), ExpandableObject, Expand, UsedImplicitly]
        public AgentModifierConfig Modifiers { get; set; } = new();

        [YamlIgnore, Browsable(false)]
        protected override bool RequiresHeroAgent => true;

        public override string ToString() => $"{Name}: {Modifiers}";

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            Modifiers.GenerateDocumentation(generator);
        }
        #endregion

        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers)
        {
            handlers.OnAgentBuild += (_, agent) =>
            {
                BLTAgentModifierBehavior.Current.Add(agent, Modifiers);
            };
        }

        protected override void OnActivation(Hero hero, PowerHandler.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null)
        {
            BLTAgentModifierBehavior.Current.Add(agent, Modifiers);
            if (deactivationHandler != null)
            {
                deactivationHandler.OnDeactivate += _ => BLTAgentModifierBehavior.Current.Remove(agent, Modifiers);
            }
        }
    }
}
