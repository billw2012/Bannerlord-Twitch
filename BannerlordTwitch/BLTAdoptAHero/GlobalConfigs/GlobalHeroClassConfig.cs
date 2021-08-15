using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
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
        [Description("Defined classes"),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(1), UsedImplicitly] 
        public ObservableCollection<HeroClassDef> ClassDefs { get; set; } = new();

        [Description("Requirements for class levels"),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(2), UsedImplicitly]
        public ObservableCollection<ClassLevelRequirementsDef> ClassLevelRequirements { get; set; } = new();
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

        [Browsable(false), YamlIgnore]
        public IEnumerable<ClassLevelRequirementsDef> ValidClassLevelRequirements 
            => ClassLevelRequirements
                .Where(c => c.Enabled && c.ClassLevel != 0)
                .OrderBy(c => c.ClassLevel);
        
        public int GetHeroClassLevel(Hero hero)
        {
            int level = 0;
            foreach (var requirements in ValidClassLevelRequirements)
            {
                if (!requirements.IsMet(hero)) return level;
                level = requirements.ClassLevel;
            }
            return level;
        }
        #endregion

        #region IUpdateFromDefault
        public void OnUpdateFromDefault(Settings defaultSettings)
        {
            ClassDefs ??= new();
            ClassLevelRequirements ??= new();
            
            SettingsHelpers.MergeCollections(
                ClassDefs, 
                Get(defaultSettings).ClassDefs,
                (a, b) => a.ID == b.ID
            );
            SettingsHelpers.MergeCollections(
                ClassLevelRequirements, 
                Get(defaultSettings).ClassLevelRequirements,
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

    public class ClassLevelRequirementsDef : INotifyPropertyChanged, ICloneable
    {
        #region User Editable
        [ReadOnly(true), UsedImplicitly]
        public Guid ID { get; set; } = Guid.NewGuid();

        [PropertyOrder(1), UsedImplicitly]
        public bool Enabled { get; set; }
                
        [PropertyOrder(2), Description("Class level"), UsedImplicitly] 
        public int ClassLevel { get; set; }

        [Description("Requirements for this class level"), PropertyOrder(3), UsedImplicitly, 
         Editor(typeof(DerivedClassCollectionEditor<IAchievementRequirement>), 
             typeof(DerivedClassCollectionEditor<IAchievementRequirement>))]
        public ObservableCollection<IAchievementRequirement> Requirements { get; set; } = new();
        #endregion
        
        #region Public Interface
        public override string ToString() 
            => $"{ClassLevel}: "
               + (!Requirements.Any() 
                   ? "(no requirements)" 
                   : string.Join(" + ", Requirements.Select(r => r.ToString())));

        public bool IsMet(Hero hero) => Requirements.All(r => r.IsMet(hero));
        #endregion

        #region ICloneable
        public object Clone() =>
            new ClassLevelRequirementsDef
            {
                Requirements = new(CloneHelpers.CloneCollection(Requirements)),
            };
        #endregion
                
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}