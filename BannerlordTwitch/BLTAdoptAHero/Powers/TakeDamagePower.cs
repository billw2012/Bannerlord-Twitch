using System.Collections.Generic;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    [Description("Changes the effects of incoming damage"), UsedImplicitly]
    public class TakeDamagePower : DurationMissionHeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        #region User Editable
        [Browsable(false)]
        public float DamageMultiplier { get; set; } = 1f;
        
        [Category("Power Config"),
         Description("Damage modifier (set less than 100% to reduce incoming damage, set greater than 100% to increase it)"),
         UIRange(0, 1000, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         YamlIgnore, PropertyOrder(1), UsedImplicitly]
        public float DamageModifierPercent { get => DamageMultiplier * 100; set => DamageMultiplier = value / 100f; }

        [Category("Power Config"),
         Description("Behaviors to add to the damage (e.g. add Shrug Off to ensure the hero is never " +
                     "stunned when hit)"), PropertyOrder(2), ExpandableObject, UsedImplicitly]
        public HitBehavior AddHitBehavior { get; set; }

        [Category("Power Config"),
         Description("Behaviors to remove from the damage (e.g. remove Shrug Off to ensure the hero is always " +
                     "stunned when hit)"), PropertyOrder(3), ExpandableObject, UsedImplicitly]
        public HitBehavior RemoveHitBehavior { get; set; }
        #endregion

        #region Private Implementation
        protected override void OnActivation(Hero hero, PowerHandler.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null)
            => handlers.OnTakeDamage += OnTakeDamage;
        
        private void OnTakeDamage(Hero hero, Agent agent, Hero attackerHero, Agent attackerAgent,
            BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams)
        {
            blowParams.blow.BlowFlag |= AddHitBehavior.Generate(agent);
            blowParams.blow.BlowFlag &= ~RemoveHitBehavior.Generate(agent);

            if (DamageMultiplier != 1)
            {
                blowParams.collisionData.BaseMagnitude = blowParams.blow.BaseMagnitude 
                    = (int) (blowParams.blow.BaseMagnitude * DamageMultiplier);
                blowParams.collisionData.InflictedDamage = blowParams.blow.InflictedDamage
                    = (int) (blowParams.blow.BaseMagnitude - blowParams.blow.AbsorbedByArmor);
            }
        }
        #endregion

        #region Public Interface
        public override string ToString() => $"{base.ToString()}: {Description}";

        [YamlIgnore]
        public string Description
        {
            get 
            {
                var parts = new List<string>();
                if (DamageModifierPercent != 100)
                {
                    parts.Add($"{DamageModifierPercent:0}% damage");
                }

                string addHit = AddHitBehavior.ToString();
                if (!string.IsNullOrEmpty(addHit))
                {
                    parts.Add(addHit);
                }
                string removeHit = RemoveHitBehavior.ToString();
                if (!string.IsNullOrEmpty(removeHit))
                {
                    parts.Add(removeHit);
                }
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
            generator.PropertyValuePair(
                nameof(DamageMultiplier).SplitCamelCase(), 
                $"{DamageMultiplier * 100:0}%");
            generator.PropertyValuePair(nameof(AddHitBehavior).SplitCamelCase(), () => AddHitBehavior.GenerateDocumentation(generator));
            generator.PropertyValuePair(nameof(RemoveHitBehavior).SplitCamelCase(), () => RemoveHitBehavior.GenerateDocumentation(generator));
        }
        #endregion
    }
}
