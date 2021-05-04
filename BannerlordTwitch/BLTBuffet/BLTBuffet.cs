using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using HarmonyLib;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

#pragma warning disable 649

namespace BLTBuffet
{
    // Avoid depending on AdoptAHero directly
    internal static class HeroExtensions
    {
        internal static bool IsAdopted(this Hero hero) => hero.Name.Contains("[BLT]");
    }
    
    internal static class AgentExtensions
    {
        internal static bool IsAdopted(this Agent agent) => (agent.Character as CharacterObject)?.HeroObject?.IsAdopted() ?? false;
    }
    
    public class BLTBuffetModule : MBSubModuleBase
    {
        public const string Name = "BLTBuffet";
        public const string Ver = "1.0.1";

        public BLTBuffetModule()
        {
            RewardManager.RegisterAll(typeof(BLTBuffetModule).Assembly);
        }
        
        private static Harmony harmony = null;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            if (harmony == null)
            {
                Harmony.DEBUG = true;
                try
                {
                    harmony = new Harmony("mod.bannerlord.bltbuffet");
                    harmony.PatchAll();
                }
                catch (Exception ex)
                {
                    Log.LogFeedCritical($"Error applying patches: {ex.Message}");
                }
            }
        }
    }

    [UsedImplicitly]
    public class TestPfx : ICommandHandler
    {
        private BoneAttachments active;
        public void Execute(ReplyContext context, object config)
        {
            if (Agent.Main == null)
            {
                return;
            }
            if (active != null)
            {
                CharacterEffect.RemoveAgentEffects(active);
                active = null;
            }
            if (!string.IsNullOrEmpty(context.Args))
            {
                active = CharacterEffect.CreateAgentEffects(Agent.Main, context.Args, 
                    MatrixFrame.Identity.Strafe(0.1f),
                    Game.Current.HumanMonster.HeadLookDirectionBoneIndex);
            }
        }

        public Type HandlerConfigType => null;
    }
    
    [UsedImplicitly]
    public class TestSfx : ICommandHandler
    {
        public void Execute(ReplyContext context, object config)
        {
            if (!string.IsNullOrEmpty(context.Args) && Agent.Main != null)
            {
                Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(context.Args), Agent.Main.AgentVisuals.GetGlobalFrame().origin, false, true, Agent.Main.Index, -1);
            }
        }

        public Type HandlerConfigType => null;
    }
    
    public class BoneAttachments
    {
        public readonly Agent agent;
        public readonly GameEntity holderEntity;

        public struct Attachment
        {
            public int boneIdx;
            public ParticleSystem particleSystem;
            public MatrixFrame localFrame;
        }

        private readonly Attachment[] attachments;

        public BoneAttachments(Agent agent, GameEntity holderEntity, IEnumerable<Attachment> attachments)
        {
            this.agent = agent;
            this.holderEntity = holderEntity;
            this.attachments = attachments.ToArray();
        }

        public void Update()
        {
            var agentVisuals = agent.AgentVisuals;
            holderEntity.SetGlobalFrame(agentVisuals.GetGlobalFrame());

            var skeleton = agentVisuals.GetSkeleton();
            if (skeleton != null)
            {
                foreach (var a in attachments)
                {
                    var frame = skeleton.GetBoneEntitialFrame(a.boneIdx) * a.localFrame;
                    a.particleSystem.SetLocalFrame(ref frame);
                }
            }
        }
    }
    
    public class BLTBoneAttachmentsUpdateBehaviour : MissionBehaviour
    {
        public override MissionBehaviourType BehaviourType => MissionBehaviourType.Other;

        private readonly List<BoneAttachments> attachmentsList = new(); 
        
        public void AddAttachments(BoneAttachments attachments)
        {
            attachmentsList.Add(attachments);
        }

        public void RemoveAttachments(BoneAttachments attachments)
        {
            attachments.holderEntity.ClearComponents();
            attachments.holderEntity.Scene?.RemoveEntity(attachments.holderEntity, 85);
            attachmentsList.Remove(attachments);
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            base.OnAgentDeleted(affectedAgent);
            var attachmentsToDelete = attachmentsList
                .Where(a => a.agent == affectedAgent)
                .ToList();
            foreach (var a in attachmentsToDelete)
            {
                RemoveAttachments(a);
            }
        }

        public override void OnPreDisplayMissionTick(float dt)
        {
            base.OnPreDisplayMissionTick(dt);
            foreach (var a in attachmentsList.ToList())
            {
                if (!a.agent.IsActive())
                {
                    RemoveAttachments(a);   
                }
                else
                {
                    a.Update();
                }
            }
        }

        public static BLTBoneAttachmentsUpdateBehaviour Get()
        {
            var beh = Mission.Current.GetMissionBehaviour<BLTBoneAttachmentsUpdateBehaviour>();
            if (beh == null)
            {
                beh = new BLTBoneAttachmentsUpdateBehaviour();
                Mission.Current.AddMissionBehaviour(beh);
            }
            return beh;
        }
    }

    [HarmonyPatch, UsedImplicitly]
    public static class Patches
    {
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(Mission), "GetAttackCollisionResults",
             new[]
             {
                 typeof(Agent), typeof(Agent), typeof(GameEntity), typeof(float), typeof(AttackCollisionData),
                 typeof(MissionWeapon), typeof(bool), typeof(bool), typeof(bool), typeof(WeaponComponentData),
                 typeof(CombatLogData)
             },
             new[]
             {
                 ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref,
                 ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out,
                 ArgumentType.Out
             })]
        public static void GetAttackCollisionResultsPostfix(Mission __instance, Agent attackerAgent, Agent victimAgent, ref AttackCollisionData attackCollisionData)
        {
            CharacterEffect.BLTEffectsBehaviour.Get().ApplyHitDamage(attackerAgent, victimAgent, ref attackCollisionData);
        }

        // [UsedImplicitly]
        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(Mission), "AddActiveMissionObject")]
        // public static void AddActiveMissionObjectPostfix()
        // {
        //     Log.Screen("Test");
        // }
    }

    [Description("Applies effects to a character / the player"), UsedImplicitly]
    public class CharacterEffect : ActionAndHandlerBase
    {
        internal class PropertyDef
        {
            [Description("The property to modify"), PropertyOrder(1)]
            public DrivenProperty Name { get; set; }
            [Description("Add to the property value"), PropertyOrder(2)]
            public float? Add { get; set; }
            [Description("Multiply the property value"), PropertyOrder(3)]
            public float? Multiply { get; set; }

            public override string ToString()
            {
                var parts = new List<string> { Name.ToString() };
                if (Multiply.HasValue && Multiply.Value != 0)
                {
                    parts.Add($"* {Multiply}");
                }
                if (Add.HasValue && Add.Value != 0)
                {
                    parts.Add(Add > 0 ? $"+ {Add}" : $"{Add}");
                }
                return string.Join(" ", parts);
            }
        }

        internal class ParticleEffectDef
        {
            [Description("Particle effect system name, see ParticleEffects.txt for the full vanilla list"),
             ItemsSource(typeof(ParticleEffectItemSource)), PropertyOrder(1)]
            public string Name { get; set; }

            public enum AttachPointEnum
            {
                OnWeapon,
                OnHands,
                OnHead,
                OnBody,
            }
            
            [Description("Where to attach the particles"), PropertyOrder(2)]
            public AttachPointEnum AttachPoint { get; set; }
            
            // [Description("Apply the effect to the weapon")]
            // public bool OnWeapon { get; set; }
            // [Description("Apply the effect to the hands")]
            // public bool OnHands { get; set; }
            // [Description("Apply the effect to the head")]
            // public bool OnHead { get; set; }
            // [Description("Apply the effect to the whole body")]
            // public bool OnBody { get; set; }
            public override string ToString()
            {
                return $"{Name} {AttachPoint}";
            }
        }

        internal class LightDef
        {
            public float Radius { get; set; }
            public float Intensity { get; set; }
            public string Color { get; set; }

            public Vec3 ColorParsed
            {
                get
                {
                    if (string.IsNullOrEmpty(Color))
                        return new Vec3();
                    string[] parts = Color.Split(' ');
                    var color = new Vec3();
                    for (int index = 0; index < parts.Length && index < 3; index++)
                    {
                        if (!float.TryParse(parts[index], out float val))
                        {
                            return new Vec3();
                        }
                        color[index] = val;
                    }

                    return color;
                }
            }
        }

        internal enum Target
        {
            Player,
            AdoptedHero,
            Any,
            EnemyTeam,
            PlayerTeam,
            AllyTeam,
            Random
        }
        
        internal class Config
        {
            [Description("Name to use when referring to this effect"), PropertyOrder(1)]
            public string Name { get; set; }
            [Description("Target of the effect"), PropertyOrder(2)]
            public Target Target { get; set; }
            [Description("Will target unmounted soldiers only"), PropertyOrder(3)]
            public bool TargetOnFootOnly { get; set; }
            [Description("Scaling of the target"), PropertyOrder(4)]
            public float? Scale { get; set; }
            [Description("Particle effects to apply"), PropertyOrder(5)]
            public List<ParticleEffectDef> ParticleEffects { get; set; }
            [Description("Properties to change, and how much by"), PropertyOrder(6)]
            public List<PropertyDef> Properties { get; set; }
            [Description("Creates a light attached to the target"), PropertyOrder(7)]
            public LightDef Light { get; set; }
            [Description("Heal amount per second"), PropertyOrder(8)]
            public float HealPerSecond { get; set; }
            [Description("Damage amount per second"), PropertyOrder(9)]
            public float DamagePerSecond { get; set; }
            [Description("Duration the effect will last for, if not specified the effect will last until the end of the mission"), PropertyOrder(10), DefaultValue(null)]
            public float? Duration { get; set; }
            [Description("Force agent to drop weapons"), PropertyOrder(11)]
            public bool ForceDropWeapons { get; set; }
            // [Description("Force agent dismount"), PropertyOrder(12)]
            // public bool ForceDismount { get; set; }
            [Description("Remove all armor"), PropertyOrder(13)]
            public bool RemoveArmor { get; set; }
            [Description("Raw damage multiplier"), PropertyOrder(14)]
            public float? DamageMultiplier { get; set; }

            [Description("One shot vfx to apply when the effect is activated"),
             ItemsSource(typeof(ParticleEffectItemSource)), PropertyOrder(15)]
            public string ActivateParticleEffect { get; set; }

            [Description("Sound to play when effect is activated, see Sounds.txt for the full vanilla list"),
             ItemsSource(typeof(SoundEffectItemSource)), PropertyOrder(16)]
            public string ActivateSound { get; set; }

            [Description("One shot vfx to apply when the effect is deactivated, see ParticleEffects.txt for the full vanilla list"), ItemsSource(typeof(ParticleEffectItemSource)), PropertyOrder(17)]
            public string DeactivateParticleEffect { get; set; }

            [Description("Sound to play when effect is deactivated, see Sounds.txt for the full vanilla list"),
             ItemsSource(typeof(SoundEffectItemSource)), PropertyOrder(18)]
            public string DeactivateSound { get; set; }
        }

        protected override Type ConfigType => typeof(Config);

        internal class CharacterEffectState
        {
            public readonly Agent agent;
            public readonly Config config;

            public float started = MBCommon.GetTime(MBCommon.TimeType.Mission);
            public class PfxState
            {
                public List<GameEntityComponent> weaponEffects;
                public BoneAttachments boneAttachments;
            }
            public readonly List<PfxState> state = new();
            public Light light;

            public CharacterEffectState(Agent agent, Config config)
            {
                this.agent = agent;
                this.config = config;
            }

            public void Apply(float dt)
            {
                if (config.HealPerSecond != 0)
                {
                    agent.Health = Math.Min(agent.HealthLimit, agent.Health + Math.Abs(config.HealPerSecond) * dt);
                }

                if(config.DamagePerSecond != 0 && !agent.Invulnerable && !Mission.DisableDying)
                {
                    var blow = new Blow(agent.Index);
                    blow.DamageType = DamageTypes.Blunt;
                    //blow.BlowFlag = BlowFlags.CrushThrough;
                    //blow.BlowFlag |= BlowFlags.KnockDown;
                    blow.BoneIndex = agent.Monster.HeadLookDirectionBoneIndex;
                    blow.Position = agent.Position;
                    blow.Position.z += agent.GetEyeGlobalHeight();
                    blow.BaseMagnitude = 0f;
                    blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
                    blow.InflictedDamage = (int) Math.Abs(config.DamagePerSecond * dt);
                    blow.SwingDirection = agent.LookDirection;
                    blow.SwingDirection.Normalize();
                    blow.Direction = blow.SwingDirection;
                    blow.DamageCalculated = true;
                    agent.RegisterBlow(blow);
                }

                if (config.ForceDropWeapons)
                {
                    var index = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                    if (index != EquipmentIndex.None)
                    {
                        agent.DropItem(index);
                    }
                    var index2 = agent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
                    if (index2 != EquipmentIndex.None)
                    {
                        agent.DropItem(index2);
                    }
                }

                // Doesn't work, doesn't something weird instead
                // if (config.ForceDismount)
                // {
                //     AccessTools.Method(typeof(Agent), "SetMountAgent").Invoke(agent, new object[] { null });
                // }

                if (config.Properties != null)
                {
                    ApplyPropertyModifiers(agent, config);
                }
            }

            public bool CheckRemove()
            {
                if (config.Duration.HasValue 
                    && MBCommon.GetTime(MBCommon.TimeType.Mission) > config.Duration.Value + started)
                {
                    Log.LogFeedEvent($"{config.Name} expired on {agent.Name}!");
                    if (!string.IsNullOrEmpty(config.DeactivateParticleEffect))
                    {
                        Mission.Current.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName(config.DeactivateParticleEffect), agent.AgentVisuals.GetGlobalFrame());
                    }
                    if (!string.IsNullOrEmpty(config.DeactivateSound))
                    {
                        Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(config.DeactivateSound), agent.AgentVisuals.GetGlobalFrame().origin, false, true, agent.Index, -1);
                    }
                    Stop();
                    return true;
                }
                return false;
            }

            public void Stop()
            {
                foreach (var s in state)
                {
                    if(s.weaponEffects != null) RemoveWeaponEffects(s.weaponEffects);
                    if(s.boneAttachments != null) RemoveAgentEffects(s.boneAttachments);
                }

                if (light != null)
                {
                    RemoveLight(agent, light);
                }
                
                agent.UpdateAgentProperties();
            }
        }

        internal class BLTEffectsBehaviour : MissionBehaviour
        {
            private readonly Dictionary<Agent, List<CharacterEffectState>> agentEffectsActive = new ();

            private float accumulatedTime;
            
            public bool Contains(Agent agent, Config config)
            {
                return agentEffectsActive.TryGetValue(agent, out var effects) 
                       && effects.Any(e => e.config.Name == config.Name);
            }

            public CharacterEffectState Add(Agent agent, Config config)
            {
                if (!agentEffectsActive.TryGetValue(agent, out var effects))
                {
                    effects = new List<CharacterEffectState>();
                    agentEffectsActive.Add(agent, effects);
                }

                var state = new CharacterEffectState(agent, config);
                effects.Add(state);
                return state;
            }

            public static BLTEffectsBehaviour Get()
            {
                var beh = Mission.Current.GetMissionBehaviour<BLTEffectsBehaviour>();
                if (beh == null)
                {
                    beh = new BLTEffectsBehaviour();
                    Mission.Current.AddMissionBehaviour(beh);
                }
                return beh;
            }

            public void ApplyHitDamage(Agent attackerAgent, Agent victimAgent, ref AttackCollisionData attackCollisionData)
            {
                float[] hitDamageMultipliers = agentEffectsActive
                    .Where(e => ReferenceEquals(e.Key, attackerAgent))
                    .SelectMany(e => e.Value
                        .Select(f => f.config.DamageMultiplier ?? 0)
                        .Where(f => f != 0)
                    )
                    .ToArray();
                if (hitDamageMultipliers.Any())
                {
                    float forceMag = hitDamageMultipliers.Sum();
                    attackCollisionData.BaseMagnitude = (int) (attackCollisionData.BaseMagnitude * forceMag);
                    attackCollisionData.InflictedDamage = (int) (attackCollisionData.InflictedDamage * forceMag);
                }
            }
            
            // public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, int damage, in MissionWeapon affectorWeapon)
            // {
            //     float[] knockBackForces = agentEffectsActive
            //         .Where(e => ReferenceEquals(e.Key, affectorAgent))
            //         .SelectMany(e => e.Value
            //             .Select(f => f.config.DamageMultiplier ?? 0)
            //             .Where(f => f != 0)
            //         )
            //         .ToArray();
            //     if (knockBackForces.Any())
            //     {
            //         var direction = (affectedAgent.Frame.origin - affectorAgent.Frame.origin).NormalizedCopy();
            //         var force = knockBackForces.Select(f => direction * f).Aggregate((a, b) => a + b);
            //         affectedAgent.AgentVisuals.SetAgentLocalSpeed(force.AsVec2);
            //         //var entity = affectedAgent.AgentVisuals.GetEntity();
            //         // // entity.ActivateRagdoll();
            //         // entity.AddPhysics(0.1f, Vec3.Zero, null, force * 2000, Vec3.Zero, PhysicsMaterial.GetFromIndex(0), false, 0);
            //
            //         // entity.EnableDynamicBody();
            //         // entity.SetPhysicsState(true, true);
            //
            //         //entity.ApplyImpulseToDynamicBody(entity.GetGlobalFrame().origin, force);
            //         // foreach (float knockBackForce in knockBackForces)
            //         // {
            //         //     entity.ApplyImpulseToDynamicBody(entity.GetGlobalFrame().origin, direction * knockBackForce);
            //         // }
            //         // Mission.Current.AddTimerToDynamicEntity(entity, 3f + MBRandom.RandomFloat * 2f);
            //     }
            // }

            public override void OnAgentDeleted(Agent affectedAgent)
            {
                if (agentEffectsActive.TryGetValue(affectedAgent, out var effectStates))
                {
                    foreach (var e in effectStates)
                    {
                        e.Stop();
                    }
                    agentEffectsActive.Remove(affectedAgent);
                }
            }

            private readonly Dictionary<Agent, float[]> agentDrivenPropertiesCache = new();
            private readonly Dictionary<Agent, float> agentBaseScaleCache = new();

            public override void OnMissionTick(float dt)
            {
                base.OnMissionTick(dt);

                foreach (var agent in agentEffectsActive
                    .Where(kv => !kv.Key.IsActive())
                    .ToArray())
                {
                    foreach (var effect in agent.Value.ToList())
                    {
                        effect.Stop();
                    }
                    agentEffectsActive.Remove(agent.Key);
                }
                
                const float Interval = 2;
                accumulatedTime += dt;
                if (accumulatedTime < Interval)
                    return;
                
                accumulatedTime -= Interval;
                foreach (var agentEffects in agentEffectsActive.ToArray())
                {
                    var agent = agentEffects.Key;
                    // Restore all the properties from the cache to start with
                    if (!agentDrivenPropertiesCache.TryGetValue(agent, out float[] initialAgentDrivenProperties))
                    {
                        initialAgentDrivenProperties = new float[(int) DrivenProperty.Count];
                        for (int i = 0; i < (int) DrivenProperty.Count; i++)
                        {
                            initialAgentDrivenProperties[i] = agent.AgentDrivenProperties.GetStat((DrivenProperty) i);
                        }
                        agentDrivenPropertiesCache.Add(agent, initialAgentDrivenProperties);
                    }
                    else
                    {
                        for (int i = 0; i < (int) DrivenProperty.Count; i++)
                        {
                            agent.AgentDrivenProperties.SetStat((DrivenProperty) i, initialAgentDrivenProperties[i]);
                        }
                    }

                    if (!agentBaseScaleCache.TryGetValue(agent, out float baseAgentScale))
                    {
                        agentBaseScaleCache.Add(agent, agent.AgentScale);
                        baseAgentScale = agent.AgentScale;
                    }

                    float newAgentScale = baseAgentScale;
                    // Now update the dynamic properties
                    agent.UpdateAgentProperties();
                    // Then apply our effects as a stack
                    foreach (var effect in agentEffects.Value.ToList())
                    {
                        effect.Apply(Interval);
                        if(effect.CheckRemove())
                        {
                            agentEffects.Value.Remove(effect);
                        }

                        newAgentScale *= effect.config.Scale ?? 1;
                    }

                    if (newAgentScale != agent.AgentScale)
                    {
                        SetAgentScale(agent, baseAgentScale, newAgentScale);
                    }
                    // Finally commit our modified values to the engine
                    agent.UpdateCustomDrivenProperties();
                }
            }

            public override MissionBehaviourType BehaviourType => MissionBehaviourType.Other;
        }

        protected override void ExecuteInternal(ReplyContext context, object baseConfig,
            Action<string> onSuccess, Action<string> onFailure)
        {
            // DOING:
            // - search agents for the target to apply to
            // - keep list of agents already with active effects so we can avoid adding more than one effect per agent
            // - clean agent lists and reset stats on missing state change / after some timeout

            if (Mission.Current == null)
            {
                onFailure($"No mission is active!");
                return;
            }
            
            var effectsBehaviour = BLTEffectsBehaviour.Get();

            var config = (Config) baseConfig;

            bool GeneralAgentFilter(Agent agent) 
                => !config.TargetOnFootOnly || agent.HasMount == false && !effectsBehaviour.Contains(agent, config);

            var target = config.Target switch
            {
                Target.Player => Agent.Main,
                Target.AdoptedHero => Mission.Current.Agents.FirstOrDefault(a =>
                {
                    if (a.Character is not CharacterObject charObj) return false;
                    return charObj.HeroObject != null && charObj.HeroObject.FirstName.Contains(context.UserName) &&
                           charObj.HeroObject.FirstName.ToString() == context.UserName;
                }),
                Target.Any => Mission.Current.Agents.Where(GeneralAgentFilter).Where(a => !a.IsAdopted()).SelectRandom(),
                Target.EnemyTeam => Mission.Current.Agents.Where(GeneralAgentFilter)
                    .Where(a => a.Team?.IsPlayerTeam == false && !a.Team.IsPlayerAlly && !a.IsAdopted())
                    .SelectRandom(),
                Target.PlayerTeam => Mission.Current.Agents.Where(GeneralAgentFilter)
                    .Where(a => a.Team?.IsPlayerTeam == true && !a.IsAdopted())
                    .SelectRandom(),
                Target.AllyTeam => Mission.Current.Agents.Where(GeneralAgentFilter)
                    .Where(a => a.Team?.IsPlayerAlly == false && !a.IsAdopted())
                    .SelectRandom(),
                _ => null
            };

            if (target == null || target.AgentVisuals == null)
            {
                onFailure($"Couldn't find the target!");
                return;
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                onFailure($"CharacterEffect {context.Source} configuration error: Name is missing!");
                return;
            }

            if (effectsBehaviour.Contains(target, config))
            {
                onFailure($"{target.Name} already affected by {config.Name}!");
                return;
            }
            
            if (config.TargetOnFootOnly && target.HasMount)
            {
                onFailure($"{target.Name} is mounted so cannot be affected by {config.Name}!");
                return;
            }
            
            var effectState = effectsBehaviour.Add(target, config);

            foreach (var pfx in config.ParticleEffects ?? Enumerable.Empty<ParticleEffectDef>())
            {
                var pfxState = new CharacterEffectState.PfxState();
                switch (pfx.AttachPoint)
                {
                    case ParticleEffectDef.AttachPointEnum.OnWeapon:
                        pfxState.weaponEffects = CreateWeaponEffects(target, pfx.Name);
                        break;
                    case ParticleEffectDef.AttachPointEnum.OnHands:
                        pfxState.boneAttachments = CreateAgentEffects(target,
                            pfx.Name,
                            MatrixFrame.Identity,
                            Game.Current.HumanMonster.MainHandItemBoneIndex,
                            Game.Current.HumanMonster.OffHandItemBoneIndex);
                        break;
                    case ParticleEffectDef.AttachPointEnum.OnHead:
                        pfxState.boneAttachments = CreateAgentEffects(target,
                            pfx.Name,
                            MatrixFrame.Identity.Strafe(0.1f),
                            Game.Current.HumanMonster.HeadLookDirectionBoneIndex);
                        break;
                    case ParticleEffectDef.AttachPointEnum.OnBody:
                        pfxState.boneAttachments = CreateAgentEffects(target, pfx.Name, MatrixFrame.Identity.Elevate(0.1f));
                        break;
                    default:
                        Log.Error($"{config.Name}: No location specified for particle Id {pfx.Name} in CharacterEffect");
                        break;
                }
                effectState.state.Add(pfxState);
            }

            // if (config.Properties != null && target.AgentDrivenProperties != null)
            // {
            //     ApplyPropertyModifiers(target, config);
            // }

            if (config.Light != null)
            {
                effectState.light = CreateLight(target, config.Light.Radius, config.Light.Intensity, config.Light.ColorParsed);
            }

            if (config.RemoveArmor)
            {
                foreach (var (_, index) in target.SpawnEquipment.YieldArmorSlots())
                {
                    target.SpawnEquipment[index] = EquipmentElement.Invalid;
                }
                target.UpdateSpawnEquipmentAndRefreshVisuals(target.SpawnEquipment);
            }
            
            if (!string.IsNullOrEmpty(config.ActivateParticleEffect))
            {
                Mission.Current.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName(config.ActivateParticleEffect), target.AgentVisuals.GetGlobalFrame());
            }
            if (!string.IsNullOrEmpty(config.ActivateSound))
            {
                Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(config.ActivateSound), target.AgentVisuals.GetGlobalFrame().origin, false, true, target.Index, -1);
            }

            Log.LogFeedEvent($"{config.Name} is active on {target.Name}!");

            onSuccess($"{config.Name} is active on {target.Name}!");
        }

        private static void ApplyPropertyModifiers(Agent target, Config config)
        {
            foreach (var prop in config.Properties)
            {
                float baseValue = target.AgentDrivenProperties.GetStat(prop.Name);
                if (prop.Multiply.HasValue)
                    baseValue *= prop.Multiply.Value;
                if (prop.Add.HasValue)
                    baseValue += prop.Add.Value;
                target.AgentDrivenProperties.SetStat(prop.Name, baseValue);
            }
            // target.UpdateCustomDrivenProperties();
        }

        public static void SetAgentScale(Agent agent, float baseScale, float scale)
        {
            AccessTools.Method(typeof(Agent), "SetInitialAgentScale").Invoke(agent, new []{ (object) scale });
            // Doesn't have any affect...
            //AgentVisualsNativeData agentVisualsNativeData = agent.Monster.FillAgentVisualsNativeData();
            //AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(agent.Character.GetStepSize() * scale / baseScale , false);
            // animationSystemData.WalkingSpeedLimit *= scale;
            // animationSystemData.CrouchWalkingSpeedLimit *= scale;
            //animationSystemData.NumPaces = 10;
            //agent.SetActionSet(ref agentVisualsNativeData, ref animationSystemData);
        }
        
        public static List<GameEntityComponent> CreateWeaponEffects(Agent agent, string pfxSystem)
        {
            var index = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            if (index == EquipmentIndex.None)
                return default;

            var wieldedWeaponEntity = agent.GetWeaponEntityFromEquipmentSlot(index);
            var wieldedWeapon = agent.WieldedWeapon;
            if (wieldedWeapon.WeaponsCount == 0)
                return default;

            var agentVisuals = agent.AgentVisuals;
            if (agentVisuals == null)
                return default;

            var skeleton = agentVisuals.GetSkeleton();

            int length = wieldedWeapon.GetWeaponStatsData()[0].WeaponLength;
            int numInstances = (int)Math.Round(length / 10f);

            var components = new List<GameEntityComponent>();
            switch (wieldedWeapon.CurrentUsageItem.WeaponClass)
            {
                case WeaponClass.OneHandedSword:
                case WeaponClass.TwoHandedSword:
                case WeaponClass.Mace:
                case WeaponClass.TwoHandedMace:
                    for (int i = 1; i < numInstances; i++)
                    {
                        var localFrame = MatrixFrame.Identity.Elevate(i * 0.1f);
                        var particle = ParticleSystem.CreateParticleSystemAttachedToEntity(pfxSystem, wieldedWeaponEntity, ref localFrame);
                        particle.SetRuntimeEmissionRateMultiplier(MBRandom.RandomFloatRanged(0.75f, 1.25f));
                        skeleton.AddComponentToBone(Game.Current.HumanMonster.MainHandItemBoneIndex, particle);
                        components.Add(particle);
                    }
                    break;
                case WeaponClass.OneHandedAxe:
                case WeaponClass.TwoHandedAxe:
                case WeaponClass.OneHandedPolearm:
                case WeaponClass.TwoHandedPolearm:
                case WeaponClass.LowGripPolearm:
                    // Apply the effect to the blade only hopefully
                    int effectLength = (numInstances > 19) ? 9 : (numInstances > 15) ? 6 : (numInstances > 12) ? 5 : (numInstances > 10) ? 4 : 3;
                    for (int i = numInstances - 1; i > 0 && i > numInstances - effectLength; i--)
                    {
                        var localFrame = MatrixFrame.Identity.Elevate(i * 0.1f);
                        var particle = ParticleSystem.CreateParticleSystemAttachedToEntity(pfxSystem, wieldedWeaponEntity, ref localFrame);
                        particle.SetRuntimeEmissionRateMultiplier(MBRandom.RandomFloatRanged(0.75f, 1.25f));
                        skeleton.AddComponentToBone(Game.Current.HumanMonster.MainHandItemBoneIndex, particle);
                        components.Add(particle);
                    }
                    break;
                default:
                    return default;
            }
            // Only by throwing it away and picking it up again can the particle effect appear
            //var dropLock = true;
            agent.DropItem(index);
            var spawnedItemEntity = wieldedWeaponEntity.GetFirstScriptOfType<SpawnedItemEntity>();
            if(spawnedItemEntity != null)
                agent.OnItemPickup(spawnedItemEntity, EquipmentIndex.None, out bool _);
            //dropLock = false;

            return components;
        }

        public static void RemoveWeaponEffects(List<GameEntityComponent> effects)
        {
            foreach (var effect in effects)
            {
                effect.GetEntity().Skeleton.RemoveComponent(effect);
            }
        }
        
        public static Light CreateLight(Agent agent, float lightRadius, float lightIntensity, Vec3 lightColor)
        {
            var agentVisuals = agent.AgentVisuals;
            if (agentVisuals == null)
                return default;
            var skeleton = agentVisuals.GetSkeleton();
            var light = Light.CreatePointLight(lightRadius);
            light.Intensity = lightIntensity;
            light.LightColor = lightColor;
            light.Frame = MatrixFrame.Identity.Advance(1);
            skeleton.AddComponentToBone(Game.Current.HumanMonster.MainHandItemBoneIndex, light);
            return light;
        }

        public static void RemoveLight(Agent agent, Light light)
        {
            var agentVisuals = agent.AgentVisuals;
            if(agentVisuals != null)
            {
                var skeleton = agentVisuals.GetSkeleton();
                if (light != null && skeleton != null)
                    skeleton.RemoveComponent(light);
            }
        }

        public static BoneAttachments CreateAgentEffects(Agent agent, string pfxSystem, MatrixFrame offset, params sbyte[] boneIndices)
        {
            var agentVisuals = agent.AgentVisuals;
            if (agentVisuals == null)
            {
                return null;
            }   
            var skeleton = agentVisuals.GetSkeleton();
            //var localFrame = MatrixFrame.Identity;
            //var localFrame = MatrixFrame.Identity;//new MatrixFrame(Mat3.Identity, new Vec3(0f, 0f, 0f, -1f)).Elevate(0.5f);
            
            //var prevents = new List<GameEntity>();
            //agentVisuals.GetEntity().Scene.GetEntities(ref prevents);

            // var ents = new List<GameEntity>();
            // agentVisuals.GetEntity().Scene.GetEntities(ref ents);
            // var allEnts = new List<GameEntity>();
            // foreach (var e in ents)
            // {
            //     allEnts.Add(e);
            //     e.GetChildrenRecursive(ref allEnts);
            // }
            
            // var prevPfx = allEnts
            //     .SelectMany(e =>
            //     {
            //         var pfx = new List<GameEntityComponent>();
            //         for (int i = 0; i < e.GetComponentCount(GameEntity.ComponentType.ParticleSystemInstanced); i++)
            //         {
            //             pfx.Add(e.GetComponentAtIndex(i, GameEntity.ComponentType.ParticleSystemInstanced));
            //         }
            //
            //         return pfx.Select(p => (e, p));
            //     })
            //     .ToList();

            var attachments = new List<BoneAttachments.Attachment>();
            //owner.AddParticleSystemComponent("psys_game_burning_agent");
            var owner = GameEntity.CreateEmpty(agentVisuals.GetEntity().Scene); // agentVisuals.GetEntity();
            owner.SetGlobalFrame(agentVisuals.GetGlobalFrame());

            bool CreateAttachment(sbyte boneIdx)
            {
                var frame = skeleton.GetBoneEntitialFrame(boneIdx);
                var particle = ParticleSystem.CreateParticleSystemAttachedToEntity(pfxSystem, owner, ref frame);
                if (particle == null)
                {
                    return true;
                }

                particle.SetRuntimeEmissionRateMultiplier(MBRandom.RandomFloatRanged(0.75f, 1.25f));
                attachments.Add(new BoneAttachments.Attachment
                {
                    boneIdx = boneIdx,
                    localFrame = offset,
                    particleSystem = particle
                });
                return false;
            }

            if (boneIndices == null || boneIndices.Length == 0)
            {
                int count = skeleton.GetBoneCount();
                for (sbyte index = 0; index < count; index++)
                {
                    if (CreateAttachment(index)) return null;
                }
            }
            else
            {
                foreach (sbyte index in boneIndices)
                {
                    if (CreateAttachment(index)) return null;
                }
            }

            // var newPfx = allEnts
            //     .SelectMany(e =>
            //     {
            //         var pfx = new List<GameEntityComponent>();
            //         for (int i = 0; i < e.GetComponentCount(GameEntity.ComponentType.ParticleSystemInstanced); i++)
            //         {
            //             pfx.Add(e.GetComponentAtIndex(i, GameEntity.ComponentType.ParticleSystemInstanced));
            //         }
            //
            //         return pfx.Select(p => (e, p));
            //     })
            //     .ToList();
            //var ents = new List<GameEntity>();
            //agentVisuals.GetEntity().Scene.GetEntities(ref ents);
            //var newEnts = ents.Except(prevents);
            //owner.RecomputeBoundingBox();
            //owner.EntityFlags = (EntityFlags)0;
            //owner.SetBodyFlagsRecursive(Bod);
            //agentVisuals.GetEntity().EnableDynamicBody();
            //agentVisuals.GetEntity().AddChild(owner, autoLocalizeFrame: false);
            var boneAttachments = new BoneAttachments(agent, owner, attachments);
            BLTBoneAttachmentsUpdateBehaviour.Get().AddAttachments(boneAttachments);
            return boneAttachments;
        }

        public static void RemoveAgentEffects(BoneAttachments attachments)
        {
            // attachments?.holderEntity?.Scene?.RemoveEntity(attachments.holderEntity, 85);
            BLTBoneAttachmentsUpdateBehaviour.Get().RemoveAttachments(attachments);
            // var agentVisuals = agent.AgentVisuals;
            // if (agentVisuals == null)
            // {
            //     return;
            // }   
            // var skeleton = agentVisuals.GetSkeleton();
            // int count = skeleton.GetBoneCount();
            // if (count != pfxs.Count)
            // {
            //     return;
            // }
            // for (byte i = 0; i < count; i++)
            // {
            //     skeleton.RemoveBoneComponent(i, skeleton.GetBoneComponentAtIndex(i, 0));
            // }
        }
    }
}