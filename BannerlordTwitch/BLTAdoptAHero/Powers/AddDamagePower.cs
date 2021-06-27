using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class AddDamagePower : HeroPowerDefBase, IHeroPowerPassive
    {
        [Category("Power Config"), Description("How much damage to add"), PropertyOrder(1), UsedImplicitly]
        public int? DamageToAdd { get; set; }
        [Category("Power Config"), Description("How much to multiply base damage by"), PropertyOrder(2), UsedImplicitly]
        public float? DamageToMultiply { get; set; }

        [Category("Power Config"), Description("Whether to apply this bonus damage against normal troops"), PropertyOrder(3), UsedImplicitly]
        public bool ApplyAgainstNonHeroes { get; set; } = true;
        [Category("Power Config"), Description("Whether to apply this bonus damage against heroes"), PropertyOrder(4), UsedImplicitly]
        public bool ApplyAgainstHeroes { get; set; } = true;
        [Category("Power Config"), Description("Whether to apply this bonus damage against adopted heroes"), PropertyOrder(5), UsedImplicitly]
        public bool ApplyAgainstAdoptedHeroes { get; set; } = true;
        [Category("Power Config"), Description("Whether to apply this bonus damage against the player"), PropertyOrder(6), UsedImplicitly]
        public bool ApplyAgainstPlayer { get; set; } = true;
        
        public AddDamagePower()
        {
            Type = new ("378648B6-5586-4812-AD08-22DA6374440C");
        }

        public void OnAdded(Hero hero) {}
        public void OnRemoved(Hero hero) {}
        public void OnBattleStart(Hero hero) {}
        public void OnBattleTick(Hero hero, Agent agent) {}
        public void OnBattleEnd(Hero hero) {}
        public void OnAgentBuild(Hero hero, Agent agent) {}
        public void OnAgentKilled(Hero hero, Agent agent, Hero killerHero, Agent killerAgent) {}
        public void OnDoDamage(Hero hero, Agent agent, Hero victimHero, Agent victimAgent, ref AttackCollisionData attackCollisionData)
        {
            if (!ApplyAgainstAdoptedHeroes && victimHero != null
                || !ApplyAgainstHeroes && victimAgent.IsHero
                || !ApplyAgainstNonHeroes && !victimAgent.IsHero
                || !ApplyAgainstPlayer && victimAgent == Agent.Main)
            {
                return;
            }
            if (DamageToMultiply.HasValue && DamageToMultiply.Value != 1f)
            {
                //attackCollisionData.BaseMagnitude = (int) (attackCollisionData.BaseMagnitude * DamageToMultiply.Value);
                attackCollisionData.InflictedDamage = (int) (attackCollisionData.InflictedDamage * DamageToMultiply.Value);
            }
            if (DamageToAdd.HasValue && DamageToAdd.Value != 0f)
            {
                //attackCollisionData.BaseMagnitude += DamageToAdd.Value;
                attackCollisionData.InflictedDamage += DamageToAdd.Value;
            }        
        }
        public void OnTakeDamage(Hero hero, Agent agent, Hero attackerHero, Agent attackerAgent,
            ref AttackCollisionData attackCollisionData) { }

        public override string ToString()
        {
            var parts = new List<string>();
            if (DamageToMultiply.HasValue && DamageToMultiply.Value != 1)
            {
                parts.Add($"x{DamageToMultiply.Value:0.0} ({DamageToMultiply.Value * 100:0.0}%) health");
            }
            if (DamageToAdd.HasValue && DamageToAdd.Value != 0)
            {
                parts.Add(DamageToAdd > 0 ? $"+{DamageToAdd.Value} damage" : $"{DamageToAdd.Value} damage");
            }
            if (ApplyAgainstNonHeroes) parts.Add("Non-heroes");
            if (ApplyAgainstHeroes) parts.Add("Heroes");
            if (ApplyAgainstAdoptedHeroes) parts.Add("Adopted");
            if (ApplyAgainstPlayer) parts.Add("Player");
            return $"{Name}: {string.Join(", ", parts)}";
        }
    }
}