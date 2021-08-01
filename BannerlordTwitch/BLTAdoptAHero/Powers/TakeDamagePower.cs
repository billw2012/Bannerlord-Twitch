using System.Collections.Generic;
using System.ComponentModel;
using BannerlordTwitch;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    [Description("Changes the effects of incoming damage"), UsedImplicitly]
    public class TakeDamagePower : DurationMissionHeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        [Category("Power Config"),
         Description("Damage multiplier (set less than 1 to reduce incoming damage, set greater than 1 to increase it)"),
         PropertyOrder(1), UsedImplicitly]
        public float DamageMultiplier { get; set; } = 1f;

        [Category("Power Config"),
         Description("Behaviors to add to the damage (e.g. add Shrug Off to ensure the hero is never " +
                     "stunned when hit)"), PropertyOrder(2), ExpandableObject, UsedImplicitly]
        public HitBehavior AddHitBehavior { get; set; }

        [Category("Power Config"),
         Description("Behaviors to remove from the damage (e.g. remove Shrug Off to ensure the hero is always " +
                     "stunned when hit)"), PropertyOrder(3), ExpandableObject, UsedImplicitly]
        public HitBehavior RemoveHitBehavior { get; set; }

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

        public override string ToString() => $"{Name}: {ToStringInternal()}";

        private string ToStringInternal()
        {
            var parts = new List<string>();
            if (DamageMultiplier != 1)
            {
                parts.Add($"{DamageMultiplier*100:0}% damage");
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

        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.PropertyValuePair(
                nameof(DamageMultiplier).SplitCamelCase(), 
                $"{DamageMultiplier * 100:0}%");
            generator.PropertyValuePair(nameof(AddHitBehavior).SplitCamelCase(), () => AddHitBehavior.GenerateDocumentation(generator));
            generator.PropertyValuePair(nameof(RemoveHitBehavior).SplitCamelCase(), () => RemoveHitBehavior.GenerateDocumentation(generator));
        }

        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers) => OnActivation(hero, handlers);
        protected override void OnActivation(Hero hero, PowerHandler.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null)
            => handlers.OnTakeDamage += OnTakeDamage;
    }
}
