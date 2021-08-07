using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.ObjectSystem;
using BannerlordTwitch;
using BannerlordTwitch.Annotations;
using BannerlordTwitch.SaveSystem;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Achievements;
using BLTAdoptAHero.Actions.Util;
using Helpers;
using Newtonsoft.Json;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    public class BLTAdoptAHeroCampaignBehavior : CampaignBehaviorBase
    {
        public static BLTAdoptAHeroCampaignBehavior Current => Campaign.Current?.GetCampaignBehavior<BLTAdoptAHeroCampaignBehavior>();

        #region HeroData
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
            public AchievementStatsData AchievementStats { get; set; } = new();
            
            public class SavedEquipment
            {
                [SaveableProperty(1), UsedImplicitly]
                public ItemObject Item { get; set; }
                [SaveableProperty(2), UsedImplicitly]
                public string ItemModifierId { get; set; }
                [SaveableProperty(3), UsedImplicitly]
                public int ItemSaveIndex { get; set; }
                
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
        #endregion

        #region CampaignBehaviorBase
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

                    // Remove invalid custom items
                    int removedCustomItems = data.CustomItems.RemoveAll(
                        i => i.Item == null || i.Item.Type == ItemObject.ItemTypeEnum.Invalid);
                    if (removedCustomItems > 0)
                    {
                        // Compensate with gold for each one lost
                        data.Gold += removedCustomItems * 50000;
                        
                        Log.LogFeedSystem(
                            $"Compensated @{hero.Name} with {removedCustomItems * 50000}{Naming.Gold} for " +
                            $"{removedCustomItems} invalid custom items");
                    }

                    // Also remove them from the equipment
                    foreach (var s in hero.BattleEquipment
                        .YieldFilledEquipmentSlots()
                        .Where(i => i.element.Item.Type == ItemObject.ItemTypeEnum.Invalid))
                    {
                        hero.BattleEquipment[s.index] = EquipmentElement.Invalid;
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
            using var scopedJsonSync = new ScopedJsonSync(dataStore, nameof(BLTAdoptAHeroCampaignBehavior));
            
            scopedJsonSync.SyncDataAsJson("HeroData", ref heroData);
            
            if (dataStore.IsLoading)
            {
                Dictionary<Hero, HeroData> oldHeroData = null;
                scopedJsonSync.SyncDataAsJson("HeroData", ref oldHeroData);

                List<Hero> usedHeroList = null;
                dataStore.SyncData("UsedHeroObjectList", ref usedHeroList);

                List<CharacterObject> usedCharList = null;
                dataStore.SyncData("UsedCharacterObjectList", ref usedCharList);

                Dictionary<int, HeroData> heroData2 = null;
                scopedJsonSync.SyncDataAsJson("HeroData2", ref heroData2);
                if (heroData2 == null && oldHeroData != null)
                {
                    heroData = oldHeroData;
                }
                else if (heroData2 != null)
                {
                    heroData = heroData2.ToDictionary(kv
                        => usedHeroList[kv.Key], kv => kv.Value);
                    foreach (var r in heroData.Values.SelectMany(h => h.Retinue))
                    {
                        r.TroopType = usedCharList[r.SavedTroopIndex];
                    }
                }
                
                List<ItemObject> saveItemList = null;
                dataStore.SyncData("SavedItems", ref saveItemList);
                foreach (var h in heroData.Values)
                {
                    if (saveItemList != null)
                    {
                        foreach (var i in h.SavedCustomItems)
                        {
                            i.Item = saveItemList[i.ItemSaveIndex];
                        }
                    }
                    h.PostLoad();
                }
                
                foreach (var (hero, data) in heroData)
                {
                    // Try and find an appropriate character to replace the missing retinue with
                    foreach (var r in data.Retinue.Where(r => r.TroopType == null))
                    {
                        r.TroopType = hero.Culture?.EliteBasicTroop
                            ?.UpgradeTargets?.SelectRandom()
                            ?.UpgradeTargets?.SelectRandom();
                    }

                    // Remove any we couldn't replace
                    int removedRetinue = data.Retinue.RemoveAll(r => r.TroopType == null);

                    // Compensate with gold for each one lost
                    data.Gold += removedRetinue * 50000;

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
                var usedCharList = heroData.Values
                    .SelectMany(h => h.Retinue.Select(r => r.TroopType)).Distinct().ToList();
                dataStore.SyncData("UsedCharacterObjectList", ref usedCharList);

                var usedHeroList = heroData.Keys.ToList();
                dataStore.SyncData("UsedHeroObjectList", ref usedHeroList);
                
                // var heroImtes = heroData.Values.SelectMany(h => h.)

                foreach (var r in heroData.Values.SelectMany(h => h.Retinue))
                {
                    r.SavedTroopIndex = usedCharList.IndexOf(r.TroopType);
                }

                var saveItemList = new List<ItemObject>();
                foreach (var h in heroData.Values)
                {
                    // PreSave first to update SavedCustomItems
                    h.PreSave();
                    foreach (var i in h.SavedCustomItems)
                    {
                        i.ItemSaveIndex = saveItemList.Count;
                        saveItemList.Add(i.Item);
                    }
                }

                dataStore.SyncData("SavedItems", ref saveItemList);

                var heroDataSavable = heroData.ToDictionary(kv 
                    => usedHeroList.IndexOf(kv.Key), kv => kv.Value);
                scopedJsonSync.SyncDataAsJson("HeroData2", ref heroDataSavable);
            }
        }
        #endregion
        
        #region Adoption
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
                    => !h.Value.IsRetiredOrDead
                       && (h.Key.FirstName?.Raw().ToLower() == nameToFind || h.Value.Owner?.ToLower() == nameToFind))
                .Key;

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
        
        public void RetireHero(Hero hero)
        {
            var data = GetHeroData(hero, suppressAutoRetire: true);
            if (data.IsRetiredOrDead) return;
            
            string heroName = hero.FirstName?.Raw().ToLower();
            int count = heroData.Count(h 
                => h.Value.IsRetiredOrDead &&
                   (h.Key.FirstName?.Raw().ToLower() == heroName || h.Value.Owner?.ToLower() == heroName));

            string desc = hero.IsDead ? "deceased" : "retired";
            var oldName = hero.Name;
            HeroHelpers.SetHeroName(hero, new (hero.FirstName + $" {ToRoman(count + 1)} ({desc})"));
            Campaign.Current.EncyclopediaManager.BookmarksTracker.RemoveBookmarkFromItem(hero);
            
            Log.LogFeedEvent($"{oldName} is {desc}!");
            Log.Info($"Dead or retired hero {oldName} renamed to {hero.Name}");

            data.IsRetiredOrDead = true;
        }
        #endregion
        
        #region Gold
        public int GetHeroGold(Hero hero) =>
            // #if DEBUG
            // 1000000000
            // #else
            GetHeroData(hero).Gold
            // #endif
        ;

        public void SetHeroGold(Hero hero, int gold) => GetHeroData(hero).Gold = gold;
        
        public int ChangeHeroGold(Hero hero, int change, bool isSpending = false)
        {
            var hd = GetHeroData(hero);
            hd.Gold = Math.Max(0, change + hd.Gold);
            if (isSpending && change < 0)
            {
                hd.SpentGold += -change;
            }
            return hd.Gold;
        }

        public int InheritGold(Hero inheritor, float amount)
        {
            string inheritorName = inheritor.FirstName?.Raw();
            var ancestors = heroData.Where(h => h.Key != inheritor 
                                                && h.Key.FirstName?.Raw() == inheritorName
                                                ).ToList();
            int inheritance = (int) (ancestors.Sum(a => a.Value.SpentGold + a.Value.Gold) * amount);
            ChangeHeroGold(inheritor, inheritance);
            foreach (var (_, value) in ancestors)
            {
                value.SpentGold = 0;
                value.Gold = 0;
            }
            return inheritance;
        }
        #endregion

        #region Stats and achievements
        public void IncreaseKills(Hero hero, Agent killed)
        {
            if (killed?.IsAdopted() == true)
            {
                IncreaseStatistic(hero, AchievementStatsData.Statistic.TotalViewerKills, 1);
            }
            else if(killed?.GetHero() == Hero.MainHero)
            {
                IncreaseStatistic(hero, AchievementStatsData.Statistic.TotalStreamerKills, 1);
            }
            else if (killed?.IsHero == true)
            {
                IncreaseStatistic(hero, AchievementStatsData.Statistic.TotalHeroKills, 1);
            }
            else if (killed?.IsMount == true)
            {
                IncreaseStatistic(hero, AchievementStatsData.Statistic.TotalMountKills, 1);
            }
            IncreaseStatistic(hero, AchievementStatsData.Statistic.TotalKills, 1);
        }

        public void IncreaseParticipationCount(Hero hero, bool playerSide)
        {
            IncreaseStatistic(hero, playerSide
                ? AchievementStatsData.Statistic.Summons
                : AchievementStatsData.Statistic.Attacks, 1);
        }

        public void IncreaseHeroDeaths(Hero hero, Agent killer)
        {
            if (killer?.IsAdopted() == true)
            {
                IncreaseStatistic(hero, AchievementStatsData.Statistic.TotalViewerDeaths, 1);
            }
            else if(killer?.GetHero() == Hero.MainHero)
            {
                IncreaseStatistic(hero, AchievementStatsData.Statistic.TotalStreamerDeaths, 1);
            }
            else if (killer?.IsHero == true)
            {
                IncreaseStatistic(hero, AchievementStatsData.Statistic.TotalHeroDeaths, 1);
            }
            else if (killer?.IsMount == true)
            {
                IncreaseStatistic(hero, AchievementStatsData.Statistic.TotalMountDeaths, 1);
            }
            IncreaseStatistic(hero, AchievementStatsData.Statistic.TotalDeaths, 1);
        }

        public void IncreaseTournamentRoundLosses(Hero hero) 
            => IncreaseStatistic(hero, AchievementStatsData.Statistic.TotalTournamentRoundLosses, 1);

        public void IncreaseTournamentRoundWins(Hero hero) 
            => IncreaseStatistic(hero, AchievementStatsData.Statistic.TotalTournamentRoundWins, 1);

        public void IncreaseTournamentChampionships(Hero hero) 
            => IncreaseStatistic(hero, AchievementStatsData.Statistic.TotalTournamentFinalWins, 1);

        private void IncreaseStatistic(Hero hero, AchievementStatsData.Statistic statistic, int amount)
        {
            var achievementStatsData = GetHeroData(hero).AchievementStats;
            
            achievementStatsData.UpdateValue(statistic, hero.GetClass()?.ID ?? default, amount);
           
            CheckForAchievements(hero);
        }

        private void CheckForAchievements(Hero hero)
        {
            var achievementData = GetHeroData(hero).AchievementStats;
            
            var newAchievements = BLTAdoptAHeroModule.CommonConfig.Achievements?
                .Where(a => a.IsAchieved(hero))
                .Where(a
                    => !achievementData.Achievements.Contains(a.ID)) ?? Enumerable.Empty<AchievementDef>();

            foreach (var achievement in newAchievements)
            {
                string message = achievement.NotificationText
                    .Replace("{player}", hero.FirstName.ToString())
                    .Replace("{name}", achievement.Name);
                Log.ShowInformation(message, hero.CharacterObject,
                    BLTAdoptAHeroModule.CommonConfig.KillStreakPopupAlertSound);
                achievementData.Achievements.Add(achievement.ID);

                achievement.Apply(hero);
            }
        }

        public int GetAchievementTotalStat(Hero hero, AchievementStatsData.Statistic type) 
            => GetHeroData(hero)?.AchievementStats?.GetTotalValue(type) ?? 0;

        public int GetAchievementClassStat(Hero hero, AchievementStatsData.Statistic type) 
            => GetHeroData(hero)?.AchievementStats?.GetClassValue(type, hero.GetClass()?.ID ?? Guid.Empty) ?? 0;

        public int GetAchievementClassStat(Hero hero, Guid classGuid, AchievementStatsData.Statistic type) 
            => GetHeroData(hero)?.AchievementStats?.GetClassValue(type, classGuid) ?? 0;

        public IEnumerable<AchievementDef> GetAchievements(Hero hero) =>
            GetHeroData(hero)?.AchievementStats?.Achievements?
                .Select(a => BLTAdoptAHeroModule.CommonConfig.GetAchievement(a))
                .Where(a => a != null) 
            ?? Enumerable.Empty<AchievementDef>();

        #endregion

        #region Equipment
        public int GetEquipmentTier(Hero hero) => GetHeroData(hero).EquipmentTier;
        public void SetEquipmentTier(Hero hero, int tier) => GetHeroData(hero).EquipmentTier = tier;
        public HeroClassDef GetEquipmentClass(Hero hero) 
            => BLTAdoptAHeroModule.HeroClassConfig.GetClass(GetHeroData(hero).EquipmentClassID);
        public void SetEquipmentClass(Hero hero, HeroClassDef classDef) 
            => GetHeroData(hero).EquipmentClassID = classDef?.ID ?? Guid.Empty;
        #endregion

        #region Custom Items
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
            if (!data.CustomItems.Any(i => i.IsEqualTo(element)))
            {
                data.CustomItems.Add(element);
                Log.Info($"Item {element.GetModifiedItemName()} added to storage of {hero.Name}");
            }
        }

        public void RemoveCustomItem(Hero hero, EquipmentElement element)
        {
            var data = GetHeroData(hero);
            
            data.CustomItems.RemoveAll(i => i.IsEqualTo(element));
            
            foreach (var slot in hero.BattleEquipment
                .YieldEquipmentSlots()
                .Where(i => i.element.IsEqualTo(element)))
            {
                hero.BattleEquipment[slot.index] = EquipmentElement.Invalid;
            }
            
            foreach (var slot in hero.CivilianEquipment
                .YieldEquipmentSlots()
                .Where(i => i.element.IsEqualTo(element)))
            {
                hero.CivilianEquipment[slot.index] = EquipmentElement.Invalid;
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
            foreach (var (_, value) in ancestors)
            {
                value.CustomItems.Clear();
            }
            return items;
        }

        private class Auction
        {
            public EquipmentElement item;
            public Hero itemOwner;
            public int reservePrice;
            private readonly Dictionary<Hero, int> bids = new();

            public Auction(EquipmentElement item, Hero itemOwner, int reservePrice)
            {
                this.item = item;
                this.itemOwner = itemOwner;
                this.reservePrice = reservePrice;
            }

            public (bool success, string description) Bid(Hero bidder, int bid)
            {
                if (itemOwner == bidder)
                {
                    return (false, $"You can't bid on your own item");
                }
                
                if (bid < reservePrice)
                {
                    return (false, $"Bid of {bid}{Naming.Gold} does not meet reserve price of {reservePrice}{Naming.Gold}");
                }

                if (bids.Values.Any(v => v == bid))
                {
                    return (false, $"Another bid at {bid}{Naming.Gold} already exists");
                }
                
                if (bids.TryGetValue(bidder, out int currBid) && currBid >= bid)
                {
                    return (false, $"You already bid more ({currBid}{Naming.Gold}), you can only raise your bid");
                }

                int bidderGold = Current.GetHeroGold(bidder);
                if (bidderGold < bid)
                {
                    return (false, $"You cannot cover a bid of {bid}{Naming.Gold}, you only have {bidderGold}{Naming.Gold}");
                }

                bids[bidder] = bid;

                return (true, $"Bid of {bid}{Naming.Gold} placed!");
            }

            public (Hero hero, int bid) GetHighestValidBid() => bids
                .Select(x => (hero: x.Key, bid: x.Value))
                .Where(x => x.hero.IsAdopted() && !x.hero.IsDead && Current.GetHeroGold(x.hero) >= x.bid)
                .OrderByDescending(x => x.bid)
                .FirstOrDefault();
        }

        private Auction currentAuction;

        public bool AuctionInProgress => currentAuction != null;
        
        public async void StartItemAuction(EquipmentElement item, Hero itemOwner, 
            int reservePrice, int durationInSeconds, int reminderInterval, Action<string> output)
        {
            if (AuctionInProgress)
                return;

            currentAuction = new (item, itemOwner, reservePrice);

            // Count down in chunks with reminder of the auction status
            while (durationInSeconds > reminderInterval)
            {
                await Task.Delay(TimeSpan.FromSeconds(reminderInterval));
                durationInSeconds -= reminderInterval;
                int seconds = durationInSeconds;
                MainThreadSync.Run(() =>
                {
                    var highestBid = currentAuction.GetHighestValidBid();
                    if (highestBid != default)
                    {
                        output($"{seconds} seconds left in auction of \"{item.GetModifiedItemName()}\", " +
                               $"high bid is {highestBid.bid}{Naming.Gold} (@{highestBid.hero.FirstName})");
                    }
                    else
                    {
                        output($"{seconds} seconds left in auction of \"{item.GetModifiedItemName()}\", no bids placed");
                    }
                });
            }
            
            await Task.Delay(TimeSpan.FromSeconds(durationInSeconds));

            MainThreadSync.Run(() =>
            {
                try
                {
                    var highestBid = currentAuction.GetHighestValidBid();
                    if (highestBid == default)
                    {
                        output($"Auction for {currentAuction.item.GetModifiedItemName()} is FINISHED! The item " +
                               $"will remain with @{currentAuction.itemOwner.FirstName}, as no bid met the reserve " +
                               $"price of {currentAuction.reservePrice}{Naming.Gold}.");
                        return;
                    }

                    if (!currentAuction.itemOwner.IsAdopted() || currentAuction.itemOwner.IsDead)
                    {
                        output($"Auction for {currentAuction.item.GetModifiedItemName()} is CANCELLED! " +
                               $"@{currentAuction.itemOwner.FirstName} retired or died.");
                        return;
                    }

                    if (!GetCustomItems(currentAuction.itemOwner).Any(i => i.IsEqualTo(currentAuction.item)))
                    {
                        output($"Auction for {currentAuction.item.GetModifiedItemName()} is CANCELLED! " +
                               $"@{currentAuction.itemOwner.FirstName} is no longer in possession of the item.");
                        return;
                    }

                    output($"Auction for {currentAuction.item.GetModifiedItemName()} is FINISHED! The item will " +
                           $"go to @{highestBid.hero.FirstName} for {highestBid.bid}{Naming.Gold}.");

                    TransferCustomItem(currentAuction.itemOwner, highestBid.hero, 
                        currentAuction.item, highestBid.bid);
                }
                finally
                {
                    currentAuction = null;
                }
            });
        }

        public void TransferCustomItem(Hero oldOwner, Hero newOwner, EquipmentElement item, int transferFee)
        {
            if (transferFee != 0)
            {
                ChangeHeroGold(newOwner, -transferFee, isSpending: true);
                ChangeHeroGold(oldOwner, transferFee);
            }

            RemoveCustomItem(oldOwner, item);
            AddCustomItem(newOwner, item);

            // Update the equipment of both, this should only modify the slots related to the custom item
            // (the gap in the previous owners equipment and optionally equipping the new item)
            EquipHero.UpgradeEquipment(oldOwner, GetEquipmentTier(oldOwner), oldOwner.GetClass(), replaceSameTier: false);
            EquipHero.UpgradeEquipment(newOwner, GetEquipmentTier(newOwner), newOwner.GetClass(), replaceSameTier: false);
        }
        
        public void DiscardCustomItem(Hero owner, EquipmentElement item)
        {
            RemoveCustomItem(owner, item);

            // Update equipment, this should only modify the slots related to the custom item
            EquipHero.UpgradeEquipment(owner, GetEquipmentTier(owner), owner.GetClass(), replaceSameTier: false);
        }

        public (bool success, string description) AuctionBid(Hero bidder, int bid)
        {
            return currentAuction?.Bid(bidder, bid) 
                   ?? (false, $"No auction in progress");
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

        [CategoryOrder("Limits", 1)]
        [CategoryOrder("Costs", 2)]
        [CategoryOrder("Troop Types", 3)]
        public class RetinueSettings : IDocumentable
        {
            [Category("Limits"), Description("Maximum number of units in the retinue. " +
                                            "Recommend less than 20, summons to NOT obey the games unit limits."), 
             PropertyOrder(1), UsedImplicitly]
            public int MaxRetinueSize { get; set; } = 5;

            [Category("Costs"), Description("Gold cost for Tier 1 retinue"), PropertyOrder(1), UsedImplicitly]
            public int CostTier1 { get; set; } = 25000;

            [Category("Costs"), Description("Gold cost for Tier 2 retinue"), PropertyOrder(2), UsedImplicitly]
            public int CostTier2 { get; set; } = 50000;

            [Category("Costs"), Description("Gold cost for Tier 3 retinue"), PropertyOrder(3), UsedImplicitly]
            public int CostTier3 { get; set; } = 100000;

            [Category("Costs"), Description("Gold cost for Tier 4 retinue"), PropertyOrder(4), UsedImplicitly]
            public int CostTier4 { get; set; } = 175000;

            [Category("Costs"), Description("Gold cost for Tier 5 retinue"), PropertyOrder(5), UsedImplicitly]
            public int CostTier5 { get; set; } = 275000;

            [Category("Costs"), Description("Gold cost for Tier 6 retinue"), PropertyOrder(6), UsedImplicitly]
            public int CostTier6 { get; set; } = 400000;

            // etc..
            public int GetTierCost(int tier)
            {
                return tier switch
                {
                    0 => CostTier1,
                    1 => CostTier2,
                    2 => CostTier3,
                    3 => CostTier4,
                    4 => CostTier5,
                    5 => CostTier6,
                    _ => CostTier6
                };
            }

            [Category("Troop Types"), 
             Description("Whether to use the adopted hero's culture (if not enabled then a random one is used)"),
             PropertyOrder(1), UsedImplicitly]
            public bool UseHeroesCultureUnits { get; set; } = true;

            [Category("Troop Types"), 
             Description("Whether to allow bandit units when UseHeroesCultureUnits is disabled"), 
             PropertyOrder(2), UsedImplicitly]
            public bool IncludeBanditUnits { get; set; }

            [Category("Troop Types"), Description("Whether to allow basic troops"), PropertyOrder(3), UsedImplicitly]
            public bool UseBasicTroops { get; set; } = true;

            [Category("Troop Types"), Description("Whether to allow elite troops"), PropertyOrder(4), UsedImplicitly]
            public bool UseEliteTroops { get; set; } = true;

            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                generator.PropertyValuePair("Max retinue", $"{MaxRetinueSize}");
                generator.PropertyValuePair("Tier costs", $"1={CostTier1}{Naming.Gold}, 2={CostTier2}{Naming.Gold}, 3={CostTier3}{Naming.Gold}, 4={CostTier4}{Naming.Gold}, 5={CostTier5}{Naming.Gold}, 5={CostTier5}{Naming.Gold}, 6={CostTier6}{Naming.Gold}");
                var allowed = new List<string>();
                if(UseHeroesCultureUnits) allowed.Add($"Same culture only");
                if(IncludeBanditUnits) allowed.Add($"Bandits");
                if(UseBasicTroops) allowed.Add($"Basic troops");
                if(UseEliteTroops) allowed.Add($"Elite troops");
                generator.PropertyValuePair("Allowed", string.Join(", ", allowed));
            }
        }

        public (bool success, string status) UpgradeRetinue(Hero hero, RetinueSettings settings)
        {
            // Somewhat based on RecruitmentCampaignBehavior.UpdateVolunteersOfNotables
            var heroRetinue = GetHeroData(hero).Retinue;
            
            // first fill in any missing ones
            if (heroRetinue.Count < settings.MaxRetinueSize)
            {
                var troopType = HeroHelpers.AllCultures
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

                int cost = settings.GetTierCost(0);
                int heroGold = GetHeroGold(hero);
                if (GetHeroGold(hero) < cost)
                {
                    return (false, Naming.NotEnoughGold(cost, heroGold));
                }
                ChangeHeroGold(hero, -cost, isSpending: true);
                heroRetinue.Add(new() { TroopType = troopType, Level = 1 });
                return (true, $"+{troopType} ({Naming.Dec}{cost}{Naming.Gold})");
            }

            // upgrade the lowest tier unit
            var retinueToUpgrade = heroRetinue
                .OrderBy(h => h.TroopType.Tier)
                .FirstOrDefault(t => t.TroopType.UpgradeTargets?.Any() == true);
            if (retinueToUpgrade != null)
            {
                int upgradeCost = settings.GetTierCost(retinueToUpgrade.Level);
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

        public static void SetAgentStartingHealth(Hero hero, Agent agent)
        {
            if (BLTAdoptAHeroModule.CommonConfig.StartWithFullHealth)
            {
                agent.Health = agent.HealthLimit;
            }

            bool inTournament = MissionHelpers.InTournament();
            float multiplier = inTournament
                ? BLTAdoptAHeroModule.TournamentConfig.StartHealthMultiplier
                : BLTAdoptAHeroModule.CommonConfig.StartHealthMultiplier;
            
            agent.BaseHealthLimit *= Math.Max(1, multiplier);
            agent.HealthLimit *= Math.Max(1, multiplier);
            agent.Health *= Math.Max(1, multiplier);
        }
        
        public static IEnumerable<Hero> GetAvailableHeroes(Func<Hero, bool> filter = null) =>
            HeroHelpers.AliveHeroes.Where(h =>
                    // Some buggy mods can result in null heroes
                    h != null &&
                    // Some buggy mods can result in heroes with out valid names
                    h.Name != null &&
                    // Not the player of course
                    h != Hero.MainHero
                    // Don't want notables ever
                    && !h.IsNotable && h.Age >= 18f)
                .Where(filter ?? (_ => true))
                .Where(n => !n.Name.Contains(BLTAdoptAHeroModule.Tag));

        public static IEnumerable<Hero> GetAllAdoptedHeroes() => HeroHelpers.AliveHeroes.Where(n => n.Name?.Contains(BLTAdoptAHeroModule.Tag) == true);

        public static string GetFullName(string name) => $"{name} {BLTAdoptAHeroModule.Tag}";

        public static void SetHeroAdoptedName(Hero hero, string userName)
        {
            HeroHelpers.SetHeroName(hero, new (GetFullName(userName)), new (userName));
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

            if (!suppressAutoRetire && hero.IsDead && !hd.IsRetiredOrDead)
            {
                RetireHero(hero);
            }

            return hd;
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

        #endregion
    }
}