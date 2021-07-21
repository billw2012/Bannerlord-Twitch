using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerlordTwitch.Helpers
{
    public static class AgentHelpers
    {
        public static void RemoveArmor(Agent agent)
        {
            foreach (var (_, index) in agent.SpawnEquipment.YieldArmorSlots())
            {
                agent.SpawnEquipment[index] = EquipmentElement.Invalid;
            }
            agent.UpdateSpawnEquipmentAndRefreshVisuals(agent.SpawnEquipment);
        }

        public static void DropWeapons(Agent agent)
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
        
        public static void SetAgentScale(Agent agent, float baseScale, float scale)
        {
            // Only remaining problem here is to get collision to work properly for scaled agents. 
            
            //var n = ((this._data.ScaleData == 0f) ? MBBodyProperties.GetScaleFromKey(this.IsFemale ? 1 : 0, this._data.BodyPropertiesData) : this._data.ScaleData);
            // var frameData = agent.AgentVisuals.GetFrame();
            // frameData.rotation.ApplyScaleLocal(scale);
            // agent.AgentVisuals.SetFrame(ref frameData);
            //var entity = agent.AgentVisuals.GetEntity();

            AccessTools.Method(typeof(Agent), "SetInitialAgentScale").Invoke(agent, new []{ (object) scale });
            // Not sure which of these (if either of them) is actually helpful here
            agent.AgentVisuals.LazyUpdateAgentRendererData();
            agent.AgentVisuals.UpdateSkeletonScale((int)agent.SpawnEquipment.BodyDeformType);

            // if (agent.IsMount)
            // {
            //     //agent.AgentVisuals.ApplySkeletonScale(mountItem.HorseComponent.SkeletonScale.MountSitBoneScale, mountItem.HorseComponent.SkeletonScale.MountRadiusAdder, mountItem.HorseComponent.SkeletonScale.BoneIndices, mountItem.HorseComponent.SkeletonScale.Scales);
            // }

            var rider = agent.RiderAgent;
            if (rider != null)
            {
                // Unset and reset the mount so the rider is reattached at the right place
                AccessTools.Method(typeof(Agent), "SetMountAgent").Invoke(rider, new[] {(object) null});
                AccessTools.Method(typeof(Agent), "SetMountAgent").Invoke(rider, new[] {(object) agent});
                //agent.RiderAgent?.Mount(null);
                //agent.RiderAgent?.Mount(agent);
            }
            //agent.RiderAgent.AgentVisuals.LazyUpdateAgentRendererData();
            //agent.RiderAgent.AgentVisuals.UpdateSkeletonScale((int)agent.RiderAgent.SpawnEquipment.BodyDeformType);
            // Doesn't have any affect...
            //AgentVisualsNativeData agentVisualsNativeData = agent.Monster.FillAgentVisualsNativeData();
            //AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(agent.Character.GetStepSize() * scale / baseScale , false);
            // animationSystemData.WalkingSpeedLimit *= scale;
            // animationSystemData.CrouchWalkingSpeedLimit *= scale;
            //animationSystemData.NumPaces = 10;
            //agent.SetActionSet(ref agentVisualsNativeData, ref animationSystemData);
        }
    }
}