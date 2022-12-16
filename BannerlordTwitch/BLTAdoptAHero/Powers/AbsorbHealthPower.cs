using System;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    // NOTES:
    // Any HeroPowerDefBase instance can only be active once for any hero, so unique key for
    // registering an active one is Hero + Power Instance. 
    // Active Power should have Agent life time in Battle
    // Passive Power has Battle life time
    // The specific implementation can define the lifetime though, using events/callbacks/however its going to be implemented
    
    [LocDisplayName("{=HmDfLY2B}Absorb Health Power"),
     LocDescription("{=ZHdogsAa}Absorbs a fraction of damage you do to enemies as health"),
     UsedImplicitly]
    public class AbsorbHealthPower : DurationMissionHeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        #region User Editable
        [LocCategory("Power Config", "{=75UOuDM}Power Config"),
         LocDescription("{=8SE2asYO}What percentage of damage done to absorb as health"),
         UIRange(0, 100, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(1), UsedImplicitly]
        public float DamageToAbsorbPercent { get; set; }
        #endregion
        
        #region IHeroPowerPassive
        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers) 
            => OnActivation(hero, handlers);
        #endregion

        #region Implementation Details
        protected override void OnActivation(Hero hero, PowerHandler.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null) 
            => handlers.OnDoDamage += OnDoDamage;

        private void OnDoDamage(Agent agent, Agent victimAgent, 
            BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams)
        {
            agent.Health = Math.Min(agent.HealthLimit, 
                agent.Health + blowParams.blow.InflictedDamage * DamageToAbsorbPercent / 100f);
        }
        #endregion

        #region Public Interface
        [YamlIgnore, Browsable(false)]
        public bool IsEnabled => DamageToAbsorbPercent != 0;
        
        [YamlIgnore, Browsable(false)]
        public override LocString Description => !IsEnabled 
            ? "{=41sZdkDw}(disabled)".Translate() 
            : "{=04WkC9p6}Absorb {DamageToAbsorbPercent}% of damage dealt as HP"
                .Translate(("DamageToAbsorbPercent", DamageToAbsorbPercent.ToString("0")));
        #endregion

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator) => generator.P(Description.ToString());
        #endregion
    }
}
