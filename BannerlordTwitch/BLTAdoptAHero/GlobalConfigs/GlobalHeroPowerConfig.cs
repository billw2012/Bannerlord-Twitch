using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Powers;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=v75UOuDM}Power Config")]
    public class GlobalHeroPowerConfig : IUpdateFromDefault, ILoaded
    {
        #region Static
        private const string ID = "Adopt A Hero - Power Config";
        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalHeroPowerConfig));
        internal static GlobalHeroPowerConfig Get() => ActionManager.GetGlobalConfig<GlobalHeroPowerConfig>(ID);
        internal static GlobalHeroPowerConfig Get(Settings fromSettings) => fromSettings.GetGlobalConfig<GlobalHeroPowerConfig>(ID);
        #endregion

        #region User Editable
        [LocDisplayName("{=9vUtdRu2}Power Definitions"),
         LocDescription("{=ymGZUjoU}Defined powers"),
         Editor(typeof(DerivedClassCollectionEditor<HeroPowerDefBase>), 
             typeof(DerivedClassCollectionEditor<HeroPowerDefBase>)),  
         UsedImplicitly] 
        public ObservableCollection<HeroPowerDefBase> PowerDefs { get; set; } = new();
        
        [LocDisplayName("{=5CD7bmuC}Disable Powers In Tournaments"),
         LocDescription("{=K7uKtO90}Whether powers are disabled in a tournament"),
         UsedImplicitly] 
        public bool DisablePowersInTournaments { get; set; } = true;
        
        #region Deprecated
        [Browsable(false), UsedImplicitly]
        public List<Dictionary<object, object>> SavedPowerDefs { get; set; }
        #endregion
        #endregion

        #region Public Interface
        public HeroPowerDefBase GetPower(Guid id)
            => PowerDefs?.FirstOrDefault(c => c.ID == id);
        #endregion
        
        #region IUpdateFromDefault
        public void OnUpdateFromDefault(Settings defaultSettings)
        {
            SettingsHelpers.MergeCollections(
                PowerDefs, 
                Get(defaultSettings).PowerDefs,
                (a, b) => a.ID == b.ID
            );
        }
        #endregion
        
        #region ILoaded
        public void OnLoaded(Settings settings)
        {
            // Upgrade path
            if (SavedPowerDefs != null)
            {
                PowerDefs = new(SavedPowerDefs
                    .Select(d =>
                    {
                        if (d.TryGetValue("Type", out object o) && o is string id)
                        {
                            var t = id.ToUpper() switch
                            {
                                "E0A274DF-ADBB-4725-9EAE-59806BF9B5DC" => typeof(AbsorbHealthPower),
                                "378648B6-5586-4812-AD08-22DA6374440C" => typeof(AddDamagePower),
                                "C4213666-2176-42B4-8DBB-BFE0182BCCE1" => typeof(AddHealthPower),
                                "FFE07DA3-E977-42D8-80CA-5DFFF66123EB" => typeof(ReflectDamagePower),
                                "6DF1D8D6-02C6-4D30-8D12-CCE24077A4AA" => typeof(StatModifyPower),
                                "366C25BD-5B20-4EB1-98F5-04B5FDDD6285" => typeof(TakeDamagePower),
                                _ => throw new Exception($"Power type id {id} not found")
                            };
                            
                            return (HeroPowerDefBase)YamlHelpers.ConvertObjectUntagged(d, t);
                        }

                        throw new Exception($"Invalid power found during load");
                    }));
                SavedPowerDefs = null;
            }
        }
        #endregion
    }
}