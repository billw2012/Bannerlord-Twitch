using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch.Rewards;
using BLTAdoptAHero.Powers;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    internal class GlobalHeroPowerConfig : IConfig
    {
        private const string ID = "Adopt A Hero - Power Config";
        internal static void Register()
        {
            ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalHeroPowerConfig));
        }

        internal static GlobalHeroPowerConfig Get() => ActionManager.GetGlobalConfig<GlobalHeroPowerConfig>(ID);

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

        // This is just a copy of the classes that existed on loading, so we can assign unique IDs to any new ones when
        // we save
        private List<HeroPowerDefBase> powersOnLoad;
        public void OnLoaded()
        {
            PowerDefs = SavedPowerDefs
                .Select(o => YamlHelpers.ConvertObject<HeroPowerDefBase>(o)?.ConvertToProperType())
                .Where(p => p != null)
                .ToList();
            
            foreach (var c in PowerDefs
                .GroupBy(c => c.ID)
                .SelectMany(g => g.Skip(1)))
            {
                c.ID = Guid.NewGuid();
            }
            powersOnLoad = PowerDefs.ToList();
        }

        public void OnSaving()
        {
            // Assign unique IDs to new class definitions
            foreach (var classDef in PowerDefs.Except(powersOnLoad))
            {
                classDef.ID = Guid.NewGuid();
            }
            
            SavedPowerDefs = PowerDefs.Cast<object>().ToList();
        }

        public void OnEditing()
        {
            HeroPowerDefBase.ItemSource.All = PowerDefs;
        }
    }
}