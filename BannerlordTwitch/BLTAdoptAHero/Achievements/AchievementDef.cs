using System.ComponentModel;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;

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

        [Category("Requirements"), PropertyOrder(1), UsedImplicitly, 
         Editor(typeof(DerivedClassCollectionEditor<IAchievementRequirement>), 
             typeof(DerivedClassCollectionEditor<IAchievementRequirement>))]
        public List<IAchievementRequirement> Requirements { get; set; } = new();

        [Category("Reward"), PropertyOrder(1), 
         Description("Gold awarded for gaining this achievement."), UsedImplicitly]
        public int GoldGain { get; set; }

        [Category("Reward"), PropertyOrder(2), 
         Description("Experience awarded for gaining this achievement."), UsedImplicitly]
        public int XPGain { get; set; }

        [Category("Reward"), PropertyOrder(3), 
         Description("Whether to give a custom item as reward (defined below)"), UsedImplicitly]
        public bool GiveItemReward { get; set; }
        
        [Category("Reward"), PropertyOrder(4), 
         Description("Specifications of the custom item reward, if enabled"), UsedImplicitly]
        public GeneratedRewardDef ItemReward { get; set; } = new();

        public bool IsAchieved(Hero hero) => Requirements.All(r => r.IsMet(hero));

        public override string ToString()
            => $"{Name} " +
               string.Join("+", $"{Requirements.Select(r => r.ToString())}") +
               $" {(GoldGain > 0 ? GoldGain + Naming.Gold : "")} " +
               $"{(XPGain > 0 ? XPGain + Naming.XP : "")} " +
               $"{(GiveItemReward ? "Item" : "")}";
        
        public object Clone()
        {
            var newObj = CloneHelpers.CloneFields(this);
            newObj.ID = Guid.NewGuid();
            newObj.Requirements = CloneHelpers.CloneCollection(Requirements).ToList();
            return newObj;
        }

        // DOING: 
        // Refactor tournament rewards to be usable for achievements
        // Refactor hero powers to a list instead of fixed set
        // Add unlock criteria to hero powers (stats)

        public event PropertyChangedEventHandler PropertyChanged;

        public void Apply(Hero hero)
        {
            if (GoldGain > 0)
            {
                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(hero, GoldGain);
                BLTAdoptAHeroCommonMissionBehavior.Current?.RecordGoldGain(hero, GoldGain);
            }

            if (XPGain > 0)
            {
                SkillXP.ImproveSkill(hero, XPGain, SkillsEnum.All, auto: true);
                BLTAdoptAHeroCommonMissionBehavior.Current?.RecordXPGain(hero, XPGain);
            }

            if (GiveItemReward)
            {
                var (item, modifier, slot) = ItemReward.Generate(hero);
                if (item != null)
                {
                    RewardHelpers.AssignCustomReward(hero, item, modifier, slot);
                }
            }
        }
    }
}
