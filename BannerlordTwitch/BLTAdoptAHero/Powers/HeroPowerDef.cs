using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    public interface IHeroPowerPassive
    {
        void OnAdded(Hero hero);
        void OnRemoved(Hero hero);

        void OnBattleStart(Hero hero);
        void OnBattleTick(Hero hero, Agent agent);
        void OnBattleEnd(Hero hero);
        void OnAgentBuild(Hero hero, Agent agent);
        void OnAgentKilled(Hero hero, Agent agent, Hero killerHero, Agent killerAgent);
        void OnDoDamage(Hero hero, Agent agent, Hero victimHero, Agent victimAgent, ref AttackCollisionData attackCollisionData);
        void OnTakeDamage(Hero hero, Agent agent, Hero attackerHero, Agent attackerAgent, ref AttackCollisionData attackCollisionData);
    }

    public interface IHeroPowerActive
    {
        bool CanActivate();
        
        void Activate();
        
        void OnBattleTick();
        void OnCampaignTick();
    }
    
    public class HeroPowerDefBase
    {
        private static readonly Dictionary<Guid, Type> registeredPowers = new();

        public static void RegisterPowerType<T>()
        {
            var type = typeof(T);
            var instance = (HeroPowerDefBase)Activator.CreateInstance(type);
            registeredPowers.Add(instance.Type, type);
        }

        internal static IEnumerable<Type> RegisteredPowerDefTypes => registeredPowers.Values;

        public HeroPowerDefBase ConvertToProperType()
        {
            if (!registeredPowers.TryGetValue(Type, out var type))
            {
                Log.Error($"HeroPowerDef {Type} ({Name}) was not found");
                return null;
            }
            return (HeroPowerDefBase) YamlHelpers.ConvertObject(this, type);
        }
            
        [Browsable(false)]
        public Guid Type { get; set; }
        
        [ReadOnly(true), UsedImplicitly]
        public Guid ID { get => ObjectIDRegistry.Get(this); set => ObjectIDRegistry.Set(this, value); }

        [Description("Name of the power that will be shown in game"), PropertyOrder(1)]
        public string Name { get; set; } = "Enter Name Here";

        public override string ToString() => $"{Name}";

        public class ItemSource
        {
            public static IEnumerable<HeroPowerDefBase> All { get; set; }
        }
        
        public class ItemSourcePassive : ItemSource, IItemsSource
        {
            public ItemCollection GetValues()
            {
                var col = new ItemCollection();
                col.Add(Guid.Empty, "(none)");

                if (All != null)
                {
                    foreach (var item in All.Where(i => i is IHeroPowerPassive))
                    {
                        col.Add(item.ID, item.ToString());
                    }
                }

                return col;
            }
        }
        
        public class ItemSourceActive : ItemSource, IItemsSource
        {
            public ItemCollection GetValues()
            {
                var col = new ItemCollection();
                col.Add(Guid.Empty, "(none)");

                if (All != null)
                {
                    foreach (var item in All.Where(i => i is IHeroPowerActive))
                    {
                        col.Add(item.ID, item.ToString());
                    }
                }

                return col;
            }
        }
    }
}