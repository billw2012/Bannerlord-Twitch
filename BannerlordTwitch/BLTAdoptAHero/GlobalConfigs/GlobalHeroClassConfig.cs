using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Achievements;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
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
        [Description("Defined classes"), PropertyOrder(1), UsedImplicitly] 
        public ObservableCollection<HeroClassDef> ClassDefs { get; set; } = new();

        [Description("Requirements for class levels"), PropertyOrder(2), UsedImplicitly]
        public ObservableCollection<ClassLevelRequirementsDef> ClassLevelRequirements { get; set; }
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

        public int GetHeroClassLevel(Hero hero)
        {
            int level = 0;
            foreach (var requirements in ClassLevelRequirements)
            {
                if (!requirements.IsMet(hero)) return level;
                ++level;
            }
            return level;
        }
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

    public class ClassLevelRequirementsDef : INotifyPropertyChanged, ILoaded, ICloneable
    {
        [Description("Requirements for this class level"), PropertyOrder(1), UsedImplicitly, 
         Editor(typeof(DerivedClassCollectionEditor<IAchievementRequirement>), 
             typeof(DerivedClassCollectionEditor<IAchievementRequirement>))]
        public ObservableCollection<IAchievementRequirement> Requirements { get; set; } = new();

        [YamlIgnore, Browsable(false)]
        public int ClassLevel => ClassConfig.ClassLevelRequirements.IndexOf(this) + 1;
        
        public ClassLevelRequirementsDef()
        {
            // For when these are created via the configure tool
            ClassConfig = ConfigureContext.CurrentlyEditedSettings == null 
                ? null : GlobalHeroClassConfig.Get(ConfigureContext.CurrentlyEditedSettings);
        }
        
        public override string ToString() 
            => $"{ClassConfig.ClassLevelRequirements.IndexOf(this) + 1}: "
                + (!Requirements.Any() 
                ? "(none)" 
                : string.Join(" + ", Requirements.Select(r => r.ToString())));

        public object Clone() =>
            new ClassLevelRequirementsDef
            {
                Requirements = new(CloneHelpers.CloneCollection(Requirements)),
            };

        public bool IsMet(Hero hero) => Requirements.All(r => r.IsMet(hero));
            
        #region Implementation Details
        [YamlIgnore, Browsable(false)]
        private GlobalHeroClassConfig ClassConfig { get; set; }
        #endregion
                
        #region ILoaded
        public void OnLoaded(Settings settings)
        {
            ClassConfig = GlobalHeroClassConfig.Get(settings);
        }
        #endregion
        
        public event PropertyChangedEventHandler PropertyChanged;
    }
}