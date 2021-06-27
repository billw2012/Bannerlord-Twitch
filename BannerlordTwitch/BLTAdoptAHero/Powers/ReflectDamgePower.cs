using System.ComponentModel;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class ReflectDamagePower : HeroPowerDefBase, IHeroPowerPassive
    {
        [Category("Power Config"), Description("What fraction of damage to reflect back to attacker"), PropertyOrder(1), UsedImplicitly]
        public float FractionOfDamageToReflect { get; set; } = 0.1f;
        
        public ReflectDamagePower()
        {
            Type = new ("FFE07DA3-E977-42D8-80CA-5DFFF66123EB");
        }

        public void OnAdded(Hero hero) {}
        public void OnRemoved(Hero hero) {}
        public void OnBattleStart(Hero hero) {}
        public void OnBattleTick(Hero hero, Agent agent) {}
        public void OnBattleEnd(Hero hero) {}
        public void OnAgentBuild(Hero hero, Agent agent) {}
        public void OnAgentKilled(Hero hero, Agent agent, Hero killerHero, Agent killerAgent) {}
        public void OnDoDamage(Hero hero, Agent agent, Hero victimHero, Agent victimAgent, ref AttackCollisionData attackCollisionData)
        { }

        public void OnTakeDamage(Hero hero, Agent agent, Hero attackerHero, Agent attackerAgent,
            ref AttackCollisionData attackCollisionData)
        {
            int damage = (int) (attackCollisionData.InflictedDamage * FractionOfDamageToReflect);
            if (damage > 0)
            {
                var blow = new Blow(agent.Index);
                blow.DamageType = (DamageTypes) attackCollisionData.DamageType;
                //blow.BlowFlag = BlowFlags.CrushThrough;
                //blow.BlowFlag |= BlowFlags.KnockDown;
                blow.BoneIndex = attackCollisionData.AttackBoneIndex;
                blow.Position = agent.Position;
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

        public override string ToString() => $"{Name}: reflect x{FractionOfDamageToReflect:0.0} ({FractionOfDamageToReflect * 100:0.0}%) of damage dealt";
    }
}