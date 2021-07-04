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
        public int DamageToAdd { get; set; } = 0;

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
        
        [Category("Power Config"), Description("Whether to apply this bonus damage against the player"), PropertyOrder(10), UsedImplicitly]
        public float AreaOfEffect { get; set; }

        [Description("Effect to play on hit (intended mainly for AoE effects)"), PropertyOrder(10), ExpandableObject, UsedImplicitly]
        public OneShotEffect HitEffect { get; set; } = new();
        
        public AddDamagePower()
        {
            Type = new ("378648B6-5586-4812-AD08-22DA6374440C");
        }

        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers1)
        {
            BLTHeroPowersMissionBehavior.Current
                .ConfigureHandlers(hero, this, handlers => handlers.OnDoDamage += OnDoDamage);
        }
        
        protected override void OnActivation(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null) 
            => handlers.OnDoDamage += OnDoDamage;

        public void OnDoDamage(Hero hero, Agent agent, Hero victimHero, Agent victimAgent, AttackCollisionDataRef attackCollisionData)
        {
            if (!ApplyAgainstAdoptedHeroes && victimHero != null
                || !ApplyAgainstHeroes && victimAgent.IsHero
                || !ApplyAgainstNonHeroes && !victimAgent.IsHero
                || !ApplyAgainstPlayer && victimAgent == Agent.Main
                || !Ranged && attackCollisionData.Data.IsMissile
                || !Melee && !(attackCollisionData.Data.IsMissile || attackCollisionData.Data.IsFallDamage || attackCollisionData.Data.IsHorseCharge)
                || !Charge && attackCollisionData.Data.IsHorseCharge
                )
            {
                return;
            }
            //attackCollisionData.BaseMagnitude = (int) (attackCollisionData.BaseMagnitude * DamageToMultiply);
            attackCollisionData.Data.InflictedDamage = (int) (attackCollisionData.Data.InflictedDamage * DamageToMultiply);
            //attackCollisionData.BaseMagnitude += DamageToAdd;
            attackCollisionData.Data.InflictedDamage += DamageToAdd;
            
            HitEffect.Trigger(victimAgent);

            if (AreaOfEffect > 0 && agent != null)
            {
	            DoAoEDamage(agent, new() { agent, victimAgent }, attackCollisionData.Data.InflictedDamage, AreaOfEffect, ref attackCollisionData.Data);
            }
        }

        public static void DoAgentDamage(Agent from, Agent agent, int damage, Vec3 direction, ref AttackCollisionData data)
        {
	        var blow = new Blow(from.Index)
	        {
		        DamageType = (DamageTypes) data.DamageType,
		        BoneIndex = agent.Monster.HeadLookDirectionBoneIndex,
		        Position = agent.Position,
		        BaseMagnitude = data.BaseMagnitude,
		        InflictedDamage = damage,
		        SwingDirection = direction,
		        Direction = direction,
		        DamageCalculated = true,
		        VictimBodyPart = BoneBodyPartType.Chest,
	        };

	        agent.RegisterBlow(blow);
        }
        
        public static void DoAoEDamage(Agent from, List<Agent> ignoreAgents, float maxDamage, float range, ref AttackCollisionData data)
        {
	        foreach (var agent in Mission.Current
		        .GetAgentsInRange(data.CollisionGlobalPosition.AsVec2, range, true)
		        .Where(a => a.State == AgentState.Active 
		                    && !ignoreAgents.Contains(a))
		        .ToList() // ToList is required due to potential collection change exception when agents are killed below
	        )
	        {
		        float distance = agent.Position.Distance(data.CollisionGlobalPosition);
		        int damage = (int) (maxDamage / Math.Pow(distance / range + 1f, 2f));
		        DoAgentDamage(from, agent, damage, (agent.Position - data.CollisionGlobalPosition).NormalizedCopy(), ref data);
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