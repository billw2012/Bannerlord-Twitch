using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
	[CategoryOrder("Effect", 11)]
	[CategoryOrder("Targets", 12)]
	[CategoryOrder("Appearance", 13)]
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class AddDamagePower : DurationMissionHeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        [Category("Effect"), Description("How much to multiply base damage by"), PropertyOrder(1),
         UsedImplicitly]
        public float DamageToMultiply { get; set; } = 1f;

        [Category("Effect"), Description("How much damage to add"), PropertyOrder(2), UsedImplicitly]
        public int DamageToAdd { get; set; }
        
        [Category("Effect"), 
         Description("Behaviors to add to the damage"), PropertyOrder(4), ExpandableObject, UsedImplicitly]
        public HitBehavior AddHitBehavior { get; set; }

        [Category("Effect"),
         Description("Behaviors to remove from the damage (e.g. remove Shrug Off to ensure the target is always " +
                     "stunned when hit)"), PropertyOrder(5), ExpandableObject, UsedImplicitly]
        public HitBehavior RemoveHitBehavior { get; set; }

        [Category("Effect"), Description("What fraction (0 to 1) of armor to ignore when applying damage"), 
         PropertyOrder(6),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float ArmorToIgnore { get; set; }
        
        [Category("Effect"), 
         Description("Chance (0 to 1) that the hit will be unblockable"), PropertyOrder(7),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float UnblockableChance { get; set; }
        
        [Category("Effect"), 
         Description("Chance (0 to 1) that the hit will shatter shield if it is blocked"), 
         PropertyOrder(8),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float ShatterShieldChance { get; set; }
        
        [Category("Effect"), 
         Description("Chance (0 to 1) that the hit will cut through any unit it encounters (evaluated on each " +
                     "collision, so a cut through chance of 1 will result in cutting through everyone with every hit)"), 
         PropertyOrder(9),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float CutThroughChance { get; set; }
        
        [Category("Effect"), 
         Description("Chance (0 to 1) that the hit will stagger the agent it hits (hit can either cut through OR " +
                     "stagger, it can't do both, cut through chance is evaluated before this one)"), 
         PropertyOrder(10),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float StaggerChance { get; set; }

        public class AreaOfEffectDef : ICloneable, INotifyPropertyChanged
        {
	        [Description("The radius to apply the damage in"), PropertyOrder(1),
             Range(0, 20), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
             UsedImplicitly]
	        public float Range { get; set; }
	        
	        [Description("Only apply the damage if the attack hits an agent (as opposed to the ground, " +
	                     "e.g. for arrows)"), PropertyOrder(2), UsedImplicitly]
	        public bool OnlyOnHit { get; set; }

	        [Description("Damage at distance 0 from the hit"), PropertyOrder(3),
             Range(0, 500), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
             UsedImplicitly]
	        public float DamageAtCenter { get; set; } = 50;

	        [Description("Maximum number of agents that can be affected"), PropertyOrder(4),
             Range(0, int.MaxValue),
             UsedImplicitly]
	        public int MaxAgentsToDamage { get; set; } = 4;

	        [Description("Damage type"), PropertyOrder(5), UsedImplicitly]
	        public DamageTypes DamageType { get; set; } = DamageTypes.Blunt;
        
	        [Description("Flags to apply to the damage"), PropertyOrder(6), ExpandableObject, UsedImplicitly]
	        public HitBehavior HitBehavior { get; set; }

	        [YamlIgnore, ReadOnly(true)]
	        public bool IsEnabled => Range > 0;
	        
	        [YamlIgnore, ReadOnly(true)]
	        public string Example =>
		        string.Join(", ",
			        Enumerable.Range(0, (int) Math.Min(Range, 20))
				        .Select(i => $"{i}m: {CalculateDamage(i)}dmg"));

	        public override string ToString()
	        {
		        return $"{DamageAtCenter} AoE ({Range}m)";
	        }

	        public object Clone() => CloneHelpers.CloneProperties(this);

	        public void Apply(Agent from, List<Agent> ignoreAgents, Vec3 position)
	        {
		        foreach ((var agent, float distance) in Mission.Current
				        .GetAgentsInRange(position.AsVec2, Range * 1, true)
				        .Where(a => 
						        a.State == AgentState.Active	// alive only
						        && !ignoreAgents.Contains(a)	// not in the ignore list
						        && a.IsEnemyOf(from)			// enemies only
				        )
				        .Select(a => (agent: a, distance: a.Position.Distance(position)))
				        .OrderBy(a => a.distance)
				        // ToList is required due to potential collection change exception when agents are killed below
				        .Take(MaxAgentsToDamage).ToList() 
		        )
		        {
			        int damage = CalculateDamage(distance); 
			        DoAgentDamage(@from, agent, damage, (agent.Position - position).NormalizedCopy(), 
				        DamageType, HitBehavior);
		        }
	        }

	        private int CalculateDamage(float distance)
	        {
		        return (int) (DamageAtCenter / Math.Pow(distance / Range + 1f, 2f));
	        }
            
            public event PropertyChangedEventHandler PropertyChanged;
        }

        [Category("Effect"), 
         Description("Area of Effect damage to apply"), PropertyOrder(20), UsedImplicitly, ExpandableObject]
        public AreaOfEffectDef AoE { get; set; } = new();
        
        [Category("Targets"), 
         Description("Whether to apply this bonus damage against normal troops"), PropertyOrder(13), UsedImplicitly]
        public bool ApplyAgainstNonHeroes { get; set; } = true;
        [Category("Targets"), 
         Description("Whether to apply this bonus damage against heroes"), PropertyOrder(14), UsedImplicitly]
        public bool ApplyAgainstHeroes { get; set; } = true;
        [Category("Targets"), 
         Description("Whether to apply this bonus damage against adopted heroes"), PropertyOrder(15), UsedImplicitly]
        public bool ApplyAgainstAdoptedHeroes { get; set; } = true;
        [Category("Targets"), 
         Description("Whether to apply this bonus damage against the player"), PropertyOrder(16), UsedImplicitly]
        public bool ApplyAgainstPlayer { get; set; } = true;

        [Category("Targets"), 
         Description("Whether to apply this bonus damage when using ranged weapons"), PropertyOrder(17), UsedImplicitly]
        public bool Ranged { get; set; } = true;
        
        [Category("Targets"), 
         Description("Whether to apply this bonus damage when using melee weapons"), PropertyOrder(18), UsedImplicitly]
        public bool Melee { get; set; } = true;
        
        [Category("Targets"), 
         Description("Whether to apply this bonus damage from charge damage"), PropertyOrder(19), UsedImplicitly]
        public bool Charge { get; set; } = true;

        [Category("Appearance"), 
         Description("Particle Effect to attach to the missile (recommend psys_game_burning_agent for trailing " +
                     "fire/smoke effect)"), 
         ItemsSource(typeof(ParticleEffectItemSource)), PropertyOrder(21), UsedImplicitly]
        public string MissileTrailParticleEffect { get; set; }
        
        [Category("Appearance"),
         Description("Effect to play on hit (intended mainly for AoE effects)"), 
         PropertyOrder(22), ExpandableObject, UsedImplicitly]
        public OneShotEffect HitEffect { get; set; }

        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers) 
	        => BLTHeroPowersMissionBehavior.PowerHandler
		        .ConfigureHandlers(hero, this, handlers2 => OnActivation(hero, handlers2));

        protected override void OnActivation(Hero hero, PowerHandler.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null)
        {
	        handlers.OnDoMeleeHit += OnDoMeleeHit;
	        handlers.OnDecideCrushedThrough += OnDecideCrushedThroughDelegate;
	        handlers.OnDecideMissileWeaponFlags += OnDecideMissileWeaponFlags;
	        handlers.OnDoMissileHit += OnDoMissileHit;
	        handlers.OnDecideWeaponCollisionReaction += OnDecideWeaponCollisionReaction;
	        handlers.OnDoDamage += OnDoDamage;
	        handlers.OnMissileCollision += OnMissileCollisionReaction;
	        handlers.OnAddMissile += OnAddMissile;
	        handlers.OnPostDoMeleeHit += OnPostDoMeleeHit;
        }

        private void OnDecideMissileWeaponFlags(Hero attackerHero, Agent attackerAgent, 
	        BLTAgentApplyDamageModel.DecideMissileWeaponFlagsParams args)
        {
	        if (MBRandom.RandomFloat < CutThroughChance)
	        {
		        args.missileWeaponFlags |= WeaponFlags.CanPenetrateShield;
		        args.missileWeaponFlags |= WeaponFlags.MultiplePenetration;
	        }
        }

        private void OnDecideCrushedThroughDelegate(Hero attackerHero, Agent attackerAgent, Hero victimHero, 
	        Agent victimAgent, BLTAgentApplyDamageModel.DecideCrushedThroughParams meleeHitParams)
        {
	        if (UnblockableChance != 0 && MBRandom.RandomFloat < UnblockableChance)
	        {
		        meleeHitParams.crushThrough = true;
	        }
        }

        private void OnDoMissileHit(Hero attackerHero, Agent attackerAgent, Hero victimHero, Agent victimAgent, 
	        BLTHeroPowersMissionBehavior.MissileHitParams missileHitParams)
        {
	        if (IgnoreDamageType(victimHero, victimAgent, missileHitParams.collisionData))
	        {
		        return;
	        }

	        // We remove the shield when its a missile hit, as it won't be checked for removal
	        ApplyShatterShieldChance(victimAgent, ref missileHitParams.collisionData, removeShield: true);
	        
            // Disabling unblockable for missiles for now as it is quite complicated
    	    // if (UnblockableChance != 0 && MBRandom.RandomFloat < UnblockableChance)
            // {
            //     #if e159 || e1510 || e160
    		//     AttackCollisionData.UpdateDataForShieldPenetration(ref missileHitParams.collisionData);
            //     #endif
            // }
        }

        private void OnDoMeleeHit(Hero attackerHero, Agent attackerAgent, Hero victimHero, Agent victimAgent, 
	        BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams)
        {
	        if (IgnoreDamageType(victimHero, victimAgent, meleeHitParams.collisionData))
	        {
		        return;
	        }

	        // We don't remove the shield for melee hit, as it will crash if we do
	        ApplyShatterShieldChance(victimAgent, ref meleeHitParams.collisionData, removeShield: false);
        }
        
        private void OnPostDoMeleeHit(Hero attackerHero, Agent attackerAgent, Hero victimHero, Agent victimAgent, 
	        BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams)
        {
	        if (IgnoreDamageType(victimHero, victimAgent, meleeHitParams.collisionData))
	        {
		        return;
	        }

	        if (UnblockableChance != 0 && MBRandom.RandomFloat < UnblockableChance)
	        {
		        meleeHitParams.inOutMomentumRemaining = 1;
	        }
        }

        private void ApplyShatterShieldChance(Agent victimAgent, ref AttackCollisionData collisionData, bool removeShield)
        {
	        if (collisionData.AttackBlockedWithShield && MBRandom.RandomFloat < ShatterShieldChance)
	        {
		        // just makes sure any missile that hit the shield disappears
		        collisionData.IsShieldBroken = true;
                
                // Hopefully this isn't needed in 161
                #if e159 || e1510 || e160
		        AttackCollisionData.UpdateDataForShieldPenetration(ref collisionData);
                #endif

		        var (element, slotIndex) = victimAgent.Equipment
			        .YieldFilledSlots()
			        .FirstOrDefault(s => s.element.IsShield());
		        if (!element.IsEmpty)
		        {
			        OneShotEffect.Trigger("psys_game_shield_break", "event:/mission/combat/shield/broken",
				        victimAgent.AgentVisuals.GetGlobalFrame()
				        * victimAgent.AgentVisuals.GetSkeleton()
					        .GetBoneEntitialFrame(Game.Current.HumanMonster.OffHandItemBoneIndex)
			        );

			        victimAgent.ChangeWeaponHitPoints(slotIndex, 0);
			        if(removeShield) victimAgent.RemoveEquippedWeapon(slotIndex);
		        }
	        }
        }

        private void OnAddMissile(Hero shooterHero, Agent shooterAgent, RefHandle<WeaponData> weaponData, 
	        WeaponStatsData[] weaponStatsData)
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


        private bool IgnoreDamageType(Hero victimHero, Agent victimAgent, AttackCollisionData attackCollisionData) =>
	        attackCollisionData.IsFallDamage
	        || !ApplyAgainstAdoptedHeroes && victimHero != null
	        || !ApplyAgainstHeroes && victimAgent.IsHero
	        || !ApplyAgainstNonHeroes && !victimAgent.IsHero
	        || !ApplyAgainstPlayer && victimAgent == Agent.Main
	        || !Melee && !(attackCollisionData.IsMissile || attackCollisionData.IsHorseCharge)
	        || !Ranged && attackCollisionData.IsMissile
	        || !Charge && attackCollisionData.IsHorseCharge;

        // NEED TO OVERRIDE MissileHitCallback to change blocking behavior for missiles

        private void OnDoDamage(Hero hero, Agent agent, Hero victimHero, Agent victimAgent, 
            BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams)
        {
            if (IgnoreDamageType(victimHero, victimAgent, blowParams.collisionData))
            {
                return;
            }

            blowParams.collisionData.AbsorbedByArmor = (int) (blowParams.blow.AbsorbedByArmor *= 1 - ArmorToIgnore);
            blowParams.collisionData.BaseMagnitude = blowParams.blow.BaseMagnitude 
                = (int) (blowParams.blow.BaseMagnitude * DamageToMultiply + DamageToAdd);
            blowParams.collisionData.InflictedDamage = blowParams.blow.InflictedDamage
                = (int) (blowParams.blow.BaseMagnitude - blowParams.blow.AbsorbedByArmor);
            
            blowParams.blow.BlowFlag |= AddHitBehavior.Generate(victimAgent);
            blowParams.blow.BlowFlag &= ~RemoveHitBehavior.Generate(victimAgent);

            // If attack type is a missile and AoE is not set to only on hit, then we will be applying this in the
            // OnMissileCollisionReaction below
            if (!blowParams.collisionData.IsMissile || AoE.OnlyOnHit)
            {
	            DoAoE(agent, victimAgent, 
		            new MatrixFrame(Mat3.Identity, blowParams.collisionData.CollisionGlobalPosition));
            }
        }

        private void OnDecideWeaponCollisionReaction(Hero attackerHero, Agent attackerAgent, Hero victimHero, 
	        Agent victimAgent, 
	        BLTHeroPowersMissionBehavior.DecideWeaponCollisionReactionParams decideWeaponCollisionReactionParams)
        {
	        if (MBRandom.RandomFloat < CutThroughChance)
	        {
		        decideWeaponCollisionReactionParams.colReaction = MeleeCollisionReaction.SlicedThrough;
	        }
	        else if (MBRandom.RandomFloat < StaggerChance)
	        {
		        decideWeaponCollisionReactionParams.colReaction = MeleeCollisionReaction.Staggered;
	        }
        }

        private void OnMissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction, Hero attackerHero,
	        Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex, bool attachedToShield, 
	        MatrixFrame attachLocalFrame, Mission.Missile missile)
        {
	        if (Ranged && !AoE.OnlyOnHit)
	        {
		        DoAoE(attackerAgent, attachedAgent, 
			        attachedAgent?.Frame.TransformToParent(attachLocalFrame) ?? attachLocalFrame);
	        }
        }

        private void DoAoE(Agent attackerAgent, Agent attackedAgent, MatrixFrame globalFrame)
        {
	        HitEffect.Trigger(globalFrame);

	        if (AoE.IsEnabled)
	        {
		        AoE.Apply(attackerAgent, new() {attackerAgent, attackedAgent}, globalFrame.origin);
	        }
        }

        private static void DoAgentDamage(Agent from, Agent agent, int damage, Vec3 direction, 
	        DamageTypes damageType, HitBehavior hitBehavior)
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
		        BlowFlag = hitBehavior.Generate(agent),
	        };

	        agent.RegisterBlow(blow);
        }

        public override string ToString() => $"{Name}: {ToStringInternal()}";
        
        private string ToStringInternal()
        {
	        var appliesToList = new List<string>();
            if (ApplyAgainstNonHeroes) appliesToList.Add("Non-heroes");
            if (ApplyAgainstHeroes) appliesToList.Add("Heroes");
            if (ApplyAgainstAdoptedHeroes) appliesToList.Add("Adopted");
            if (ApplyAgainstPlayer) appliesToList.Add("Player");

            var appliesFromList = new List<string>();
            if (Ranged) appliesFromList.Add("Ranged");
            if (Melee) appliesFromList.Add("Melee");
            if (Charge) appliesFromList.Add("Charge");

            var modifiers = new List<string>();
            
            if (DamageToMultiply != 1) modifiers.Add($"+{DamageToMultiply * 100:0.0}%");
            if (DamageToAdd != 0) modifiers.Add($"+{DamageToAdd}");
            if (AoE.Range != 0) appliesFromList.Add(AoE.ToString());

            return $"{string.Join(" / ", modifiers)} " +
                   $"{string.Join("/", appliesFromList)} dmg to {string.Join(", ", appliesToList)}";
        }

        public void GenerateDocumentation(IDocumentationGenerator generator) => generator.P(ToStringInternal());
    }
}