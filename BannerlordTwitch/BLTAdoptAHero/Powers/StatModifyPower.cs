using System.ComponentModel;
using System.IO;
using BannerlordTwitch;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class StatModifyPower : DurationMissionHeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        [Category("Power Config"), Description("What hero stat to modify"), PropertyOrder(1), ExpandableObject, UsedImplicitly]
        public AgentModifierConfig Modifiers { get; set; } = new();

        protected override bool RequiresHeroAgent => true;
        
        public StatModifyPower()
        {
            Type = new ("6DF1D8D6-02C6-4D30-8D12-CCE24077A4AA");
        }

        public override string ToString() => $"{Name}: {Modifiers}";

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            Modifiers.GenerateDocumentation(generator);
        }
        #endregion

        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers)
        {
            handlers.OnAgentBuild += (_, agent) =>
            {
                BLTAgentModifierBehavior.Current.Add(agent, Modifiers);
            };
        }

        protected override void OnActivation(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers,
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
