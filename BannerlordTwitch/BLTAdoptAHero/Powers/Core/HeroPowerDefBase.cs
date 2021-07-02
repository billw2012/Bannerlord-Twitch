using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    /// <summary>
    /// Base class for Hero Power definitions
    /// </summary>
    public class HeroPowerDefBase
    {
        #region Static Management Functions

        private static readonly Dictionary<Guid, Type> registeredPowers = new();
        public static void RegisterPowerType<T>() => RegisterPowerType(typeof(T));

        public static void RegisterPowerType(Type type)
        {
            var instance = (HeroPowerDefBase) Activator.CreateInstance(type);
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

        #endregion

        #region Saved Properties

        /// <summary>
        /// This should be set by derived classes to a unique guid (if your IDE doesn't generate them for you then
        /// use https://www.guidgenerator.com/online-guid-generator.aspx)
        /// </summary>
        [Browsable(false), UsedImplicitly] 
        public Guid Type { get; set; }

        /// <summary>
        /// This is automatically generated when the object is created, it should never be changed in code.
        /// </summary>
        [ReadOnly(true), UsedImplicitly]
        public Guid ID
        {
            get => ObjectIDRegistry.Get(this);
            set => ObjectIDRegistry.Set(this, value);
        }

        [Description("Name of the power that will be shown in game"), PropertyOrder(1), UsedImplicitly]
        public string Name { get; set; } = "Enter Name Here";

        #endregion

        // Power is loaded as an object to ensure all properties are kept in tact (actually its a dictionary under the hood),
        // and then we convert it to a HeroPowerDefBase to get the Type Guid property, then finally we convert it to the 
        // actual target Type after looking it up.
        public HeroPowerDefBase ConvertToProperType(object o)
        {
            if (!registeredPowers.TryGetValue(Type, out var type))
            {
                Log.Error($"HeroPowerDef {Type} ({Name}) was not found");
                return null;
            }

            return (HeroPowerDefBase) YamlHelpers.ConvertObject(o, type);
        }

        /// <summary>
        /// Override this to give more useful string explanations
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Name}";

        #region Item Source

        public class ItemSource
        {
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

        #endregion
    }
}