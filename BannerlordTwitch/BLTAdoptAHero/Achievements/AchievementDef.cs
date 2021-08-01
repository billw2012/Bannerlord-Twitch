using System.ComponentModel;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Achievements
{
    [CategoryOrder("General", 1)]
    [CategoryOrder("Requirements", 2)]
    [CategoryOrder("Reward", 3)]
    public sealed class AchievementDef : ICloneable, INotifyPropertyChanged
    {
        [ReadOnly(true), UsedImplicitly]
        public Guid ID { get; set; } = Guid.NewGuid();

        [Category("General"), PropertyOrder(1), UsedImplicitly]
        public bool Enabled { get; set; }

        [Category("General"), PropertyOrder(2), UsedImplicitly] 
        public string Name { get; set; } = "New Achievement";

        [Category("General"), PropertyOrder(3), 
         Description("Text that will display when the achievement is gained and when the player lists their " +
                     "achievements.  Can use {player} for the viewers name and {name} for the name of the " +
                     "achievement."), UsedImplicitly]
        public string NotificationText { get; set; }

        [Category("Requirements"), PropertyOrder(1), 
         Editor(typeof(DerivedClassCollectionEditor<IAchievementRequirement>), 
             typeof(DerivedClassCollectionEditor<IAchievementRequirement>))]
        public List<IAchievementRequirement> Requirements { get; set; } = new();
        // [Category("Requirements"), PropertyOrder(1), 
        //  Description("The statistic this achievement relates to."), UsedImplicitly]
        // public AchievementStatsData.Statistic Type { get; set; }
        //
        // [Category("Requirements"), PropertyOrder(2), 
        //  Description("Value needed to obtain the achievement."), UsedImplicitly]
        // public int Value { get; set; }
        //
        // [Category("Requirements"), PropertyOrder(3), 
        //  Description("Whether this achievement only applies to the Required Class."), UsedImplicitly]
        // public bool ClassSpecific { get; set; }
        //
        // [Category("Requirements"), 
        //  Description("Class required to get this achievement (if Class Specific is enabled). If (none) is specified " +
        //              "then the achievement will apply ONLY when the hero doesn't have a class set."), 
        //  PropertyOrder(4), ItemsSource(typeof(HeroClassDef.ItemSource)), UsedImplicitly]
        // public Guid RequiredClass { get; set; }

        [Category("Reward"), PropertyOrder(1), 
         Description("Gold awarded for gaining this achievement."), UsedImplicitly]
        public int GoldGain { get; set; }

        [Category("Reward"), PropertyOrder(2), 
         Description("Experience awarded for gaining this achievement."), UsedImplicitly]
        public int XPGain { get; set; }

        public bool IsAchieved(Hero hero) => Requirements.All(r => r.IsMet(hero));
        
        public override string ToString() 
            => $"{Name}" + //: {Value} {Type} " +
               //$"{(ClassSpecific ? "as " + (ClassConfig.GetClass(RequiredClass)?.Name ?? "no class") : "")} " +
               $"{(GoldGain > 0 ? GoldGain + Naming.Gold : "")} " +
               $"{(XPGain > 0 ? XPGain + Naming.XP : "")}";
        
        public object Clone()
        {
            var newObj = CloneHelpers.CloneFields(this);
            newObj.ID = Guid.NewGuid();
            newObj.Requirements = CloneHelpers.CloneCollection(Requirements).ToList();
            return newObj;
        }

        // DOING: Add consecutive summon and consecutive attack to stats
        // Refactor tournament rewards to be usable for achievements
        // Refactor hero powers to a list instead of fixed set
        // Add unlock criteria to hero powers (stats)

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
