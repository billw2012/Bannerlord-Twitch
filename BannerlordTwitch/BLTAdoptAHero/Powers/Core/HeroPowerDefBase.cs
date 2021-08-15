using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using PropertyChanged;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    /// <summary>
    /// Base class for Hero Power definitions
    /// </summary>
    [YamlTagged]
    [CategoryOrder("General", 1)]
    [AddINotifyPropertyChangedInterface]
    public abstract class HeroPowerDefBase : ICloneable, INotifyPropertyChanged
    {
        #region Saved Properties
        /// <summary>
        /// This is automatically generated when the object is created, it should never be changed in code.
        /// </summary>
        [ReadOnly(true), UsedImplicitly]
        public Guid ID { get; set; } = Guid.NewGuid();

        [LocCategory("General", "{=C5T5nnix}General"), Description("Name of the power that will be shown in game"), 
         InstanceName, PropertyOrder(1), UsedImplicitly]
        public string Name { get; set; } = "Enter Name Here";
        #endregion
        
        #region Implementation Details
        public override string ToString() => $"{Name}: {Description}";
        [YamlIgnore, Browsable(false)]
        public abstract string Description { get; }  
        #endregion

        #region ICloneable
        public virtual object Clone()
        {
            var newObj = CloneHelpers.CloneProperties(this);
            newObj.ID = Guid.NewGuid();
            return newObj;
        }
        #endregion
        
        #region Item Source
        public class ItemSourcePassive : IItemsSource
        {
            public ItemCollection GetValues()
            {
                var col = new ItemCollection {{Guid.Empty, "(none)"}};

                var source = GlobalHeroPowerConfig.Get(ConfigureContext.CurrentlyEditedSettings);
                if (source != null)
                {
                    foreach (var item in source.PowerDefs.Where(i => i is IHeroPowerPassive))
                    {
                        col.Add(item.ID, item.ToString().Truncate(120));
                    }
                }
                return col;
            }
        }

        public class ItemSourceActive : IItemsSource
        {
            public ItemCollection GetValues()
            {
                var col = new ItemCollection {{Guid.Empty, "(none)"}};

                var source = GlobalHeroPowerConfig.Get(ConfigureContext.CurrentlyEditedSettings);
                if (source != null)
                {
                    foreach (var item in source.PowerDefs.Where(i => i is IHeroPowerActive))
                    {
                        col.Add(item.ID, item.ToString().Truncate(120));
                    }
                }

                return col;
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}