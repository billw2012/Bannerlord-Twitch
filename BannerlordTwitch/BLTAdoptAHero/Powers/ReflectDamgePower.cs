using System.ComponentModel;
using System.IO;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class ReflectDamagePower : DurationMissionHeroPowerDefBase, IHeroPowerPassive
    {
        [Category("Power Config"), Description("What fraction of damage to reflect back to attacker"), PropertyOrder(1), UsedImplicitly]
        public float FractionOfDamageToReflect { get; set; } = 0.1f;

        public ReflectDamagePower()
        {
            Type = new ("FFE07DA3-E977-42D8-80CA-5DFFF66123EB");
        }

        private void OnTakeDamage(Hero hero, Agent agent, Hero attackerHero, Agent attackerAgent,
            AttackCollisionDataRef attackCollisionData)
        {
            int damage = (int) (attackCollisionData.Data.InflictedDamage * FractionOfDamageToReflect);
            if (damage > 0)
            {
                var blow = new Blow(agent.Index)
                {
                    DamageType = (DamageTypes) attackCollisionData.Data.DamageType,
                    BoneIndex = attackCollisionData.Data.AttackBoneIndex,
                    Position = agent.Position
                };
                blow.Position.z += agent.GetEyeGlobalHeight();
                blow.BaseMagnitude = 0f;
                blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
                blow.InflictedDamage = damage;
                blow.SwingDirection = agent.LookDirection;
                blow.SwingDirection.Normalize();
                blow.Direction = blow.SwingDirection;
                blow.DamageCalculated = true;
                agent.RegisterBlow(blow);
            }
        }

        public override string ToString() 
            => $"{Name}: reflect x{FractionOfDamageToReflect:0.0} ({FractionOfDamageToReflect * 100:0.0}%) of damage dealt";

        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers) => OnActivation(hero, handlers);
        protected override void OnActivation(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null)
            => handlers.OnTakeDamage += OnTakeDamage;
    }
}
