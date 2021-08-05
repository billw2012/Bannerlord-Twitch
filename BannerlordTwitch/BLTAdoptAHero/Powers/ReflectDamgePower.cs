using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BannerlordTwitch;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class ReflectDamagePower : DurationMissionHeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        [Category("Power Config"), 
         Description("What fraction of damage to reflect back to attacker"), PropertyOrder(1),
         Range(0, 5), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float FractionOfDamageToReflect { get; set; } = 0.1f;

        [Category("Power Config"), 
         Description("Whether the damage that is reflected is also subtracted from the damage the hero takes " +
                     "(this is 'classic' damage reflection)"), PropertyOrder(2), UsedImplicitly]
        public bool ReflectedDamageIsSubtracted { get; set; } = true;
        
        [Category("Power Config"),
         Description("Hit behavior for the reflected damage"),
         PropertyOrder(3), UsedImplicitly, ExpandableObject]
        public HitBehavior HitBehavior { get; set; }

        private void OnTakeDamage(Hero hero, Agent agent, Hero attackerHero, Agent attackerAgent, BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams) 
        {
            int damage = (int) (blowParams.blow.InflictedDamage * FractionOfDamageToReflect);
            if (damage > 0 && attackerAgent != null && attackerAgent != agent)
            {
                var blow = new Blow(agent.Index)
                {
                    DamageType = blowParams.blow.DamageType,
                    BoneIndex = agent.Monster.ThoraxLookDirectionBoneIndex,
                    Position = agent.Position,
                    BlowFlag = HitBehavior.Generate(agent),
                    BaseMagnitude = 0f,
                    InflictedDamage = damage,
                    SwingDirection = agent.LookDirection.NormalizedCopy(),
                    Direction = agent.LookDirection,
                    DamageCalculated = true,
                };
                blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
                attackerAgent.RegisterBlow(blow);
                if (ReflectedDamageIsSubtracted)
                {
                    blowParams.blow.InflictedDamage -= damage;
                }
            }
        }

        public override string ToString() => $"{Name}: {ToStringInternal()}";
        
        private string ToStringInternal()
            => $"Reflect {FractionOfDamageToReflect * 100:0}% of damage received, {HitBehavior}";

        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.PropertyValuePair(
                nameof(FractionOfDamageToReflect).SplitCamelCase(), 
                $"{FractionOfDamageToReflect * 100:0}%");
            if (ReflectedDamageIsSubtracted)
            {
                generator.Value($"Reflected damage is subtracted from incoming damage");
            }
            generator.PropertyValuePair(nameof(HitBehavior).SplitCamelCase(), HitBehavior.ToString());
        }

        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers) => OnActivation(hero, handlers);
        protected override void OnActivation(Hero hero, PowerHandler.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null)
            => handlers.OnTakeDamage += OnTakeDamage;
    }
}
