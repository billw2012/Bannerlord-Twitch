using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class AddDamagePower : DurationMissionHeroPowerDefBase, IHeroPowerPassive
    {
        [Category("Power Config"), Description("How much to multiply base damage by"), PropertyOrder(1), UsedImplicitly]
        public float DamageToMultiply { get; set; } = 1f;

        [Category("Power Config"), Description("How much damage to add"), PropertyOrder(2), UsedImplicitly]
        public int DamageToAdd { get; set; }

        [Category("Power Config"), Description("Whether to apply this bonus damage against normal troops"), PropertyOrder(3), UsedImplicitly]
        public bool ApplyAgainstNonHeroes { get; set; } = true;
        [Category("Power Config"), Description("Whether to apply this bonus damage against heroes"), PropertyOrder(4), UsedImplicitly]
        public bool ApplyAgainstHeroes { get; set; } = true;
        [Category("Power Config"), Description("Whether to apply this bonus damage against adopted heroes"), PropertyOrder(5), UsedImplicitly]
        public bool ApplyAgainstAdoptedHeroes { get; set; } = true;
        [Category("Power Config"), Description("Whether to apply this bonus damage against the player"), PropertyOrder(6), UsedImplicitly]
        public bool ApplyAgainstPlayer { get; set; } = true;

        [Category("Power Config"), Description("Whether to apply this bonus damage when using ranged weapons"), PropertyOrder(7), UsedImplicitly]
        public bool Ranged { get; set; } = true;
        
        [Category("Power Config"), Description("Whether to apply this bonus damage when using melee weapons"), PropertyOrder(8), UsedImplicitly]
        public bool Melee { get; set; } = true;
        
        [Category("Power Config"), Description("Whether to apply this bonus damage from charge damage"), PropertyOrder(9), UsedImplicitly]
        public bool Charge { get; set; } = true;
        
        [Category("Power Config"), Description("Only apply the AoE damage if the attack hits an agent (as opposed to the ground, e.g. for arrows)"), PropertyOrder(10), UsedImplicitly]
        public bool AreaOfEffectOnlyOnHit { get; set; }

        [Category("Power Config"), Description("Whether to apply this bonus damage against the player"), PropertyOrder(11), UsedImplicitly]
        public float AreaOfEffect { get; set; }

        [Category("Power Config"), Description("Damage the AoE causes at distance 0 from the hit"), PropertyOrder(12), UsedImplicitly]
        public float AreaOfEffectDamage { get; set; } = 30;
        
        [Category("Power Config"), Description("Maximum number of agents that can be affected by the AoE"), PropertyOrder(13), UsedImplicitly]
        public int AreaOfEffectMaxAgents { get; set; } = 5;

        [Category("Power Config"), Description("Damage type the AoE causes"), PropertyOrder(14), UsedImplicitly]
        public DamageTypes AreaOfEffectDamageType { get; set; } = DamageTypes.Blunt;
        
        [Category("Power Config"), Description("Particle Effect to attach to the missile (recommend psys_game_burning_agent for trailing fire/smoke effect)"), 
         ItemsSource(typeof(ParticleEffectItemSource)), PropertyOrder(15), UsedImplicitly]
        public string MissileTrailParticleEffect { get; set; }
        
        [Description("Effect to play on hit (intended mainly for AoE effects)"), PropertyOrder(16), ExpandableObject, UsedImplicitly]
        public OneShotEffect HitEffect { get; set; } = new();
        
        public AddDamagePower()
        {
            Type = new ("378648B6-5586-4812-AD08-22DA6374440C");
        }

        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers1) 
	        => BLTHeroPowersMissionBehavior.Current
		        .ConfigureHandlers(hero, this, handlers => OnActivation(hero, handlers));

        protected override void OnActivation(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null)
        {
	        handlers.OnDoDamage += OnDoDamage;
	        handlers.OnMissileCollision += OnMissileCollisionReaction;
	        handlers.OnAddMissile += OnAddMissile;
        }

        private void OnAddMissile(Hero shooterHero, Agent shooterAgent, WeaponDataRef weaponData, WeaponStatsData[] weaponStatsData)
        {
	        if (!string.IsNullOrEmpty(MissileTrailParticleEffect))
	        {
		        weaponData.Data.TrailParticleName = MissileTrailParticleEffect;
		        for (int i = 0; i < weaponStatsData.Length; i++)
		        {
			        weaponStatsData[i].WeaponFlags |= (ulong) (WeaponFlags.Burning | WeaponFlags.LeavesTrail);
		        }
	        }
        }

        private void OnDoDamage(Hero hero, Agent agent, Hero victimHero, Agent victimAgent, AttackCollisionDataRef attackCollisionData)
        {
            if (attackCollisionData.Data.IsFallDamage
                || !ApplyAgainstAdoptedHeroes && victimHero != null
                || !ApplyAgainstHeroes && victimAgent.IsHero
                || !ApplyAgainstNonHeroes && !victimAgent.IsHero
                || !ApplyAgainstPlayer && victimAgent == Agent.Main
                || !Melee && !(attackCollisionData.Data.IsMissile || attackCollisionData.Data.IsHorseCharge)
                || !Ranged && attackCollisionData.Data.IsMissile
                || !Charge && attackCollisionData.Data.IsHorseCharge
                )
            {
                return;
            }

            attackCollisionData.Data.InflictedDamage = (int) (attackCollisionData.Data.InflictedDamage * DamageToMultiply + DamageToAdd);

            // If attack type is a missile and AoE is not set to only on hit, then we will be applying this in the OnMissileCollisionReaction below
            if (!attackCollisionData.Data.IsMissile || AreaOfEffectOnlyOnHit)
            {
	            DoAoE(agent, victimAgent, new MatrixFrame(Mat3.Identity, attackCollisionData.Data.CollisionGlobalPosition));
            }
        }

        private void OnMissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction, Hero attackerHero, Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex, bool attachedToShield, MatrixFrame attachLocalFrame, Mission.Missile missile)
        {
	        if (Ranged && !AreaOfEffectOnlyOnHit)
	        {
		        DoAoE(attackerAgent, attachedAgent, attachLocalFrame);
	        }
        }

        private void DoAoE(Agent attackerAgent, Agent attackedAgent, MatrixFrame hitLocalFrame)
        {
	        HitEffect.Trigger(hitLocalFrame);

	        if (AreaOfEffect > 0 && AreaOfEffectDamage > 0)
	        {
		        DoAoEDamage(attackerAgent, new() {attackerAgent, attackedAgent}, AreaOfEffectMaxAgents, AreaOfEffectDamage, AreaOfEffect,
			        hitLocalFrame.origin, AreaOfEffectDamageType);
	        }
        }

        private static void DoAgentDamage(Agent from, Agent agent, int damage, Vec3 direction, DamageTypes damageType)
        {
	        var blow = new Blow(from.Index)
	        {
		        DamageType = damageType,
		        BoneIndex = agent.Monster.HeadLookDirectionBoneIndex,
		        Position = agent.Position,
		        BaseMagnitude = damage,
		        InflictedDamage = damage,
		        SwingDirection = direction,
		        Direction = direction,
		        DamageCalculated = true,
		        VictimBodyPart = BoneBodyPartType.Chest,
	        };

	        agent.RegisterBlow(blow);
        }
        
        private static void DoAoEDamage(Agent from, List<Agent> ignoreAgents, int maxAgent, float maxDamage, float range, Vec3 position, DamageTypes damageType)
        {
	        foreach ((var agent, float distance) in Mission.Current
		        .GetAgentsInRange(position.AsVec2, range * 1, true)
		        .Where(a => 
			        a.State == AgentState.Active	// alive only
			        && !ignoreAgents.Contains(a)	// not in the ignore list
			        && a.IsEnemyOf(from)			// enemies only
			        )
		        .Select(a => (agent: a, distance: a.Position.Distance(position)))
		        .OrderByDescending(a => a.distance)
		        .Take(maxAgent).ToList() // ToList is required due to potential collection change exception when agents are killed below
	        )
	        {
		        int damage = (int) (maxDamage / Math.Pow(distance / range + 1f, 2f)); 
		        DoAgentDamage(from, agent, damage, (agent.Position - position).NormalizedCopy(), damageType);
	        }
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (DamageToMultiply != 1)
            {
                parts.Add($"x{DamageToMultiply:0.0} ({DamageToMultiply * 100:0.0}%) health");
            }
            if (DamageToAdd != 0)
            {
                parts.Add(DamageToAdd > 0 ? $"+{DamageToAdd} damage" : $"{DamageToAdd} damage");
            }
            if (ApplyAgainstNonHeroes) parts.Add("Non-heroes");
            if (ApplyAgainstHeroes) parts.Add("Heroes");
            if (ApplyAgainstAdoptedHeroes) parts.Add("Adopted");
            if (ApplyAgainstPlayer) parts.Add("Player");
            if (Ranged) parts.Add("Ranged");
            if (Melee) parts.Add("Melee");
            if (Charge) parts.Add("Charge");
            if (AreaOfEffect != 0) parts.Add($"AoE {AreaOfEffect}");
            return $"{Name}: {string.Join(", ", parts)}";
        }
    }
}