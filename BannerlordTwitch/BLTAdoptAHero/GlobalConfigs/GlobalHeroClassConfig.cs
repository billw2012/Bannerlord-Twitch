using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    internal class GlobalHeroClassConfig : IConfig
    {
        private const string ID = "Adopt A Hero - Class Config";
        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalHeroClassConfig));
        internal static GlobalHeroClassConfig Get() => ActionManager.GetGlobalConfig<GlobalHeroClassConfig>(ID);
        
        [Description("Defined classes"), UsedImplicitly] 
        public List<HeroClassDef> ClassDefs { get; set; } = new();

        [Browsable(false), YamlIgnore]
        public IEnumerable<string> ClassNames => ClassDefs?.Select(c => c.Name?.ToLower()) ?? Enumerable.Empty<string>();

        public HeroClassDef GetClass(Guid id) 
            => ClassDefs?.FirstOrDefault(c => c.ID == id);

        public HeroClassDef FindClass(string search) 
            => ClassDefs?.FirstOrDefault(c => c.Name.Equals(search, StringComparison.InvariantCultureIgnoreCase));

        #region IConfig
        public void OnLoaded() { }

        public void OnSaving() { }

        public void OnEditing()
        {
            HeroClassDef.ItemSource.All = ClassDefs;
        }
        #endregion
    }
}