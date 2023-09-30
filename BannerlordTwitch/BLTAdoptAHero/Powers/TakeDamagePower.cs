using System.Collections.Generic;
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
    [LocDisplayName("{=UJHquN95}Take Damage Power"),
     LocDescription("{=F3TxyV4d}Changes the effects of incoming damage"), 
     UsedImplicitly]
    public class TakeDamagePower : DurationMissionHeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        #region User Editable

        [LocDisplayName("{=jkYLVPpL}Damage Modifier Percent"),
         LocCategory("Effect", "{=VBuncBq5}Effect"),
         LocDescription("{=1eJHtO06}Damage modifier (set less than 100% to reduce incoming damage, set greater than 100% to increase it)"),
         UIRange(0, 200, 5), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(1), UsedImplicitly]
        public float DamageModifierPercent { get; set; } = 100f;

        [LocDisplayName("{=4XgEPe3Q}Damage To Add"),
         LocCategory("Effect", "{=VBuncBq5}Effect"),
         LocDescription("{=wuOM9C4l}How much damage to add or subtract"), 
         PropertyOrder(2), UsedImplicitly]
        public int DamageToAdd { get; set; }
        
        [LocDisplayName("{=2JFBV975}Add Hit Behavior"),
         LocCategory("Effect", "{=VBuncBq5}Effect"),
         LocDescription("{=9t2oXdqy}Behaviors to add to the damage (e.g. add Shrug Off to ensure the hero is never stunned when hit)"), 
         ExpandableObject, PropertyOrder(3), UsedImplicitly]
        public HitBehavior AddHitBehavior { get; set; }

        [LocDisplayName("{=GLAvDJl5}Remove Hit Behavior"),
         LocCategory("Effect", "{=VBuncBq5}Effect"),
         LocDescription("{=oA2TrarN}Behaviors to remove from the damage (e.g. remove Shrug Off to ensure the hero is always stunned when hit)"), 
         ExpandableObject, PropertyOrder(4), UsedImplicitly]
        public HitBehavior RemoveHitBehavior { get; set; }
        
        [LocDisplayName("{=9Lwkwni0}Armor To Ignore Percent"),
         LocCategory("Effect", "{=VBuncBq5}Effect"),
         LocDescription("{=wSkoeVw1}What fraction (0 to 1) of armor to ignore when applying damage"), 
         UIRangeAttribute(0, 100, 1f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(5), UsedImplicitly]
        public float ArmorToIgnorePercent { get; set; }
        #endregion

        #region Private Implementation
        protected override void OnActivation(Hero hero, PowerHandler.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null)
            => handlers.OnTakeDamage += OnTakeDamage;
        
        private void OnTakeDamage(Agent agent, Agent attackerAgent, BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams)
        {
            AddDamagePower.ApplyDamageEffects(agent, blowParams, ArmorToIgnorePercent, DamageModifierPercent, 
                DamageToAdd, AddHitBehavior, RemoveHitBehavior);
        }
        #endregion

        #region Public Interface
        [YamlIgnore, Browsable(false)]
        public bool IsEnabled => DamageModifierPercent != 100 || AddHitBehavior.IsEnabled || RemoveHitBehavior.IsEnabled;
        [YamlIgnore, Browsable(false)]
        public override LocString Description
        {
            get 
            {
                if (!IsEnabled) return "{=41sZdkDw}(disabled)";
                var parts = new List<string>();
                if (DamageModifierPercent != 100) 
                    parts.Add("{=stFtZvfp}{DamageModifierPercent}% damage"
                        .Translate(("DamageModifierPercent", DamageModifierPercent.ToString("0"))));
                if (AddHitBehavior.IsEnabled) parts.Add(AddHitBehavior.ToString());
                if (RemoveHitBehavior.IsEnabled) parts.Add(RemoveHitBehavior.ToString());
                return string.Join(", ", parts);
            }
        }
        #endregion

        #region IHeroPowerPassive
        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers) => OnActivation(hero, handlers);
        #endregion

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.PropertyValuePair("{=GrBznDeq}Damage".Translate(), $"{DamageModifierPercent:0}%");
            generator.PropertyValuePair(GetType().GetProperty(nameof(AddHitBehavior)).GetDisplayName(), 
                () => AddHitBehavior.GenerateDocumentation(generator));
            generator.PropertyValuePair(GetType().GetProperty(nameof(RemoveHitBehavior)).GetDisplayName(), 
                () => RemoveHitBehavior.GenerateDocumentation(generator));
        }
        #endregion
    }
}
