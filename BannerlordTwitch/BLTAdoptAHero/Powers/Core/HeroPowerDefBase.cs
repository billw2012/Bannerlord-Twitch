using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    /*
     * How powers will work:
     * Passive:
     * - Effect some thing related to the hero for as long as the hero has the power, e.g. max retinue, costs or whatever.
     *   - Implementation with depend on the thing that is changed
     * - Effect battle events, either for the heroes agent, agents they hit, or the heroes retinue agents.
     *   - Implementation can be by listeners attached when the battle starts to any heroes in the involved parties,
     *   or when the hero is summoned
     *
     * Active:
     * - Immediate effect on arbitrary thing, e.g. break city wall in siege
     *   - Implementation would be bespoke, in the Activate function
     * - Effect on an arbitrary thing over time, e.g. increase player party speed for 30 seconds
     *   - Implementation would be bespoke, in the Activate function, adding OnTick handler of some kind (siege, campaign?)
     * - Effect over time on battle events
     *   - Implementation can be by listeners attached when the effect is activated, and removed then it expires
     *
     * Any 
     */
    public interface IHeroPowerPassive 
    {
        // void OnAdded(Hero hero);
        // void OnRemoved(Hero hero);
        void OnHeroJoinedBattle(Hero hero, BLTHeroPowersMissionBehavior.Handlers handlers);
    }

    public interface IHeroPowerActive
    {
        (bool canActivate, string failReason) CanActivate(Hero hero);
        bool IsActive(Hero hero);
        void Activate(Hero hero, Action expiryCallback);
        float DurationFractionRemaining(Hero hero);
    }

    public class HeroPowerDefBase
    {
        private static readonly Dictionary<Guid, Type> registeredPowers = new();

        public static void RegisterPowerType<T>() => RegisterPowerType(typeof(T));

        public static void RegisterPowerType(Type type)
        {
            var instance = (HeroPowerDefBase)Activator.CreateInstance(type);
            registeredPowers.Add(instance.Type, type);
        }

        public static void RegisterAll(Assembly assembly)
        {
            var powerTypes = assembly
                .GetTypes()
                .Where(t => typeof(HeroPowerDefBase).IsAssignableFrom(t) && !t.IsAbstract);
            foreach (var powerType in powerTypes)
            {
                RegisterPowerType(powerType);
            }
        }

        internal static IEnumerable<Type> RegisteredPowerDefTypes => registeredPowers.Values;

        public HeroPowerDefBase ConvertToProperType(object o)
        {
            if (!registeredPowers.TryGetValue(Type, out var type))
            {
                Log.Error($"HeroPowerDef {Type} ({Name}) was not found");
                return null;
            }
            return (HeroPowerDefBase) YamlHelpers.ConvertObject(o, type);
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
            //public static IEnumerable<HeroPowerDefBase> All { get; set; }
            public static GlobalHeroPowerConfig Source { get; set; }
        }
        
        public class ItemSourcePassive : ItemSource, IItemsSource
        {
            public ItemCollection GetValues()
            {
                var col = new ItemCollection();
                col.Add(Guid.Empty, "(none)");

                if (Source != null)
                {
                    foreach (var item in Source.PowerDefs.Where(i => i is IHeroPowerPassive))
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

                if (Source != null)
                {
                    foreach (var item in Source.PowerDefs.Where(i => i is IHeroPowerActive))
                    {
                        col.Add(item.ID, item.ToString());
                    }
                }

                return col;
            }
        }
    }
}