using System.ComponentModel;
using System.Linq;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch.Helpers
{
    public class OneShotEffect
    {
        [Description("Particle Effect to play"), ItemsSource(typeof(ParticleEffectItemSource)), PropertyOrder(1)]
        public string ParticleEffect { get; set; }

        [Description("Sound to play"), ItemsSource(typeof(SoundEffectItemSource)), PropertyOrder(2)]
        public string Sound { get; set; }

        public void Trigger(Hero hero)
        {
            var agent = Mission.Current?.Agents?.FirstOrDefault(a => a.GetHero() == hero);
            if (agent == null) return;
            Trigger(agent.AgentVisuals.GetGlobalFrame(), agent.Index);
        }
                
        public void Trigger(Agent agent)
        {
            Trigger(agent.AgentVisuals.GetGlobalFrame(), agent.Index);
        }

        public void Trigger(MatrixFrame location, int relatedAgentIndex = -1)
        {
            if (!string.IsNullOrEmpty(ParticleEffect))
            {
                Mission.Current.Scene.CreateBurstParticle(
                    ParticleSystemManager.GetRuntimeIdByName(ParticleEffect),
                    location);
            }

            if (!string.IsNullOrEmpty(Sound))
            {
                Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(Sound),
                    location.origin, false, true, relatedAgentIndex, -1);
            }
        }

        public override string ToString() => $"{ParticleEffect} {Sound}";
    }
}