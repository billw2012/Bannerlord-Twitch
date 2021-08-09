using System;
using System.Collections.Generic;
using System.ComponentModel;
using BannerlordTwitch.Annotations;
using BannerlordTwitch.Util;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch.Helpers
{
    public class ParticleEffectDef : ICloneable, INotifyPropertyChanged
    {
        // private int Id { get; set; }
        
        [Description("Particle effect system name, see ParticleEffects.txt for the full vanilla list"),
         ItemsSource(typeof(ParticleEffectItemSource)), PropertyOrder(1), UsedImplicitly]
        public string Name { get; set; }

        public enum AttachPointEnum
        {
            OnWeapon,
            OnHands,
            OnHead,
            OnBody,
        }

        [Description("Where to attach the particles"), PropertyOrder(2), UsedImplicitly]
        public AttachPointEnum AttachPoint { get; set; }

        public override string ToString() => $"{Name} {AttachPoint}";
        public object Clone()
        {
            var copy = CloneHelpers.CloneFields(this);
            return copy;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
    
    public class AgentPfx
    {
        public Agent Agent { get; }

        private class PfxState
        {
            public List<GameEntityComponent> weaponEffects;
            public BoneAttachments boneAttachments;
        }

        private readonly List<ParticleEffectDef> particleEffects;
        private List<PfxState> pfxStates;

        public AgentPfx(Agent agent, List<ParticleEffectDef> particleEffects)
        {
            Agent = agent;
            this.particleEffects = particleEffects;
        }

        public void Start()
        {
            if (pfxStates != null) return;
            pfxStates = new();
            foreach (var pfx in particleEffects)
            {
                var pfxState = new PfxState();
                switch (pfx.AttachPoint)
                {
                    case ParticleEffectDef.AttachPointEnum.OnWeapon:
                        pfxState.weaponEffects = CreateWeaponEffects(Agent, pfx.Name);
                        break;
                    case ParticleEffectDef.AttachPointEnum.OnHands:
                        pfxState.boneAttachments = CreateAgentEffects(Agent,
                            pfx.Name,
                            MatrixFrame.Identity,
                            Game.Current.HumanMonster.MainHandItemBoneIndex,
                            Game.Current.HumanMonster.OffHandItemBoneIndex);
                        break;
                    case ParticleEffectDef.AttachPointEnum.OnHead:
                        pfxState.boneAttachments = CreateAgentEffects(Agent,
                            pfx.Name,
                            MatrixFrame.Identity.Strafe(0.1f),
                            Game.Current.HumanMonster.HeadLookDirectionBoneIndex);
                        break;
                    case ParticleEffectDef.AttachPointEnum.OnBody:
                        pfxState.boneAttachments = CreateAgentEffects(Agent, pfx.Name, MatrixFrame.Identity.Elevate(0.1f));
                        break;
                    default:
                        Log.Error($"No location specified for particle Id {pfx.Name} in AgentEffect");
                        break;
                }
                pfxStates.Add(pfxState);
            }
        }

        public void Stop()
        {
            if (pfxStates == null) return;
            foreach (var s in pfxStates)
            {
                if(s.weaponEffects != null) RemoveWeaponEffects(s.weaponEffects);
                if(s.boneAttachments != null) RemoveAgentEffects(s.boneAttachments);
            }

            pfxStates = null;
        }

        #region Weapon Effects
        private static List<GameEntityComponent> CreateWeaponEffects(Agent agent, string pfxSystem)
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
        
        private static void RemoveWeaponEffects(List<GameEntityComponent> effects)
        {
            foreach (var effect in effects)
            {
                effect.GetEntity()?.Skeleton?.RemoveComponent(effect);
            }
        }
        #endregion
        
        #region Agent Effects
        private static BoneAttachments CreateAgentEffects(Agent agent, string pfxSystem, MatrixFrame offset, params sbyte[] boneIndices)
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
            BLTAgentPfxBehaviour.Current.AddAttachments(boneAttachments);
            return boneAttachments;
        }
        
        private static void RemoveAgentEffects(BoneAttachments attachments)
        {
            // attachments?.holderEntity?.Scene?.RemoveEntity(attachments.holderEntity, 85);
            BLTAgentPfxBehaviour.Current.RemoveAttachments(attachments);
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
        #endregion
    }
}