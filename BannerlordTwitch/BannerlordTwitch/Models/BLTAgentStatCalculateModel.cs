using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Helpers;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerlordTwitch.Models
{
    public class BLTAgentStatCalculateModel : SandboxAgentStatCalculateModel
    {
        private readonly AgentStatCalculateModel previousModel;
        
        public static BLTAgentStatCalculateModel Current { get; private set; }

        public BLTAgentStatCalculateModel(AgentStatCalculateModel previousModel)
        {
            this.previousModel = previousModel;

            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, _ =>
            {
                activeModifiers.Clear();
            });

            Current = this;
        }

        public override void InitializeAgentStats(Agent agent, Equipment spawnEquipment, 
            AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData)
        {
            previousModel.InitializeAgentStats(agent, spawnEquipment, agentDrivenProperties, agentBuildData);
        }

        public override void InitializeMissionEquipment(Agent agent)
        {
            previousModel.InitializeMissionEquipment(agent);
        }

        public void AddModifiers(Hero hero, IEnumerable<SkillModifierDef> modifiers)
        {
            foreach (var m in modifiers)
            {
                AddModifier(hero, m);
            }
        }

        public void AddModifier(Hero hero, SkillModifierDef modifier)
        {
            if(!activeModifiers.TryGetValue(hero, out var modifiers))
            {
                modifiers = new();
                activeModifiers.Add(hero, modifiers);
            }

            modifiers.Add(modifier);
        }
        
        public void RemoveModifiers(Hero hero, IEnumerable<SkillModifierDef> modifiers)
        {
            foreach (var m in modifiers)
            {
                RemoveModifier(hero, m);
            }
        }
        
        public void RemoveModifier(Hero hero, SkillModifierDef modifier)
        {
            if(activeModifiers.TryGetValue(hero, out var modifiers))
            {
                modifiers.Remove(modifier);
            }
        }
        
        private readonly Dictionary<Hero, List<SkillModifierDef>> activeModifiers = new(); 

        public override void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            // Our EffectiveSkill override is called from UpdateAgentStats
            var hero = (agent.Character as CharacterObject)?.HeroObject;
            if(hero != null && activeModifiers.ContainsKey(hero))
                base.UpdateAgentStats(agent, agentDrivenProperties);
            else
                previousModel.UpdateAgentStats(agent, agentDrivenProperties);
        }

        public override float GetDifficultyModifier()
        {
            return previousModel.GetDifficultyModifier();
        }

        public override float GetEffectiveMaxHealth(Agent agent)
        {
            return previousModel.GetEffectiveMaxHealth(agent);
        }

        public override float GetWeaponInaccuracy(Agent agent, WeaponComponentData weapon, int weaponSkill)
        {
            return previousModel.GetWeaponInaccuracy(agent, weapon, weaponSkill);
        }

        public override float GetInteractionDistance(Agent agent)
        {
            return previousModel.GetInteractionDistance(agent);
        }

        public override float GetMaxCameraZoom(Agent agent)
        {
            return previousModel.GetMaxCameraZoom(agent);
        }

        public override int GetEffectiveSkill(BasicCharacterObject agentCharacter, IAgentOriginBase agentOrigin, 
            Formation agentFormation, SkillObject skill)
        {
            float baseModifiedSkill = previousModel.GetEffectiveSkill(agentCharacter, agentOrigin, agentFormation, skill);
            var hero = (agentCharacter as CharacterObject)?.HeroObject;
            if (hero != null && activeModifiers.TryGetValue(hero, out var modifiers))
            {
                baseModifiedSkill = modifiers
                    .Where(m => SkillGroup.GetSkills(m.Skill).Contains(skill))
                    .Aggregate(baseModifiedSkill, (current, m) => m.Apply(current));
            }

            return (int) baseModifiedSkill;
        }

        public override string GetMissionDebugInfoForAgent(Agent agent)
        {
            return previousModel.GetMissionDebugInfoForAgent(agent);
        }
    }
}