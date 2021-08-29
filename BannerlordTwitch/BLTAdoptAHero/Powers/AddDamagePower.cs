using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
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
    [CategoryOrder("Effect", 3), 
     CategoryOrder("Targets", 4), 
     CategoryOrder("Appearance", 5),
     LocDisplayName("{=JgPVRq5W}Add Damage Power"),
     LocDescription("{=RGUwxlOx}Adds fixed or relative amount of extra HP to the hero when they spawn"), 
     UsedImplicitly]
    public class AddDamagePower : DurationMissionHeroPowerDefBase, IHeroPowerPassive, IDocumentable
    {
        #region User Editable
        [LocDisplayName("{=0mhPe31m}Damage Modifier Percent"),
         LocCategory("Effect", "{=VBuncBq5}Effect"),
         LocDescription("{=tXh8oAaE}Damage modifier (set less than 100% to reduce damage, set greater than 100% to increase it)"),
         UIRangeAttribute(0, 500, 5f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(1), UsedImplicitly]
        public float DamageModifierPercent { get; set; } = 100f;

        [LocDisplayName("{=fPmJAchN}Damage To Add"),
         LocCategory("Effect", "{=VBuncBq5}Effect"), 
         LocDescription("{=lEbWq1of}How much damage to add"), 
         PropertyOrder(2), UsedImplicitly]
        public int DamageToAdd { get; set; }
        
        [LocDisplayName("{=RnGUD1jl}Add Hit Behavior"),
         LocCategory("Effect", "{=VBuncBq5}Effect"), 
         LocDescription("{=bse0NB6Z}Behaviors to add to the damage"), 
         ExpandableObject, PropertyOrder(4), UsedImplicitly]
        public HitBehavior AddHitBehavior { get; set; }

        [LocDisplayName("{=shUGx48n}Remove Hit Behavior"),
         LocCategory("Effect", "{=VBuncBq5}Effect"),
         LocDescription("{=feiVAent}Behaviors to remove from the damage (e.g. remove Shrug Off to ensure the target is always stunned when hit)"), 
         ExpandableObject, PropertyOrder(5), UsedImplicitly]
        public HitBehavior RemoveHitBehavior { get; set; }

        [LocDisplayName("{=w5j3JYog}Armor To Ignore Percent"),
         LocCategory("Effect", "{=VBuncBq5}Effect"), 
         LocDescription("{=Sv4QLgYX}What fraction (0 to 1) of armor to ignore when applying damage"), 
         UIRangeAttribute(0, 100, 1f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(6), UsedImplicitly]
        public float ArmorToIgnorePercent { get; set; }
        
        [LocDisplayName("{=SOJgZOV3}Unblockable Chance Percent"),
         LocCategory("Effect", "{=VBuncBq5}Effect"), 
         LocDescription("{=QoYMwBnz}Chance (0 to 1) that the hit will be unblockable"), 
         UIRangeAttribute(0, 100, 1f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(7), UsedImplicitly]
        public float UnblockableChancePercent { get; set; }
        
        [LocDisplayName("{=L1ZqypsT}Shatter Shield Chance Percent"),
         LocCategory("Effect", "{=VBuncBq5}Effect"), 
         LocDescription("{=MaDRQPhh}Chance (0 to 1) that the hit will shatter shield if it is blocked"), 
         UIRangeAttribute(0, 100, 1f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(8), UsedImplicitly]
        public float ShatterShieldChancePercent { get; set; }
        
        [LocDisplayName("{=xt70CI4q}Cut Through Chance Percent"),
         LocCategory("Effect", "{=VBuncBq5}Effect"), 
         LocDescription("{=uAsNXThU}Chance (0 to 1) that the hit will cut through any unit it encounters (evaluated on each collision, so a cut through chance of 1 will result in cutting through everyone with every hit)"), 
         UIRangeAttribute(0, 100, 1f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(9), UsedImplicitly]
        public float CutThroughChancePercent { get; set; }
        
        [LocDisplayName("{=XxLPeI3i}Stagger Chance Percent"),
         LocCategory("Effect", "{=VBuncBq5}Effect"), 
         LocDescription("{=8N18Zs1O}Chance (0 to 1) that the hit will stagger the agent it hits (hit can either cut through OR stagger, it can't do both, cut through chance is evaluated before this one)"), 
         UIRangeAttribute(0, 100, 1f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(10), UsedImplicitly]
        public float StaggerChancePercent { get; set; }

        [LocDisplayName("{=O4AUEtAO}AoE"),
         LocCategory("Effect", "{=VBuncBq5}Effect"), 
         LocDescription("{=5ke30xUG}Area of Effect damage to apply"),
         ExpandableObject, PropertyOrder(20), UsedImplicitly]
        public AreaOfEffectDef AoE { get; set; } = new();
        
        [LocDisplayName("{=0WAJwEvc}Apply Against Non Heroes"),
         LocCategory("Targets", "{=R7rU0TbD}Targets"), 
         LocDescription("{=guccTOSR}Whether to apply this bonus damage against normal troops"), 
         PropertyOrder(13), UsedImplicitly]
        public bool ApplyAgainstNonHeroes { get; set; } = true;
        [LocDisplayName("{=mMcjJkph}Apply Against Heroes"),
         LocCategory("Targets", "{=R7rU0TbD}Targets"), 
         LocDescription("{=gYU28V1d}Whether to apply this bonus damage against heroes"), 
         PropertyOrder(14), UsedImplicitly]
        public bool ApplyAgainstHeroes { get; set; } = true;
        [LocDisplayName("{=En7CxSs6}Apply Against Adopted Heroes"),
         LocCategory("Targets", "{=R7rU0TbD}Targets"), 
         LocDescription("{=DcMwYE76}Whether to apply this bonus damage against adopted heroes"), 
         PropertyOrder(15), UsedImplicitly]
        public bool ApplyAgainstAdoptedHeroes { get; set; } = true;
        [LocDisplayName("{=s3sYJipB}Apply Against Player"),
         LocCategory("Targets", "{=R7rU0TbD}Targets"), 
         LocDescription("{=1sYObRIg}Whether to apply this bonus damage against the player"), 
         PropertyOrder(16), UsedImplicitly]
        public bool ApplyAgainstPlayer { get; set; } = true;

        [LocDisplayName("{=ITaXWnRJ}Ranged"),
         LocCategory("Targets", "{=R7rU0TbD}Targets"), 
         LocDescription("{=pfagbSNR}Whether to apply this bonus damage when using ranged weapons"), 
         PropertyOrder(17), UsedImplicitly]
        public bool Ranged { get; set; } = true;
        
        [LocDisplayName("{=6pT4TAXW}Melee"),
         LocCategory("Targets", "{=R7rU0TbD}Targets"), 
         LocDescription("{=TiGzhjIl}Whether to apply this bonus damage when using melee weapons"), 
         PropertyOrder(18), UsedImplicitly]
        public bool Melee { get; set; } = true;
        
        [LocDisplayName("{=JR0Rdbfj}Charge"),
         LocCategory("Targets", "{=R7rU0TbD}Targets"), 
         LocDescription("{=65y7VWcS}Whether to apply this bonus damage from charge damage"), 
         PropertyOrder(19), UsedImplicitly]
        public bool Charge { get; set; } = true;

        [LocDisplayName("{=2GaGbTYR}Missile Trail Particle Effect"),
         LocCategory("Appearance", "{=umALVhJG}Appearance"), 
         LocDescription("{=UiCylfED}Particle Effect to attach to the missile (recommend psys_game_burning_agent for trailing fire/smoke effect)"), 
         ItemsSource(typeof(LoopingParticleEffectItemSource)),
         PropertyOrder(21), UsedImplicitly]
        public string MissileTrailParticleEffect { get; set; }
        
        [LocDisplayName("{=gEhH84HS}Hit Effect"),
         LocCategory("Appearance", "{=umALVhJG}Appearance"),
         LocDescription("{=r6D9pDIA}Effect to play on hit (intended mainly for AoE effects)"), 
         ExpandableObject, PropertyOrder(22), UsedImplicitly]
        public OneShotEffect HitEffect { get; set; }
        #endregion

        #region IHeroPowerPassive
        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, PowerHandler.Handlers handlers) 
	        => BLTHeroPowersMissionBehavior.PowerHandler
		        .ConfigureHandlers(hero, this, handlers2 => OnActivation(hero, handlers2));
        #endregion

        #region Implementation Details
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

        private void OnDecideMissileWeaponFlags(Agent attackerAgent, 
	        BLTAgentApplyDamageModel.DecideMissileWeaponFlagsParams args)
        {
	        if (CutThroughChancePercent != 0 && MBRandom.RandomFloat * 100f < CutThroughChancePercent)
	        {
		        args.missileWeaponFlags |= WeaponFlags.CanPenetrateShield;
		        args.missileWeaponFlags |= WeaponFlags.MultiplePenetration;
	        }
        }

        private void OnDecideCrushedThroughDelegate(Agent attackerAgent, 
	        Agent victimAgent, BLTAgentApplyDamageModel.DecideCrushedThroughParams meleeHitParams)
        {
	        if (CutThroughChancePercent != 0 && MBRandom.RandomFloat * 100f < UnblockableChancePercent)
	        {
		        meleeHitParams.crushThrough = true;
	        }
        }

        private void OnDoMissileHit(Agent attackerAgent, Agent victimAgent, 
	        BLTHeroPowersMissionBehavior.MissileHitParams missileHitParams)
        {
	        if (IgnoreDamageType(victimAgent, missileHitParams.collisionData))
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

        private void OnDoMeleeHit(Agent attackerAgent, Agent victimAgent, 
	        BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams)
        {
	        if (IgnoreDamageType(victimAgent, meleeHitParams.collisionData))
	        {
		        return;
	        }

	        // We don't remove the shield for melee hit, as it will crash if we do
	        ApplyShatterShieldChance(victimAgent, ref meleeHitParams.collisionData, removeShield: false);
        }
        
        private void OnPostDoMeleeHit(Agent attackerAgent, Agent victimAgent, 
	        BLTHeroPowersMissionBehavior.MeleeHitParams meleeHitParams)
        {
	        if (IgnoreDamageType(victimAgent, meleeHitParams.collisionData))
	        {
		        return;
	        }

	        if (UnblockableChancePercent != 0 && MBRandom.RandomFloat * 100f < UnblockableChancePercent)
	        {
		        meleeHitParams.inOutMomentumRemaining = 1;
	        }
        }

        private void ApplyShatterShieldChance(Agent victimAgent, ref AttackCollisionData collisionData, bool removeShield)
        {
	        if (collisionData.AttackBlockedWithShield 
                && ShatterShieldChancePercent != 0 
                && MBRandom.RandomFloat * 100f < ShatterShieldChancePercent)
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

        private void OnAddMissile(Agent shooterAgent, RefHandle<WeaponData> weaponData, 
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


        private bool IgnoreDamageType(Agent victimAgent, AttackCollisionData attackCollisionData)
        {
            return victimAgent == null 
                   || attackCollisionData.IsFallDamage
                   || !ApplyAgainstAdoptedHeroes && victimAgent.IsAdopted()
                   || !ApplyAgainstHeroes && victimAgent.IsHero
                   || !ApplyAgainstNonHeroes && !victimAgent.IsHero
                   || !ApplyAgainstPlayer && victimAgent == Agent.Main
                   || !Melee && !(attackCollisionData.IsMissile || attackCollisionData.IsHorseCharge)
                   || !Ranged && attackCollisionData.IsMissile
                   || !Charge && attackCollisionData.IsHorseCharge;
        }

        private void OnDoDamage(Agent agent, Agent victimAgent, 
            BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams)
        {
            if (IgnoreDamageType(victimAgent, blowParams.collisionData))
            {
                return;
            }

            ApplyDamageEffects(victimAgent, blowParams, ArmorToIgnorePercent, DamageModifierPercent, DamageToAdd, AddHitBehavior, RemoveHitBehavior);

            // If attack type is a missile and AoE is not set to only on hit, then we will be applying this in the
            // OnMissileCollisionReaction below
            if (!blowParams.collisionData.IsMissile || AoE.OnlyOnHit)
            {
	            DoAoE(agent, victimAgent, 
		            new MatrixFrame(Mat3.Identity, blowParams.collisionData.CollisionGlobalPosition));
            }
        }

        public static void ApplyDamageEffects(Agent victimAgent, BLTHeroPowersMissionBehavior.RegisterBlowParams blowParams, float armorToIgnorePercent, float damageModifierPercent, int damageToAdd, HitBehavior addHitBehavior, HitBehavior removeHitBehavior)
        {
            if (armorToIgnorePercent != 0)
            {
                float additionalDamage = blowParams.blow.AbsorbedByArmor * armorToIgnorePercent / 100f;
                blowParams.collisionData.AbsorbedByArmor =
                    (int)(blowParams.blow.AbsorbedByArmor = blowParams.blow.AbsorbedByArmor - additionalDamage);
                blowParams.collisionData.BaseMagnitude =
                    blowParams.blow.BaseMagnitude = blowParams.blow.BaseMagnitude + additionalDamage;
                blowParams.collisionData.InflictedDamage = 
                    blowParams.blow.InflictedDamage = blowParams.blow.InflictedDamage + (int)additionalDamage;
            }

            if (damageModifierPercent != 100)
            {
                blowParams.collisionData.BaseMagnitude = blowParams.blow.BaseMagnitude =
                    blowParams.blow.BaseMagnitude * damageModifierPercent / 100f;
                blowParams.collisionData.InflictedDamage = blowParams.blow.InflictedDamage =
                    (int)(blowParams.blow.InflictedDamage * damageModifierPercent / 100f);
            }

            if (damageToAdd != 0)
            {
                blowParams.collisionData.BaseMagnitude =
                    blowParams.blow.BaseMagnitude = Math.Max(0, blowParams.blow.BaseMagnitude + damageToAdd);
                blowParams.collisionData.InflictedDamage =
                    blowParams.blow.InflictedDamage = Math.Max(0, blowParams.blow.InflictedDamage + damageToAdd);
            }

            blowParams.blow.BlowFlag = addHitBehavior.AddFlags(victimAgent,
                removeHitBehavior.RemoveFlags(victimAgent, blowParams.blow.BlowFlag));
        }

        private void OnDecideWeaponCollisionReaction(Agent attackerAgent, 
	        Agent victimAgent, 
	        BLTHeroPowersMissionBehavior.DecideWeaponCollisionReactionParams decideWeaponCollisionReactionParams)
        {
	        if (MBRandom.RandomFloat * 100f < CutThroughChancePercent)
	        {
		        decideWeaponCollisionReactionParams.colReaction = MeleeCollisionReaction.SlicedThrough;
	        }
	        else if (MBRandom.RandomFloat * 100f < StaggerChancePercent)
	        {
		        decideWeaponCollisionReactionParams.colReaction = MeleeCollisionReaction.Staggered;
	        }
        }

        private void OnMissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction,
	        Agent attackerAgent, Agent attachedAgent, sbyte attachedBoneIndex, bool attachedToShield, 
	        MatrixFrame attachLocalFrame, Mission.Missile missile)
        {
	        if (Ranged && !AoE.OnlyOnHit)
	        {
		        DoAoE(attackerAgent, attachedAgent, 
                    collisionReaction == Mission.MissileCollisionReaction.Stick && attachedAgent != null
                        ? attachedAgent.Frame.TransformToParent(attachLocalFrame) 
                        : attachLocalFrame);
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

        public static void DoAgentDamage(Agent from, Agent agent, int damage, Vec3 direction, 
	        DamageTypes damageType, HitBehavior hitBehavior)
        {
	        var blow = new Blow(from.Index)
	        {
                AttackType = from.IsMount ? AgentAttackType.Collision : AgentAttackType.Standard,
		        DamageType = from.IsMount ? DamageTypes.Blunt : damageType,
		        BoneIndex = agent.Monster.HeadLookDirectionBoneIndex,
		        Position = agent.Position,
		        BaseMagnitude = damage,
		        InflictedDamage = damage,
		        SwingDirection = direction,
		        Direction = direction,
		        DamageCalculated = true,
		        VictimBodyPart = BoneBodyPartType.Chest,
		        BlowFlag = hitBehavior.AddFlags(agent, BlowFlags.None),
                WeaponRecord = new () { AffectorWeaponSlotOrMissileIndex = -1 }
            };

	        agent.RegisterBlow(blow);
        }
        #endregion

        #region Public Interface
        [YamlIgnore, Browsable(false)]
        public override LocString Description
        {
            get
            {
                var appliesToList = new List<string>();
                if (!ApplyAgainstNonHeroes || !ApplyAgainstHeroes || !ApplyAgainstAdoptedHeroes || !ApplyAgainstPlayer)
                {
                    if (ApplyAgainstNonHeroes) appliesToList.Add("{=RNh2zns4}Non-heroes".Translate());
                    if (ApplyAgainstHeroes) appliesToList.Add("{=qWIzKnVw}Heroes".Translate());
                    if (ApplyAgainstAdoptedHeroes) appliesToList.Add("{=djDGvCdQ}Adopted".Translate());
                    if (ApplyAgainstPlayer) appliesToList.Add("{=8nDvdozx}Player".Translate());
                }

                var appliesFromList = new List<string>();
                if (!Ranged || !Melee || !Charge)
                {
                    if (Ranged) appliesFromList.Add("{=1t1hdpPr}Ranged".Translate());
                    if (Melee) appliesFromList.Add("{=ibrr3fCK}Melee".Translate());
                    if (Charge) appliesFromList.Add("{=X8RrfG7V}Charge".Translate());
                }
                
                var modifiers = new List<string>();
                
                if (DamageModifierPercent != 100)
                    modifiers.Add("{=LbHOSj1l}{DamageModifierPercent}% dmg"
                        .Translate(("DamageModifierPercent", DamageModifierPercent.ToString("0.0"))));
                if (DamageToAdd != 0)
                    modifiers.Add("{=mBmFK0OK}{DamageToAdd} dmg"
                        .Translate(("DamageToAdd", (DamageToAdd > 0 ? "+" : "") + DamageToAdd)));
                if (AddHitBehavior.IsEnabled)
                    modifiers.Add("{=ybyWP8jd}Add: {AddHitBehavior}"
                        .Translate(("AddHitBehavior", AddHitBehavior)));
                if (RemoveHitBehavior.IsEnabled)
                    modifiers.Add("{=WJVreCCg}Remove: {RemoveHitBehavior}"
                        .Translate(("RemoveHitBehavior", RemoveHitBehavior)));
                if (ArmorToIgnorePercent != 0)
                    modifiers.Add("{=svDicWAa}Ignore {ArmorToIgnorePercent}% Armor"
                        .Translate(("ArmorToIgnorePercent", ArmorToIgnorePercent)));
                if (UnblockableChancePercent != 0)
                    modifiers.Add("{=TkGVRcl0}{UnblockableChancePercent}% Unblockable"
                        .Translate(("UnblockableChancePercent", UnblockableChancePercent)));
                if (ShatterShieldChancePercent != 0)
                    modifiers.Add("{=xHMYpCMY}{ShatterShieldChancePercent}% Shatter Shield"
                        .Translate(("ShatterShieldChancePercent", ShatterShieldChancePercent)));
                if (CutThroughChancePercent != 0)
                    modifiers.Add("{=I3BiKkXC}{CutThroughChancePercent}% Cut Through"
                        .Translate(("CutThroughChancePercent", CutThroughChancePercent)));
                if (StaggerChancePercent != 0)
                    modifiers.Add("{=NetyrPKo}{StaggerChancePercent}% Stagger"
                        .Translate(("StaggerChancePercent", StaggerChancePercent)));
                
                if (AoE.IsEnabled) 
                    modifiers.Add("{=A6Vulmvy}AoE: {AoEDescription}"
                        .Translate(("AoEDescription", AoE.Description)));

                if (!modifiers.Any()) return "{=cM8NOj2B}(inactive)";
                return (appliesFromList.Any() && appliesToList.Any()
                    ? "{=UhEtVOWV}{Modifiers} from {AppliesFrom} against {AppliesAgainst}"
                    : appliesFromList.Any()
                        ? "{=GrP7yeqP}{Modifiers} from {AppliesFrom}"
                        : appliesToList.Any()
                            ? "{=dAObdN2r}{Modifiers} against {AppliesAgainst}"
                            : "{=ZXm0fYfO}{Modifiers}")
                    .Translate(
                        ("Modifiers", string.Join(" / ", modifiers)),
                        ("AppliesFrom", string.Join(" / ", appliesFromList)),
                        ("AppliesAgainst", string.Join(" / ", appliesToList)));
            }
        }
        #endregion

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator) => generator.P(Description.ToString());
        #endregion
    }

    public class AreaOfEffectDef : ICloneable, INotifyPropertyChanged
    {
        [LocDisplayName("{=DMsD70Yc}Range"),
         LocDescription("{=yjmcKGuH}The radius to apply the damage in"),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UIRange(0, 20, 0.5f), 
         PropertyOrder(1), UsedImplicitly]
        public float Range { get; set; }
	        
        [LocDisplayName("{=u1EgbhAw}Only On Hit"),
         LocDescription("{=uxyVrVjV}Only apply the damage if the attack hits an agent (as opposed to the ground, e.g. for arrows)"),
         PropertyOrder(2), UsedImplicitly]
        public bool OnlyOnHit { get; set; }

        [LocDisplayName("{=oLCQliJn}Damage At Center"),
         LocDescription("{=eLcjxP3j}Damage at distance 0 from the hit"), 
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UIRange(0, 500, 1), 
         PropertyOrder(3), UsedImplicitly]
        public float DamageAtCenter { get; set; } = 50;

        [LocDisplayName("{=GpuS1lb5}Max Agents To Damage"),
         LocDescription("{=ZFsDLtEj}Maximum number of agents that can be affected"), 
         Range(0, int.MaxValue),
         PropertyOrder(4), UsedImplicitly]
        public int MaxAgentsToDamage { get; set; } = 4;

        [LocDisplayName("{=zfp8aakV}Damage Type"),
         LocDescription("{=VcfFbhYX}Damage type"),
         PropertyOrder(5), UsedImplicitly]
        public DamageTypes DamageType { get; set; } = DamageTypes.Blunt;
        
        [LocDisplayName("{=MCTf7KFT}Hit Behavior"),
         LocDescription("{=Ud0EJmY0}Flags to apply to the damage"),
         ExpandableObject, PropertyOrder(6), UsedImplicitly]
        public HitBehavior HitBehavior { get; set; }

        [YamlIgnore, ReadOnly(true)]
        public bool IsEnabled => Range > 0;
	        
        [YamlIgnore, ReadOnly(true)]
        public string Example =>
            string.Join(", ",
                Enumerable.Range(0, (int) Math.Min(Range, 20))
                    .Select(i => "{=GRtsL26e}{Distance}m: {Damage}dmg"
                        .Translate(("Distance", i), ("Damage", CalculateDamage(i)))));

        public override string ToString() => Description;
        
        [YamlIgnore, Browsable(false)]
        public string Description
        {
            get
            {
                if (!IsEnabled) 
                    return "{=cM8NOj2B}(inactive)".Translate();
                else if (HitBehavior.IsEnabled)
                    return "{=S5DNQWpQ}{Damage}dmg in {Distance}m with {HitBehavior}"
                        .Translate(
                            ("Damage", DamageAtCenter),
                            ("Distance", Range),
                            ("HitBehavior", HitBehavior));
                else
                    return "{=MzOdRykr}{Damage}dmg in {Distance}m"
                        .Translate(
                            ("Damage", DamageAtCenter), 
                            ("Distance", Range));
            }
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
                AddDamagePower.DoAgentDamage(from, agent, damage, (agent.Position - position).NormalizedCopy(), 
                    DamageType, HitBehavior);
            }
        }

        private int CalculateDamage(float distance)
        {
            return (int) (DamageAtCenter / Math.Pow(distance / Range + 1f, 2f));
        }
            
        public event PropertyChangedEventHandler PropertyChanged;
    }
}