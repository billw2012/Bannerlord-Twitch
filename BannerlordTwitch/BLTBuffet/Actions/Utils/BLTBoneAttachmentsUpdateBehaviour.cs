using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BLTBuffet
{
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

    public class BLTBoneAttachmentsUpdateBehaviour : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

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
            var beh = Mission.Current.GetMissionBehavior<BLTBoneAttachmentsUpdateBehaviour>();
            if (beh == null)
            {
                beh = new BLTBoneAttachmentsUpdateBehaviour();
                Mission.Current.AddMissionBehavior(beh);
            }

            return beh;
        }
    }
}