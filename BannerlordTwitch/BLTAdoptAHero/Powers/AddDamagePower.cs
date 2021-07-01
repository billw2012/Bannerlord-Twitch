using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    [Description("Adds fixed or relative amount of extra HP to the hero when they spawn"), UsedImplicitly]
    public class AddDamagePower : DurationMissionHeroPowerDefBase, IHeroPowerPassive
    {
        [Category("Power Config"), Description("How much to multiply base damage by"), PropertyOrder(1), UsedImplicitly]
        public float DamageToMultiply { get; set; } = 1f;

        [Category("Power Config"), Description("How much damage to add"), PropertyOrder(2), UsedImplicitly]
        public int DamageToAdd { get; set; } = 0;

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

        void IHeroPowerPassive.OnHeroJoinedBattle(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers1)
        {
            BLTHeroPowersMissionBehavior.Current
                .ConfigureHandlers(hero, this, handlers => handlers.OnDoDamage += OnDoDamage);
        }
        
        protected override void OnActivation(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers,
            Agent agent = null, DeactivationHandler deactivationHandler = null) 
            => handlers.OnDoDamage += OnDoDamage;

        public void OnDoDamage(Hero hero, Agent agent, Hero victimHero, Agent victimAgent, AttackCollisionDataRef attackCollisionData)
        {
            if (!ApplyAgainstAdoptedHeroes && victimHero != null
                || !ApplyAgainstHeroes && victimAgent.IsHero
                || !ApplyAgainstNonHeroes && !victimAgent.IsHero
                || !ApplyAgainstPlayer && victimAgent == Agent.Main)
            {
                return;
            }
            //attackCollisionData.BaseMagnitude = (int) (attackCollisionData.BaseMagnitude * DamageToMultiply);
            attackCollisionData.Data.InflictedDamage = (int) (attackCollisionData.Data.InflictedDamage * DamageToMultiply);
            //attackCollisionData.BaseMagnitude += DamageToAdd;
            attackCollisionData.Data.InflictedDamage += DamageToAdd;
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (DamageToMultiply != 1)
            {
                parts.Add($"x{DamageToMultiply:0.0} ({DamageToMultiply * 100:0.0}%) health");
            }
            if (DamageToAdd != 0)
            {
                parts.Add(DamageToAdd > 0 ? $"+{DamageToAdd} damage" : $"{DamageToAdd} damage");
            }
            if (ApplyAgainstNonHeroes) parts.Add("Non-heroes");
            if (ApplyAgainstHeroes) parts.Add("Heroes");
            if (ApplyAgainstAdoptedHeroes) parts.Add("Adopted");
            if (ApplyAgainstPlayer) parts.Add("Player");
            return $"{Name}: {string.Join(", ", parts)}";
        }
    }
}