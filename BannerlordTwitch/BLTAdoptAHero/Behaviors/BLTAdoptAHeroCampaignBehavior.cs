using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Bannerlord.ButterLib.Common.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using Bannerlord.ButterLib.SaveSystem.Extensions;
using BannerlordTwitch.Annotations;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Actions.Util;
using Helpers;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    public class BLTAdoptAHeroCampaignBehavior : CampaignBehaviorBase
    {
        public static BLTAdoptAHeroCampaignBehavior Current => GetCampaignBehavior<BLTAdoptAHeroCampaignBehavior>();

        private class HeroData
        {
            public class RetinueData
            {
                [SaveableProperty(0)]
                public CharacterObject TroopType { get; set; }
                [SaveableProperty(1)]
                public int Level { get; set; }
                [SaveableProperty(2)]
                public int SavedTroopIndex { get; set; }
            }

            public class AchievementData
            {
                [SaveableProperty(0)]
                public int TotalKills { get; set; }
                [SaveableProperty(1)]
                public int TotalDeaths { get; set; }
                [SaveableProperty(2)]
                public int TotalSummons { get; set; }
                [SaveableProperty(3)]
                public int TotalAttacks { get; set; }
                [SaveableProperty(4)]
                public int TotalMainKills { get; set; }
                [SaveableProperty(5)]
                public int TotalBLTKills { get; set; }
                [SaveableProperty(6)]
                public List<Guid> Achievements { get; set; } = new();

                public int ModifyValue(AchievementSystem.AchievementTypes type, int amount)
                {
                    return type switch
                    {
                        AchievementSystem.AchievementTypes.Summons => TotalSummons += amount,
                        AchievementSystem.AchievementTypes.TotalKills => TotalKills += amount,
                        AchievementSystem.AchievementTypes.TotalBLTKills => TotalBLTKills += amount,
                        AchievementSystem.AchievementTypes.TotalMainKills => TotalMainKills += amount,
                        AchievementSystem.AchievementTypes.Attacks => TotalAttacks += amount,
                        AchievementSystem.AchievementTypes.Deaths => TotalDeaths += amount,
                        _ => throw new ArgumentOutOfRangeException(nameof(type), type,
                            "Invalid AchievementType, probably settings are corrupt?")
                    };
                }

                public int GetValue(AchievementSystem.AchievementTypes type) =>
                    type switch 
                    {
                        AchievementSystem.AchievementTypes.Summons => TotalSummons,
                        AchievementSystem.AchievementTypes.TotalKills => TotalKills,
                        AchievementSystem.AchievementTypes.TotalBLTKills => TotalBLTKills,
                        AchievementSystem.AchievementTypes.TotalMainKills => TotalMainKills,
                        AchievementSystem.AchievementTypes.Attacks => TotalAttacks,
                        AchievementSystem.AchievementTypes.Deaths => TotalDeaths,
                        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid AchievementType, probably settings are corrupt?")
                    };
            }

            [SaveableProperty(0)]
            public int Gold { get; set; }

            [SaveableProperty(1)]
            public List<RetinueData> Retinue { get; set; } = new();

            [SaveableProperty(2)]
            public int SpentGold { get; set; }

            [SaveableProperty(3)]
            public int EquipmentTier { get; set; } = -2;

            [SaveableProperty(4)]
            public Guid EquipmentClassID { get; set; }

            [SaveableProperty(5)]
            public Guid ClassID { get; set; }

            [SaveableProperty(6)]
            public string Owner { get; set; }

            [SaveableProperty(7)]
            public bool IsRetiredOrDead { get; set; }

            [SaveableProperty(8)]
            public AchievementData AchievementInfo { get; set; } = new();
            
            public class SavedEquipment
            {
                [SaveableProperty(1), UsedImplicitly]
                public ItemObject Item { get; set; }
                [SaveableProperty(2), UsedImplicitly]
                public string ItemModifierId { get; set; }
                
                public SavedEquipment() {}
            
                public SavedEquipment(EquipmentElement element)
                {
                    Item = element.Item;
                    ItemModifierId = element.ItemModifier.StringId;
                }
            
                public static explicit operator EquipmentElement(SavedEquipment m) 
                    => new(m.Item, MBObjectManager.Instance.GetObject<ItemModifier>(m.ItemModifierId));
            }

            [SaveableProperty(9), UsedImplicitly]
            public List<SavedEquipment> SavedCustomItems { get; set; } = new();
            
            public List<EquipmentElement> CustomItems { get; set; } = new();
            
            // [SaveableProperty(7)]
            // public string OriginalName { get; set; }

            public void PreSave()
            {
                SavedCustomItems = CustomItems.Select(c => new SavedEquipment(c)).ToList();
            }
            
            public void PostLoad()
            {
                CustomItems = SavedCustomItems.Select(c => (EquipmentElement)c).ToList();
            }
        }

        private Dictionary<Hero, HeroData> heroData = new();

        public override void RegisterEvents()
        {
            // We put all initialization that relies on loading being complete into this listener
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, () =>
            {
                // Ensure all existing heroes are registered
                foreach (var hero in HeroHelpers.AllHeroes.Where(h => h.IsAdopted()))
                {
                    GetHeroData(hero);
                }
                
                // Clean up hero data
                foreach (var (hero, data) in heroData)
                {
                    // Remove invalid troop types (delayed to ensure all troop types are loaded)
                    data.Retinue.RemoveAll(r => r.TroopType == null);
                    
                    // Ensure Level is set (delayed to ensure all troop trees are loaded)
                    foreach (var r in data.Retinue.Where(r => r.Level == 0))
                    {
                        var rootUnit = CharacterHelper.FindUpgradeRootOf(r.TroopType);
                        r.Level = rootUnit != null 
                            ? r.TroopType.Tier - rootUnit.Tier + 1 
                            : 1;
                    }

                    // Make sure heroes are active, and in real locations (delayed to make sure all locations are loaded)
                    if(hero.HeroState is Hero.CharacterStates.NotSpawned && hero.CurrentSettlement == null)
                    {
                        // Activate them and put them in a random town
                        hero.ChangeState(Hero.CharacterStates.Active);
                        var targetSettlement = Settlement.All.Where(s => s.IsTown).SelectRandom();
                        EnterSettlementAction.ApplyForCharacterOnly(hero, targetSettlement);
                        Log.Info($"Placed unspawned hero {hero.Name} at {targetSettlement.Name}");
                    }  
                    
                    // Make sure all custom items are in the heroes storage (delayed to ensure BLTCustomItemsCampaignBehavior is loaded)
                    foreach (var s in hero.BattleEquipment
                        .YieldFilledEquipmentSlots()
                        .Where(i => BLTCustomItemsCampaignBehavior.Current.IsRegistered(i.ItemModifier)))
                    {
                        AddCustomItem(hero, s);
                    }
                }

                // Retire up any dead heroes (do this last to ensure all other stuff related to this hero is updated, in-case retirement interferes with it)
                foreach (var (hero, data) in heroData.Where(h => h.Key.IsDead && !h.Value.IsRetiredOrDead))
                {
                    RetireHero(hero);
                }
            });
            
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, (victim, killer, detail, _) =>
            {
                if (victim?.IsAdopted() == true || killer?.IsAdopted() == true)
                {
                    string verb = KillDetailVerb(detail);
                    if (killer != null && victim != null)
                    {
                        Log.LogFeedEvent($"{victim.Name} {verb} by {killer.Name}!");
                    }
                    else if (killer != null)
                    {
                        Log.LogFeedEvent($"{killer.Name} {verb}!");
                    }
                }
            });
            
            CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, (hero, _) =>
            {
                if (hero.IsAdopted())
                    Log.LogFeedEvent($"{hero.Name} is now level {hero.Level}!");
            });
            
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, (party, hero) =>
            {
                if (hero.IsAdopted())
                {
                    if(party != null)
                        Log.LogFeedEvent($"{hero.Name} was taken prisoner by {party.Name}!");
                    else
                        Log.LogFeedEvent($"{hero.Name} was taken prisoner!");
                }
            });
            
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, (hero, party, _, _) =>
            {
                if (hero.IsAdopted())
                {
                    if(party != null)
                        Log.LogFeedEvent($"{hero.Name} is no longer a prisoner of {party.Name}!");
                    else
                        Log.LogFeedEvent($"{hero.Name} is no longer a prisoner!");
                }
            });
            
            CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, (hero, clan) =>
            {
                if(hero.IsAdopted())
                    Log.LogFeedEvent($"{hero.Name} moved from {clan?.Name.ToString() ?? "no clan"} to {hero.Clan?.Name.ToString() ?? "no clan"}!");
            });
            
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, JoinTournament.SetupGameMenus);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncDataAsJson("HeroData", ref heroData);
            
            if (dataStore.IsLoading)
            {
                Dictionary<Hero, HeroData> oldHeroData = null;
                dataStore.SyncDataAsJson("HeroData", ref oldHeroData);

                List<Hero> usedHeroList = null;
                dataStore.SyncData("UsedHeroObjectList", ref usedHeroList);

                List<CharacterObject> usedCharList = null;
                dataStore.SyncData("UsedCharacterObjectList", ref usedCharList);

                Dictionary<int, HeroData> heroData2 = null;
                dataStore.SyncDataAsJson("HeroData2", ref heroData2);
                if (heroData2 == null && oldHeroData != null)
                {
                    heroData = oldHeroData;
                }
                else if (heroData2 != null)
                {
                    heroData = heroData2.ToDictionary(kv => usedHeroList[kv.Key], kv => kv.Value);
                    foreach (var r in heroData.Values.SelectMany(h => h.Retinue))
                    {
                        r.TroopType = usedCharList[r.SavedTroopIndex];
                    }
                }

                foreach (var h in heroData.Values)
                {
                    h.PostLoad();
                }
                
                foreach (var (hero, data) in heroData)
                {
                    // Try and find an appropriate character to replace the missing retinue with
                    foreach (var r in data.Retinue.Where(r => r.TroopType == null))
                    {
                        r.TroopType = hero.Culture?.EliteBasicTroop?.UpgradeTargets?.SelectRandom()?.UpgradeTargets?.SelectRandom();
                    }

                    // Remove any we couldn't replace
                    int count = data.Retinue.RemoveAll(r => r.TroopType == null);

                    // Compensate with gold for each one lost
                    data.Gold += count * 50000;

                    // Update EquipmentTier if it isn't set
                    if (data.EquipmentTier == -2)
                    {
                        data.EquipmentTier = EquipHero.CalculateHeroEquipmentTier(hero);
                    }

                    // Set owner name from the hero name
                    data.Owner ??= hero.FirstName.ToString();

                    data.IsRetiredOrDead = !hero.IsAdopted();
                }
                
                // // Move heroes already marked as dead or retired (no BLT tag) into the dead/retired list
                // foreach (var (hero, data) in heroData.Where(h => !h.Key.IsAdopted()).ToList())
                // {
                //     heroData.Remove(hero);
                //     retiredOrDeadHeroData.Add(hero, data);
                // }
            }
            else
            {
                // Need to explicitly write out the Heroes and CharacterObjects so we can look them up by index in the HeroData
                var usedCharList = heroData.Values.SelectMany(h => h.Retinue.Select(r => r.TroopType)).Distinct().ToList();
                dataStore.SyncData("UsedCharacterObjectList", ref usedCharList);

                var usedHeroList = heroData.Keys.ToList();
                dataStore.SyncData("UsedHeroObjectList", ref usedHeroList);

                foreach (var r in heroData.Values.SelectMany(h => h.Retinue))
                {
                    r.SavedTroopIndex = usedCharList.IndexOf(r.TroopType);
                }

                foreach (var h in heroData.Values)
                {
                    h.PreSave();
                }

                var heroDataSavable = heroData.ToDictionary(kv => usedHeroList.IndexOf(kv.Key), kv => kv.Value);
                dataStore.SyncDataAsJson("HeroData2", ref heroDataSavable);
            }
        }

        private static string KillDetailVerb(KillCharacterAction.KillCharacterActionDetail detail)
        {
            switch (detail)
            {
                case KillCharacterAction.KillCharacterActionDetail.Murdered:
                    return "was murdered";
                case KillCharacterAction.KillCharacterActionDetail.DiedInLabor:
                    return "died in labor";
                case KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge:
                    return "died of old age";
                case KillCharacterAction.KillCharacterActionDetail.DiedInBattle:
                    return "died in battle";
                case KillCharacterAction.KillCharacterActionDetail.WoundedInBattle:
                    return "was wounded in battle";
                case KillCharacterAction.KillCharacterActionDetail.Executed:
                    return "was executed";
                case KillCharacterAction.KillCharacterActionDetail.Lost:
                    return "was lost";
                default:
                case KillCharacterAction.KillCharacterActionDetail.None:
                    return "was ended";
            }
        }

        public static void SetAgentStartingHealth(Agent agent)
        {
            if (BLTAdoptAHeroModule.CommonConfig.StartWithFullHealth)
            {
                agent.Health = agent.HealthLimit;
            }

            float multiplier = MissionHelpers.InTournament()
                ? BLTAdoptAHeroModule.TournamentConfig.StartHealthMultiplier
                : BLTAdoptAHeroModule.CommonConfig.StartHealthMultiplier;

            agent.BaseHealthLimit *= Math.Max(1, multiplier);
            agent.HealthLimit *= Math.Max(1, multiplier);
            agent.Health *= Math.Max(1, multiplier);
        }

        private static string ToRoman(int number)
        {
            return number switch
            {
                < 0 => throw new ArgumentOutOfRangeException(nameof(number), "must be between 1 and 3999"),
                > 3999 => throw new ArgumentOutOfRangeException(nameof(number), "must be between 1 and 3999"),
                < 1 => string.Empty,
                >= 1000 => "M" + ToRoman(number - 1000),
                >= 900 => "CM" + ToRoman(number - 900),
                >= 500 => "D" + ToRoman(number - 500),
                >= 400 => "CD" + ToRoman(number - 400),
                >= 100 => "C" + ToRoman(number - 100),
                >= 90 => "XC" + ToRoman(number - 90),
                >= 50 => "L" + ToRoman(number - 50),
                >= 40 => "XL" + ToRoman(number - 40),
                >= 10 => "X" + ToRoman(number - 10),
                >= 9 => "IX" + ToRoman(number - 9),
                >= 5 => "V" + ToRoman(number - 5),
                >= 4 => "IV" + ToRoman(number - 4),
                >= 1 => "I" + ToRoman(number - 1)
            };
        }
        
        public void RetireHero(Hero hero)
        {
            string heroName = hero.FirstName?.Raw().ToLower();
            int count = heroData.Count(h => h.Value.IsRetiredOrDead && h.Key.FirstName?.Raw().ToLower() == heroName);

            string desc = hero.IsDead ? "deceased" : "retired";
            var oldName = hero.Name;
            HeroHelpers.SetHeroName(hero, new TextObject(hero.FirstName + $" {ToRoman(count + 1)} ({desc})"));
            Campaign.Current.EncyclopediaManager.BookmarksTracker.RemoveBookmarkFromItem(hero);
            
            Log.LogFeedEvent($"{hero.Name} is {desc}!");
            Log.Info($"Dead or retired hero {oldName} renamed to {hero.Name}");

            var data = GetHeroData(hero, suppressAutoRetire: true);
            data.IsRetiredOrDead = true;
        }

        private HeroData GetHeroData(Hero hero, bool suppressAutoRetire = false)
        {
            // Better create it now if it doesn't exist
            if (!heroData.TryGetValue(hero, out var hd))
            {
                hd = new HeroData
                {
                    Gold = hero.Gold,
                    EquipmentTier = EquipHero.CalculateHeroEquipmentTier(hero),
                };
                heroData.Add(hero, hd);
            }

            if (!suppressAutoRetire && hero.IsDead)
            {
                RetireHero(hero);
            }

            return hd;
        }

        #region Gold
        public int GetHeroGold(Hero hero) =>
            #if DEBUG
            1000000000
            #else
            GetHeroData(hero).Gold
            #endif
        ;

        public void SetHeroGold(Hero hero, int gold) => GetHeroData(hero).Gold = gold;
        
        public int ChangeHeroGold(Hero hero, int change, bool isSpending = false)
        {
            var hd = GetHeroData(hero);
            int newGold = Math.Max(0, change + hd.Gold);
            hd.Gold = newGold;
            if (isSpending && change < 0)
            {
                hd.SpentGold += -change;
            }
            return newGold;
        }

        public int InheritGold(Hero inheritor, float amount)
        {
            string inheritorName = inheritor.FirstName?.Raw();
            var ancestors = heroData.Where(h => h.Key != inheritor 
                                                && h.Key.FirstName?.Raw() == inheritorName
                                                ).ToList();
            int inheritance = (int) (ancestors.Sum(a => a.Value.SpentGold + a.Value.Gold) * amount);
            ChangeHeroGold(inheritor, inheritance);
            foreach (var (key, value) in ancestors)
            {
                value.SpentGold = 0;
                value.Gold = 0;
            }
            return inheritance;
        }
        #endregion

        #region Stats and achievements
        public void IncreaseHeroKills(Hero killer, Agent killed)
        {
            if (killed?.IsAdopted() == true)
            {
                UpdateAchievement(killer, AchievementSystem.AchievementTypes.TotalBLTKills, 1);
            }
            
            bool isMainCharacter = (killed?.Character as CharacterObject)?.HeroObject == Hero.MainHero;
            if (isMainCharacter)
            {
                UpdateAchievement(killer, AchievementSystem.AchievementTypes.TotalMainKills, 1);
            }

            UpdateAchievement(killer, AchievementSystem.AchievementTypes.TotalKills, 1);
        }

        public void IncreaseParticipationCount(Hero hero, bool playerSide) => UpdateAchievement(hero, playerSide ? AchievementSystem.AchievementTypes.Summons : AchievementSystem.AchievementTypes.Attacks, 1);

        public void IncreaseHeroDeaths(Hero hero) => UpdateAchievement(hero, AchievementSystem.AchievementTypes.Deaths, 1);

        private void UpdateAchievement(Hero hero, AchievementSystem.AchievementTypes achievementType, int amount)
        {
            var achievementData = GetHeroData(hero).AchievementInfo;

            int value = achievementData.ModifyValue(achievementType, amount);
            var achievement = BLTAdoptAHeroModule.CommonConfig.Achievements?.FirstOrDefault(k => k.Enabled && achievementType == k.Type && value == k.Value);
            if (achievement != null && !achievementData.Achievements.Contains(achievement.ID))
            {
                string message = achievement.NotificationText
                    .Replace("{player}", hero.FirstName.ToString())
                    .Replace("{name}", achievement.Name);

                Log.ShowInformation(message, hero.CharacterObject, BLTAdoptAHeroModule.CommonConfig.KillStreakPopupAlertSound);

                achievementData.Achievements.Add(achievement.ID);

                BLTAdoptAHeroCommonMissionBehavior.Current.ApplyAchievementRewards(hero, achievement.GoldGain, achievement.XPGain);
            }
        }
        #endregion

        #region Equipment
        public int GetEquipmentTier(Hero hero) => GetHeroData(hero).EquipmentTier;
        public void SetEquipmentTier(Hero hero, int tier) => GetHeroData(hero).EquipmentTier = tier;
        public HeroClassDef GetEquipmentClass(Hero hero) 
            => BLTAdoptAHeroModule.HeroClassConfig.GetClass(GetHeroData(hero).EquipmentClassID);
        public void SetEquipmentClass(Hero hero, HeroClassDef classDef) 
            => GetHeroData(hero).EquipmentClassID = classDef?.ID ?? Guid.Empty;
        public EquipmentElement FindCustomItem(Hero hero, Func<EquipmentElement, bool> predicate)
            => GetHeroData(hero).CustomItems.Where(predicate).SelectRandom();
        public List<EquipmentElement> GetCustomItems(Hero hero) => GetHeroData(hero).CustomItems;
        public void AddCustomItem(Hero hero, EquipmentElement element)
        {
            if (!BLTCustomItemsCampaignBehavior.Current.IsRegistered(element.ItemModifier))
            {
                Log.Error($"Item {element.GetModifiedItemName()} of {hero.Name} is NOT a custom item, so shouldn't be added to Custom Items storage");
                return;
            }
            var data = GetHeroData(hero);
            if (!data.CustomItems.Any(i => i.Item == element.Item && i.ItemModifier == element.ItemModifier))
            {
                data.CustomItems.Add(element);
                Log.Info($"Item {element.GetModifiedItemName()} added to storage of {hero.Name}");
            }
        }

        public IEnumerable<EquipmentElement> InheritCustomItems(Hero inheritor, int maxItems)
        {
            string inheritorName = inheritor.FirstName?.Raw();
            var ancestors = heroData.Where(h => h.Key != inheritor && h.Key.FirstName?.Raw() == inheritorName).ToList();
            var items = ancestors.SelectMany(a => a.Value.CustomItems).Shuffle().Take(maxItems).ToList();
            foreach (var item in items)
            {
                AddCustomItem(inheritor, item);
            }
            foreach (var (key, value) in ancestors)
            {
                value.CustomItems.Clear();
            }
            return items;
        }

        #endregion

        #region Class

        public HeroClassDef GetClass(Hero hero) 
            => BLTAdoptAHeroModule.HeroClassConfig.GetClass(GetHeroData(hero).ClassID);

        public void SetClass(Hero hero, HeroClassDef classDef) 
            => GetHeroData(hero).ClassID = classDef?.ID ?? Guid.Empty;

        #endregion

        #region Retinue

        public IEnumerable<CharacterObject> GetRetinue(Hero hero) 
            => GetHeroData(hero).Retinue.Select(r => r.TroopType);

        public class RetinueSettings
        {
            [Description("Maximum number of units in the retinue. " +
                         "Recommend less than 20, summons to NOT obey the games unit limits."), PropertyOrder(1)]
            public int MaxRetinueSize { get; set; } = 5;

            [Description("Cost to buy a new unit, or upgrade one"), PropertyOrder(2)]
            public int CostPerTier { get; set; } = 50000;

            [Description(
                 "Additional cost per tier multiplier (final cost for an upgrade is Cost Per Tier + Cost Per Tier x " +
                 "Current Tier x Cost Scaling Per Tier"),
             PropertyOrder(3)]
            public float CostScalingPerTier { get; set; } = 1f;

            [Description("Whether to use the adopted hero's culture (if not enabled then a random one is used)"),
             PropertyOrder(4)]
            public bool UseHeroesCultureUnits { get; set; } = true;

            [Description("Whether to allow bandit units when UseHeroesCultureUnits is disabled"), PropertyOrder(4)]
            public bool IncludeBanditUnits { get; set; } = false;

            [Description("Whether to allow basic troops"), PropertyOrder(5)]
            public bool UseBasicTroops { get; set; } = true;
            
            [Description("Whether to allow elite troops"), PropertyOrder(5)]
            public bool UseEliteTroops { get; set; } = true;
        }

        public (bool success, string status) UpgradeRetinue(Hero hero, RetinueSettings settings)
        {
            // Somewhat based on RecruitmentCampaignBehavior.UpdateVolunteersOfNotables
            
            var heroRetinue = GetHeroData(hero).Retinue;
            
            // first fill in any missing ones
            if (heroRetinue.Count < settings.MaxRetinueSize)
            {
                var troopType = MBObjectManager.Instance.GetObjectTypeList<CultureObject>()
                    .Where(c => settings.IncludeBanditUnits || c.IsMainCulture)
                    .SelectMany(c =>
                    {
                        var troopTypes = new List<CharacterObject>();
                        if(settings.UseBasicTroops && c.BasicTroop != null) troopTypes.Add(c.BasicTroop);
                        if(settings.UseEliteTroops && c.EliteBasicTroop != null) troopTypes.Add(c.EliteBasicTroop);
                        return troopTypes;
                    })
                    // At least 2 upgrade tiers available
                    .Where(c => c.UpgradeTargets?.FirstOrDefault()?.UpgradeTargets?.Any() == true)
                    .Shuffle()
                    // Sort same culture units to the front if required, but still include other units in-case the hero
                    // culture doesn't contain the requires units
                    .OrderBy(c => settings.UseHeroesCultureUnits && c.Culture != hero.Culture)
                    .FirstOrDefault();

                if (troopType == null)
                {
                    return (false, $"No valid troop type could be found, please check out settings");
                }

                int heroGold = GetHeroGold(hero);
                if (GetHeroGold(hero) < settings.CostPerTier)
                {
                    return (false, Naming.NotEnoughGold(settings.CostPerTier, heroGold));
                }
                ChangeHeroGold(hero, -settings.CostPerTier, isSpending: true);
                heroRetinue.Add(new HeroData.RetinueData { TroopType = troopType, Level = 1 });
                return (true, $"+{troopType} ({Naming.Dec}{settings.CostPerTier}{Naming.Gold})");
            }

            // upgrade the lowest tier unit
            var retinueToUpgrade = heroRetinue
                .OrderBy(h => h.TroopType.Tier)
                .FirstOrDefault(t => t.TroopType.UpgradeTargets?.Any() == true);
            if (retinueToUpgrade != null)
            {
                int upgradeCost = (int) (settings.CostPerTier + settings.CostPerTier * retinueToUpgrade.Level * settings.CostScalingPerTier);
                int heroGold = GetHeroGold(hero);
                if (GetHeroGold(hero) < upgradeCost)
                {
                    return (false, Naming.NotEnoughGold(upgradeCost, heroGold));
                }
                ChangeHeroGold(hero, -upgradeCost, isSpending: true);

                var oldTroopType = retinueToUpgrade.TroopType;
                retinueToUpgrade.TroopType = oldTroopType.UpgradeTargets.SelectRandom();
                retinueToUpgrade.Level++;
                return (true, $"{oldTroopType}{Naming.To}{retinueToUpgrade.TroopType} ({Naming.Dec}{upgradeCost}{Naming.Gold})");
            }
            return (false, $"Can't upgrade retinue any further!");
        }

        #endregion

        #region Helper Functions

        public static IEnumerable<Hero> GetAvailableHeroes(Func<Hero, bool> filter = null) =>
            HeroHelpers.AliveHeroes.Where(h =>
                    // Some buggy mods can result in null heroes
                    h != null &&
                    // Not the player of course
                    h != Hero.MainHero
                    // Don't want notables ever
                    && !h.IsNotable && h.Age >= 18f)
                .Where(filter ?? (_ => true))
                .Where(n => !n.Name.Contains(BLTAdoptAHeroModule.Tag));

        public static IEnumerable<Hero> GetAllAdoptedHeroes() => HeroHelpers.AliveHeroes.Where(n => n.Name.Contains(BLTAdoptAHeroModule.Tag));

        public static string GetFullName(string name) => $"{name} {BLTAdoptAHeroModule.Tag}";

        public static void SetHeroAdoptedName(Hero hero, string userName)
        {
            HeroHelpers.SetHeroName(hero, new (GetFullName(userName)), new (userName));
        }

        public void InitAdoptedHero(Hero newHero, string userName)
        {
            var hd = GetHeroData(newHero);
            hd.Owner = userName;
            hd.IsRetiredOrDead = false;
            SetHeroAdoptedName(newHero, userName);
        }

        public Hero GetAdoptedHero(string name)
        {
            string nameToFind = name.ToLower();

            var foundHero = heroData.FirstOrDefault(h 
                => !h.Value.IsRetiredOrDead && h.Key.FirstName?.Raw().ToLower() == nameToFind).Key;

            // correct the name to match the viewer name casing
            if (foundHero != null && foundHero.FirstName?.Raw() != name)
            {
                SetHeroAdoptedName(foundHero, name);
            }

            if (foundHero?.IsDead == true)
            {
                RetireHero(foundHero);
                return null;
            }

            return foundHero;
        }

        #endregion
    }
}