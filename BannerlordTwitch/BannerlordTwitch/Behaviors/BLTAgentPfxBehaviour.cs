using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Util;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BannerlordTwitch
{
    internal class BoneAttachments
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

    public class BLTAgentPfxBehaviour : AutoMissionBehavior<BLTAgentPfxBehaviour>
    {
        private readonly List<BoneAttachments> attachmentsList = new();
        
        internal void AddAttachments(BoneAttachments attachments)
        {
            attachmentsList.Add(attachments);
        }

        internal void RemoveAttachments(BoneAttachments attachments)
        {
            attachments.holderEntity.ClearComponents();
            attachments.holderEntity.Scene?.RemoveEntity(attachments.holderEntity, 85);
            attachmentsList.Remove(attachments);
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            SafeCall(() =>
            {
                var attachmentsToDelete = attachmentsList
                    .Where(a => a.agent == affectedAgent)
                    .ToList();
                foreach (var a in attachmentsToDelete)
                {
                    RemoveAttachments(a);
                }
            });
        }

        public override void OnPreDisplayMissionTick(float dt)
        {
            SafeCall(() =>
            {
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
            });
        }
    }
}