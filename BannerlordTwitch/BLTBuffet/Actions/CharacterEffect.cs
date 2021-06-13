using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero;
using HarmonyLib;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BLTBuffet
{
    [Description("Applies effects to a character / the player"), UsedImplicitly]
    public partial class CharacterEffect : ActionHandlerBase
    {
        protected override Type ConfigType => typeof(Config);

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

            if (BLTBuffetModule.EffectsConfig.DisableEffectsInTournaments
                && MissionHelpers.InTournament())
            {
                onFailure($"Not allowed during tournament!");
                return;
            }
            
            var effectsBehaviour = BLTEffectsBehaviour.Get();

            var config = (Config) baseConfig;

            bool GeneralAgentFilter(Agent agent) 
                => !config.TargetOnFootOnly || agent.HasMount == false && !effectsBehaviour.Contains(agent, config);

            var target = config.Target switch
            {
                Target.Player => Agent.Main,
                Target.AdoptedHero => Mission.Current.Agents.FirstOrDefault(a => a.IsAdoptedBy(context.UserName)),
                Target.Any => Mission.Current.Agents.Where(GeneralAgentFilter).Where(a => !a.IsAdopted()).SelectRandom(),
                Target.EnemyTeam => Mission.Current.Agents.Where(GeneralAgentFilter)
                    .Where(a => a.Team?.IsFriendOf(Mission.Current.PlayerTeam) == false && !a.IsAdopted())
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

            // if (config.Light != null)
            // {
            //     effectState.light = CreateLight(target, config.Light.Radius, config.Light.Intensity, config.Light.ColorParsed);
            // }

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