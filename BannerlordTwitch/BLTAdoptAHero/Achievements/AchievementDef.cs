using System.ComponentModel;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.UI;
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

        [Category("General"), InstanceName, PropertyOrder(2), UsedImplicitly] 
        public string Name { get; set; } = "New Achievement";

        [Category("General"), PropertyOrder(3),
         Description("Text that will display when the achievement is gained and when the player lists their " +
                     "achievements. Placeholders: {viewer} for the viewers name, and {name} for the name of the " +
                     "achievement."), UsedImplicitly]
        public string NotificationText { get; set; } = "{viewer} got {name}!";

        [Category("Requirements"), PropertyOrder(1), UsedImplicitly, 
         Editor(typeof(DerivedClassCollectionEditor<IAchievementRequirement>), 
             typeof(DerivedClassCollectionEditor<IAchievementRequirement>))]
        public ObservableCollection<IAchievementRequirement> Requirements { get; set; } = new();

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
         Description("Specifications of the custom item reward, if enabled"), ExpandableObject, UsedImplicitly]
        public GeneratedRewardDef ItemReward { get; set; } = new();

        public bool IsAchieved(Hero hero) => Requirements.All(r => r.IsMet(hero));

        // For UI use
        public string RewardsDescription => (GoldGain > 0 ? $"{GoldGain}{Naming.Gold}\n" : "") +
                                            (XPGain > 0 ? $"{XPGain}{Naming.XP}\n" : "") +
                                            (GiveItemReward ? "Item" : "");
        public override string ToString()
            => $"{Name} " +
               string.Join("+", Requirements.Select(r => r.ToString())) +
               $" Reward: {RewardsDescription}";
        
        public object Clone()
        {
            var clone = CloneHelpers.CloneProperties(this);
            clone.ID = Guid.NewGuid();
            clone.Requirements = new(CloneHelpers.CloneCollection(Requirements).ToList());
            return clone;
        }

        // DOING: 
        // Refactor hero powers to a list instead of fixed set
        // Add unlock criteria to hero powers (stats)

        public event PropertyChangedEventHandler PropertyChanged;

        public void Apply(Hero hero)
        {
            var results = new List<string>{ $"Unlocked {Name}" };
            if (GoldGain > 0)
            {
                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(hero, GoldGain);
                BLTAdoptAHeroCommonMissionBehavior.Current?.RecordGoldGain(hero, GoldGain);
                results.Add($"{Naming.Inc}{GoldGain}{Naming.Gold}");
            }

            if (XPGain > 0)
            {
                (bool success, string description) = SkillXP.ImproveSkill(hero, XPGain, SkillsEnum.All, auto: true);
                if (success)
                {
                    results.Add(description);
                }
                BLTAdoptAHeroCommonMissionBehavior.Current?.RecordXPGain(hero, XPGain);
            }

            if (GiveItemReward)
            {
                var (item, modifier, slot) = ItemReward.Generate(hero);
                if (item != null)
                {
                    results.Add(RewardHelpers.AssignCustomReward(hero, item, modifier, slot));
                }
            }
            
            if (results.Any())
            {
                Log.LogFeedResponse(hero.FirstName.ToString(), results.ToArray());
            }
        }
    }
}
