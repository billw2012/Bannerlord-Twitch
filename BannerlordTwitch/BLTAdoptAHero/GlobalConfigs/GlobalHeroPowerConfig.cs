using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BLTAdoptAHero.Powers;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    public class GlobalHeroPowerConfig : IConfig
    {
        private const string ID = "Adopt A Hero - Power Config";
        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalHeroPowerConfig));
        internal static GlobalHeroPowerConfig Get() => ActionManager.GetGlobalConfig<GlobalHeroPowerConfig>(ID);
        internal static GlobalHeroPowerConfig Get(Settings fromSettings) => fromSettings.GetGlobalConfig<GlobalHeroPowerConfig>(ID);

        #region User Editable
        [Description("Defined powers"), UsedImplicitly, YamlIgnore, 
         Editor(typeof(HeroPowerCollectionEditor), typeof(HeroPowerCollectionEditor))] 
        public List<HeroPowerDefBase> PowerDefs { get; set; } = new();
        
        [Description("Whether powers are disabled in a tournament")] 
        public bool DisablePowersInTournaments { get; set; } = true;
        #endregion

        [Browsable(false)]
        public List<object> SavedPowerDefs { get; set; } = new();

        [Browsable(false), YamlIgnore]
        public IEnumerable<string> PowerNames => PowerDefs?.Select(c => c.Name?.ToLower()) ?? Enumerable.Empty<string>();

        public HeroPowerDefBase GetPower(Guid id)
            => PowerDefs?.FirstOrDefault(c => c.ID == id);

        public HeroPowerDefBase FindPower(string search) 
            => PowerDefs?.FirstOrDefault(c => c.Name.Equals(search, StringComparison.InvariantCultureIgnoreCase));

        #region IConfig
        public void OnLoaded(Settings settings)
        {
            // We need to convert our generic loaded powers into their concrete types
            PowerDefs = SavedPowerDefs
                .Select(o => YamlHelpers.ConvertObject<HeroPowerDefBase>(o)?.ConvertToProperType(o))
                .Where(p => p != null)
                .ToList();
            
            foreach (var c in PowerDefs.OfType<IConfig>())
            {
                c.OnLoaded(settings);
            }
        }

        public void OnSaving()
        {
            foreach (var c in PowerDefs.OfType<IConfig>())
            {
                c.OnSaving();
            }

            SavedPowerDefs = PowerDefs.Cast<object>().ToList();
        }

        public void OnEditing()
        {
            HeroPowerDefBase.ItemSource.Source = this;
            
            foreach (var c in PowerDefs.OfType<IConfig>())
            {
                c.OnEditing();
            }
        }
        #endregion
    }
}