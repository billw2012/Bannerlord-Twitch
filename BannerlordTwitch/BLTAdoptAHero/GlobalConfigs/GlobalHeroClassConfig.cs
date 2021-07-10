using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    internal class GlobalHeroClassConfig : IConfig, IDocumentable
    {
        private const string ID = "Adopt A Hero - Class Config";
        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalHeroClassConfig));
        internal static GlobalHeroClassConfig Get() => ActionManager.GetGlobalConfig<GlobalHeroClassConfig>(ID);
        internal static GlobalHeroClassConfig Get(Settings fromSettings) => fromSettings.GetGlobalConfig<GlobalHeroClassConfig>(ID);
        
        [Description("Defined classes"), UsedImplicitly] 
        public List<HeroClassDef> ClassDefs { get; set; } = new();

        [Browsable(false), YamlIgnore]
        public IEnumerable<string> ClassNames => ClassDefs?.Select(c => c.Name?.ToLower()) ?? Enumerable.Empty<string>();

        public HeroClassDef GetClass(Guid id) 
            => ClassDefs?.FirstOrDefault(c => c.ID == id);

        public HeroClassDef FindClass(string search) 
            => ClassDefs?.FirstOrDefault(c => c.Name.Equals(search, StringComparison.InvariantCultureIgnoreCase));

        #region IConfig
        public void OnLoaded(Settings settings)
        {
            foreach (var c in ClassDefs.OfType<IConfig>())
            {
                c.OnLoaded(settings);
            }
        }

        public void OnSaving()
        {
            foreach (var c in ClassDefs.OfType<IConfig>())
            {
                c.OnSaving();
            }
        }

        public void OnEditing()
        {
            HeroClassDef.ItemSource.All = ClassDefs;
            foreach (var c in ClassDefs.OfType<IConfig>())
            {
                c.OnEditing();
            }
        }
        #endregion

        #region IDocumentationGenerator
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.Div("class-config", () =>
            {
                generator.H1("Classes");
                // foreach (var cl in ClassDefs)
                // {
                //     generator.LinkToAnchor(cl.Name, () => generator.H2(cl.Name));
                // }
                foreach (var cl in ClassDefs)
                {
                    generator.H2(cl.Name);
                    cl.GenerateDocumentation(generator);
                    generator.Br();
                }
            });
        }
        #endregion
    }
}