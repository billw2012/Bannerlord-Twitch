using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Models;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    [LocDisplayName("{=pVdYFJNQ}Stat Modify Power"),
     LocDescription("{=AOaW2ORo}Applies modifiers to various character stats"), 
     UsedImplicitly]
    public class StatModifyPower : DurationMissionHeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        #region User Editable
        [LocDisplayName("{=9gTYAifL}Modifiers"),
         LocCategory("Power Config", "{=75UOuDM}Power Config"), 
         LocDescription("{=URinrDNq}What hero stat to modify"), 
         PropertyOrder(1), ExpandableObject, Expand, UsedImplicitly]
        public AgentModifierConfig Modifiers { get; set; } = new();
        #endregion

        #region Implementation Details
        [YamlIgnore, Browsable(false)]
        protected override bool RequiresHeroAgent => true;
        
        protected override void OnActivation(Hero hero, PowerHandler.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null)
        {
            Activate(hero, agent);
            if (deactivationHandler != null)
            {
                deactivationHandler.OnDeactivate += _ =>
                {
                    BLTAgentModifierBehavior.Current.Remove(agent, Modifiers);
                    BLTAgentStatCalculateModel.Current.RemoveModifiers(hero, Modifiers.Skills);
                };
            }
        }

        private void Activate(Hero hero, Agent agent)
        {
            BLTAgentModifierBehavior.Current.Add(agent, Modifiers);
            BLTAgentStatCalculateModel.Current.AddModifiers(hero, Modifiers.Skills);
        }

        #endregion

        #region Public Interface
        [YamlIgnore, Browsable(false)]
        public override LocString Description => $"{Modifiers}";
        #endregion

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            Modifiers.GenerateDocumentation(generator);
        }
        #endregion

        #region IHeroPowerPassive
        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers)
        {
            handlers.OnAgentBuild += agent => Activate(hero, agent);
        }
        #endregion
    }
}
