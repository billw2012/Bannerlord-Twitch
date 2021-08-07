using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    internal class GlobalHeroClassConfig : IUpdateFromDefault, IDocumentable
    {
        #region Static
        private const string ID = "Adopt A Hero - Class Config";
        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalHeroClassConfig));
        internal static GlobalHeroClassConfig Get() => ActionManager.GetGlobalConfig<GlobalHeroClassConfig>(ID);
        internal static GlobalHeroClassConfig Get(Settings fromSettings) => fromSettings.GetGlobalConfig<GlobalHeroClassConfig>(ID);
        #endregion
        
        #region User Editable
        [Description("Defined classes"), UsedImplicitly] 
        public ObservableCollection<HeroClassDef> ClassDefs { get; set; } = new();
        #endregion

        #region Public Interface
        
        [Browsable(false), YamlIgnore]
        public IEnumerable<HeroClassDef> ValidClasses => ClassDefs.Where(c => c.Enabled);
        
        [Browsable(false), YamlIgnore]
        public IEnumerable<string> ClassNames => ValidClasses.Select(c => c.Name?.ToLower());

        public HeroClassDef GetClass(Guid id) 
            => ValidClasses.FirstOrDefault(c => c.ID == id);

        public HeroClassDef FindClass(string search) 
            => ValidClasses.FirstOrDefault(c => c.Name.Equals(search, StringComparison.InvariantCultureIgnoreCase));
        #endregion

        #region IUpdateFromDefault
        public void OnUpdateFromDefault(Settings defaultSettings)
        {
            SettingsHelpers.MergeCollections(
                ClassDefs, 
                Get(defaultSettings).ClassDefs,
                (a, b) => a.ID == b.ID
            );
        }
        #endregion
        
        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.Div("class-config", () =>
            {
                generator.H1("Classes");
                // foreach (var cl in ClassDefs)
                // {
                //     generator.LinkToAnchor(cl.Name, () => generator.H2(cl.Name));
                // }
                foreach (var cl in ValidClasses)
                {
                    generator.MakeAnchor(cl.Name, () => generator.H2(cl.Name));
                    cl.GenerateDocumentation(generator);
                    generator.Br();
                }
            });
        }
        #endregion
    }
}