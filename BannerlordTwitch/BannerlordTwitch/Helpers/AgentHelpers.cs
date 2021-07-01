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
            AccessTools.Method(typeof(Agent), "SetInitialAgentScale").Invoke(agent, new []{ (object) scale });
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