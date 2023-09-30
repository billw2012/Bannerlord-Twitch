using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    [LocDisplayName("{=7d0IdABV}Reflect Damage Power"),
     LocDescription("{=jcNZx1M0}Adds fixed or relative amount of extra HP to the hero when they spawn"),
     UsedImplicitly]
    public class ReflectDamagePower : DurationMissionHeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        #region User Editable
        [LocDisplayName("{=buBZhrP0}Reflect Percent"),
         LocCategory("Power Config", "{=75UOuDM}Power Config"), 
         LocDescription("{=ibxqpdtD}What percent of damage to reflect back to attacker"),
         Range(0, 200), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(1), UsedImplicitly]
        public float ReflectPercent { get; set; }
        
        [LocDisplayName("{=IxiHjT1Z}Reflected Damage Is Subtracted"),
         LocCategory("Power Config", "{=75UOuDM}Power Config"), 
         LocDescription("{=Pdg6mCPA}Whether the damage that is reflected is also subtracted from the damage the hero takes (this is 'classic' damage reflection)"), 
         PropertyOrder(2), UsedImplicitly]
        public bool ReflectedDamageIsSubtracted { get; set; } = true;
        
        [LocDisplayName("{=WDRb4mQ3}Hit Behavior"),
         LocCategory("Power Config", "{=75UOuDM}Power Config"),
         LocDescription("{=SHlCPdpp}Hit behavior for the reflected damage"),
         PropertyOrder(3), UsedImplicitly, ExpandableObject]
        public HitBehavior HitBehavior { get; set; }
        #endregion

        #region Implementation Details
        protected override void OnActivation(Hero hero, PowerHandler.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null)
            => handlers.OnTakeDamage += OnTakeDamage;
        
        private void OnTakeDamage(Agent agent, Agent attackerAgent, BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams) 
        {
            int damage = (int) (blowParams.blow.InflictedDamage * ReflectPercent / 100f);
            if (damage > 0 && attackerAgent != null && attackerAgent != agent)
            {
                var blow = new Blow(attackerAgent.Index)
                {
                    AttackType = attackerAgent.IsMount ? AgentAttackType.Collision : AgentAttackType.Standard,
                    DamageType = attackerAgent.IsMount ? DamageTypes.Blunt : blowParams.blow.DamageType,
                    BoneIndex = agent.Monster.ThoraxLookDirectionBoneIndex,
                    GlobalPosition = agent.Position,
                    BlowFlag = HitBehavior.AddFlags(agent, BlowFlags.None),
                    BaseMagnitude = 0f,
                    InflictedDamage = damage,
                    SwingDirection = agent.LookDirection.NormalizedCopy(),
                    Direction = agent.LookDirection,
                    DamageCalculated = true,
                    WeaponRecord = new () { AffectorWeaponSlotOrMissileIndex = -1 },
                };
                // blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
                attackerAgent.RegisterBlow(blow, blowParams.collisionData);
                if (ReflectedDamageIsSubtracted)
                {
                    blowParams.blow.InflictedDamage = Math.Max(0, blowParams.blow.InflictedDamage - damage);
                }
            }
        }
        #endregion

        #region Public Interface
        [YamlIgnore, Browsable(false)] 
        public bool IsEnabled => ReflectPercent != 0;
        [YamlIgnore, Browsable(false)]
        public override LocString Description => !IsEnabled 
            ? "{=41sZdkDw}(disabled)" 
            : HitBehavior.IsEnabled 
                ? "{=vLGJf7ow}Reflect {ReflectPercent}% damage with {HitBehavior}"
                    .Translate(("ReflectPercent", ReflectPercent.ToString("0")), ("HitBehavior", HitBehavior))
                : "{=12tMrCY7}Reflect {ReflectPercent}% damage"
                    .Translate(("ReflectPercent", ReflectPercent));
        #endregion

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.PropertyValuePair("{=mDczhD1H}Reflect".Translate(), $"{ReflectPercent:0}%");
            if (ReflectedDamageIsSubtracted)
            {
                generator.Value("{=yfmwtdMC}Reflected damage is subtracted from incoming damage".Translate());
            }
            generator.PropertyValuePair(GetType().GetProperty(nameof(HitBehavior)).GetDisplayName(), HitBehavior.ToString());
        }
        #endregion

        #region IHeroPowerPassive
        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers) => OnActivation(hero, handlers);
        #endregion
    }
}
