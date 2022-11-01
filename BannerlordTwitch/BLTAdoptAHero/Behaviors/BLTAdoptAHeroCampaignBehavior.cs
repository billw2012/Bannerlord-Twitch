using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.ObjectSystem;
using BannerlordTwitch;
using BannerlordTwitch.Annotations;
using BannerlordTwitch.SaveSystem;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Achievements;
using Newtonsoft.Json;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
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
                public CharacterObject TroopType { get; set; }
                public int Level { get; set; }
                public int SavedTroopIndex { get; set; }
            }
            public int Gold { get; set; }
            [UsedImplicitly]
            public List<RetinueData> Retinue { get; set; } = new();
            public int SpentGold { get; set; }
            public int EquipmentTier { get; set; } = -2;
            public Guid EquipmentClassID { get; set; }
            public Guid ClassID { get; set; }
            public string Owner { get; set; }
            public int Iteration { get; set; }
            public bool IsRetiredOrDead { get; set; }
            [UsedImplicitly]
            public AchievementStatsData AchievementStats { get; set; } = new();
            
            public class SavedEquipment
            {
                public ItemObject Item { get; set; }
                [UsedImplicitly]
                public string ItemModifierId { get; set; }
                public int ItemSaveIndex { get; set; }
                
                [UsedImplicitly]
                public SavedEquipment() {}
            
                public SavedEquipment(EquipmentElement element)
                {
                    Item = element.Item;
                    ItemModifierId = element.ItemModifier.StringId;
                }
            
                public static explicit operator EquipmentElement(SavedEquipment m) 
                    => new(m.Item, MBObjectManager.Instance.GetObject<ItemModifier>(m.ItemModifierId));
            }

            [UsedImplicitly]
            public List<SavedEquipment> SavedCustomItems { get; set; } = new();
            
            [JsonIgnore]
            public List<EquipmentElement> CustomItems { get; set; } = new();

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
                foreach (var hero in CampaignHelpers.AllHeroes.Where(h => h.IsAdopted()))
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
                            "{=hoRPbRrb}Compensated @{HeroName} with {GoldAmount}{GoldSymbol} for {CustomItemsCount} invalid custom items"
                                .Translate(
                                    ("HeroName", hero.Name),
                                    ("GoldAmount", removedCustomItems * 50000),
                                    ("GoldSymbol", Naming.Gold),
                                    ("CustomItemsCount", removedCustomItems)));
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
                foreach (var (hero, _) in heroData.Where(h => h.Key.IsDead && !h.Value.IsRetiredOrDead))
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
                        Log.LogFeedEvent("{=PCPU0lPX}@{VictimName} {Verb} by @{KillerName}!"
                            .Translate(
                                ("VictimName", victim.Name),
                                ("Verb", verb),
                                ("KillerName", killer.Name)));
                    }
                    else if (killer != null)
                    {
                        Log.LogFeedEvent("{=Bji2ULge}@{KillerName} {Verb}!"
                            .Translate(
                                ("KillerName", killer.Name),
                                ("Verb", verb)));
                    }
                }
            });
            
            CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, (hero, _) =>
            {
                if (hero.IsAdopted())
                    Log.LogFeedEvent("{=8aTmTvl8}@{HeroName} is now level {Level}!"
                        .Translate(("HeroName", hero.Name), ("Level", hero.Level)));
            });
            
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, (party, hero) =>
            {
                if (hero.IsAdopted())
                {
                    if(party != null)
                        Log.LogFeedEvent("{=4PqVnFWY}@{HeroName} was taken prisoner by {PartyName}!"
                            .Translate(("HeroName", hero.Name), ("PartyName", party.Name)));
                    else
                        Log.LogFeedEvent("{=WeRWLpKn}@{HeroName} was taken prisoner!"
                            .Translate(("HeroName", hero.Name)));
                }
            });
            
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, (hero, party, _, _) =>
            {
                if (hero.IsAdopted())
                {
                    if(party != null)
                        Log.LogFeedEvent("{=tQANBoTK}@{HeroName} is no longer a prisoner of {PartyName}!"
                            .Translate(("HeroName", hero.Name), ("PartyName", party.Name)));
                    else
                        Log.LogFeedEvent("{=MQslOwr0}@{HeroName} is no longer a prisoner!"
                            .Translate(("HeroName", hero.Name)));
                }
            });
            
            CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, (hero, clan) =>
            {
                if(hero.IsAdopted())
                    Log.LogFeedEvent("{=SUdnIyfw}@{HeroName} moved from {FromClanName} to {ToClanName}!"
                        .Translate(
                            ("HeroName", hero.Name),
                            ("FromClanName", clan?.Name.ToString() ?? "no clan"),
                            ("ToClanName", hero.Clan?.Name.ToString() ?? "no clan")));
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
            hd.Iteration = GetAncestors(userName).Max(a => (int?)a.Iteration + 1) ?? 0;
            SetHeroAdoptedName(newHero, userName);
        }

        public Hero GetAdoptedHero(string name)
        {
            var foundHero = heroData.FirstOrDefault(h 
                    => !h.Value.IsRetiredOrDead
                       && (string.Equals(h.Key.FirstName?.Raw(), name, StringComparison.CurrentCultureIgnoreCase) 
                           || string.Equals(h.Value.Owner, name, StringComparison.CurrentCultureIgnoreCase)))
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
            
            string desc = hero.IsDead ? "{=ZtZL0lbX}deceased".Translate() : "{=ISrFBorj}retired".Translate();
            var oldName = hero.Name;
            CampaignHelpers.SetHeroName(hero, new (hero.FirstName + $" {ToRoman(data.Iteration + 1)} ({desc})"));
            CampaignHelpers.RemoveEncyclopediaBookmarkFromItem(hero);
            
            // Don't leave retired heroes in the tournament queue 
            BLTTournamentQueueBehavior.Current.RemoveFromQueue(hero);
            
            Log.LogFeedEvent("{=2PHPNmuv}{OldName} is {RetireType}!"
                .Translate(("OldName", oldName), ("RetireType", desc)));
            Log.Info("{=wzpkEmTL}Dead or retired hero {OldName} renamed to {HeroName}"
                .Translate(("OldName", oldName), ("HeroName", hero.Name)));

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
            var ancestors = GetAncestors(inheritor.FirstName.ToString());
            int inheritance = (int) (ancestors.Sum(a => a.SpentGold + a.Gold) * amount);
            ChangeHeroGold(inheritor, inheritance);
            foreach (var data in ancestors)
            {
                data.SpentGold = 0;
                data.Gold = 0;
            }
            return inheritance;
        }

        private List<HeroData> GetAncestors(string name) =>
            heroData
                .Where(h 
                    => h.Value.IsRetiredOrDead
                       && string.Equals(h.Value.Owner, name, StringComparison.CurrentCultureIgnoreCase))
                .Select(kv => kv.Value)
                .OrderBy(d => d.Iteration)
                .ToList();

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
            
            var newAchievements = BLTAdoptAHeroModule.CommonConfig.ValidAchievements?
                .Where(a => a.IsAchieved(hero))
                .Where(a
                    => !achievementData.Achievements.Contains(a.ID)) ?? Enumerable.Empty<AchievementDef>();

            foreach (var achievement in newAchievements)
            {
                if (!LocString.IsNullOrEmpty(achievement.NotificationText))
                {
                    string message = achievement.NotificationText.ToString(
                        ("{viewer}", hero.FirstName.ToString()),
                        ("{player}", hero.FirstName.ToString()),
                        ("{name}", achievement.Name));
                    Log.ShowInformation(message, hero.CharacterObject,
                        BLTAdoptAHeroModule.CommonConfig.KillStreakPopupAlertSound);
                }

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
                    return (false, "{=2cbVbW91}You can't bid on your own item".Translate());
                }
                
                if (bid < reservePrice)
                {
                    return (false, "{=rbhzuJLm}Bid of {Bid}{GoldIcon} does not meet reserve price of {ReservePrice}{GoldIcon}" 
                        .Translate(
                            ("Bid", bid),
                            ("GoldIcon", Naming.Gold),
                            ("ReservePrice", reservePrice)
                            ));
                }

                if (bids.Values.Any(v => v == bid))
                {
                    return (false, "{=83uZcndH}Another bid at {Bid}{GoldIcon} already exists" 
                        .Translate(
                            ("Bid", bid),
                            ("GoldIcon", Naming.Gold)
                            ));
                }
                
                if (bids.TryGetValue(bidder, out int currBid) && currBid >= bid)
                {
                    return (false, "{=qeLF80xw}You already bid more ({Bid}{GoldIcon}), you can only raise your bid" 
                        .Translate(
                            ("Bid", bid),
                            ("GoldIcon", Naming.Gold)
                        ));
                }

                int bidderGold = Current.GetHeroGold(bidder);
                if (bidderGold < bid)
                {
                    return (false, "{=Cqi0iYNR}You cannot cover a bid of {Bid}{GoldIcon}, you only have {BidderGold}{GoldIcon}" 
                        .Translate(
                            ("Bid", bid),
                            ("GoldIcon", Naming.Gold),
                            ("BidderGold", bidderGold)
                        ));
                }

                bids[bidder] = bid;

                return (true, "{=M2B9yQ4w}Bid of {Bid}{GoldIcon} placed!" 
                    .Translate(
                        ("Bid", bid),
                        ("GoldIcon", Naming.Gold)
                    ));
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
                        output("{=TeeDJyJ1}{Time} seconds left in auction of '{ItemName}', high bid is {HighestBid}{GoldIcon} (@{HighestBidderName})"
                            .Translate(
                                ("Time", seconds),
                                ("ItemName", item.GetModifiedItemName()),
                                ("HighestBid", highestBid.bid),
                                ("GoldIcon", Naming.Gold),
                                ("HighestBidderName", highestBid.hero.FirstName)));
                    }
                    else
                    {
                        output("{=jNkGaKZw}{Time} seconds left in auction of '{ItemName}', no bids placed"
                            .Translate(
                                ("Time", seconds),
                                ("ItemName", item.GetModifiedItemName())));
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
                        output("{=d9ooHVPU}Auction for {ItemName} is FINISHED! The item will remain with @{ItemOwnerName}, as no bid met the reserve price of {ReservePrice}{GoldIcon}."
                            .Translate(
                                ("ItemName", currentAuction.item.GetModifiedItemName()),
                                ("ItemOwnerName", currentAuction.itemOwner.FirstName),
                                ("ReservePrice", currentAuction.reservePrice),
                                ("GoldIcon", Naming.Gold)));
                        return;
                    }

                    if (!currentAuction.itemOwner.IsAdopted() || currentAuction.itemOwner.IsDead)
                    {
                        output("{=SGuTRcui}Auction for {ItemName} is CANCELLED! @{ItemOwnerName} retired or died."
                            .Translate(
                                ("ItemName", currentAuction.item.GetModifiedItemName()),
                                ("ItemOwnerName", currentAuction.itemOwner.FirstName)));
                        return;
                    }

                    if (!GetCustomItems(currentAuction.itemOwner).Any(i => i.IsEqualTo(currentAuction.item)))
                    {
                        output("{=NRV4IstE}Auction for {ItemName} is CANCELLED! @{ItemOwnerName} is no longer in possession of the item."
                            .Translate(
                                ("ItemName", currentAuction.item.GetModifiedItemName()),
                                ("ItemOwnerName", currentAuction.itemOwner.FirstName)));
                        return;
                    }

                    output("{=jmbMoHta}Auction for {ItemName} is FINISHED! The item will go to @{HighestBidderName} for {HighestBid}{GoldIcon}."
                        .Translate(
                            ("ItemName", currentAuction.item.GetModifiedItemName()),
                            ("HighestBidderName", highestBid.hero.FirstName),
                            ("HighestBid", highestBid.bid),
                            ("GoldIcon", Naming.Gold)));

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
                   ?? (false, "{=Cy38Ckpk}No auction in progress".Translate());
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

        [CategoryOrder("Limits", 1),
         CategoryOrder("Costs", 2),
         CategoryOrder("Troop Types", 3)]
        public class RetinueSettings : IDocumentable
        {
            [LocDisplayName("{=wAGE7h6U}Max Retinue Size"),
             LocCategory("Limits", "{=1lHWj3nT}Limits"), 
             LocDescription("{=EOGB8EWN}Maximum number of units in the retinue. Recommend less than 20, summons to NOT obey the games unit limits."), 
             PropertyOrder(1), UsedImplicitly]
            public int MaxRetinueSize { get; set; } = 5;

            [LocDisplayName("{=VvdtvdQJ}Cost Tier 1"),
             LocCategory("Costs", "{=r7sc3Tvg}Costs"), 
             LocDescription("{=9bvn5R5A}Gold cost for Tier 1 retinue"), 
             PropertyOrder(1), UsedImplicitly]
            public int CostTier1 { get; set; } = 25000;

            [LocDisplayName("{=engRDMZx}Cost Tier 2"),
             LocCategory("Costs", "{=r7sc3Tvg}Costs"), 
             LocDescription("{=pD2ZvRVH}Gold cost for Tier 2 retinue"), 
             PropertyOrder(2), UsedImplicitly]
            public int CostTier2 { get; set; } = 50000;

            [LocDisplayName("{=3jxmITht}Cost Tier 3"),
             LocCategory("Costs", "{=r7sc3Tvg}Costs"), 
             LocDescription("{=dyB8loLF}Gold cost for Tier 3 retinue"), 
             PropertyOrder(3), UsedImplicitly]
            public int CostTier3 { get; set; } = 100000;

            [LocDisplayName("{=dhwd4ccF}Cost Tier 4"),
             LocCategory("Costs", "{=r7sc3Tvg}Costs"), 
             LocDescription("{=aji2HxKa}Gold cost for Tier 4 retinue"), 
             PropertyOrder(4), UsedImplicitly]
            public int CostTier4 { get; set; } = 175000;

            [LocDisplayName("{=zJkb4AIh}Cost Tier 5"),
             LocCategory("Costs", "{=r7sc3Tvg}Costs"), 
             LocDescription("{=fnEOFst7}Gold cost for Tier 5 retinue"), 
             PropertyOrder(5), UsedImplicitly]
            public int CostTier5 { get; set; } = 275000;

            [LocDisplayName("{=1hh3cOJO}Cost Tier 6"),
             LocCategory("Costs", "{=r7sc3Tvg}Costs"), 
             LocDescription("{=PieENBSG}Gold cost for Tier 6 retinue"), 
             PropertyOrder(6), UsedImplicitly]
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

            [LocDisplayName("{=q1Rkm3Rq}Use Heroes Culture Units"),
             LocCategory("Troop Types", "{=qYhM3gcn}Troop Types"), 
             LocDescription("{=9qAD6eZR}Whether to use the adopted hero's culture (if not enabled then a random one is used)"),
             PropertyOrder(1), UsedImplicitly]
            public bool UseHeroesCultureUnits { get; set; } = true;

            [LocDisplayName("{=dbU7WEKG}Include Bandit Units"),
             LocCategory("Troop Types", "{=qYhM3gcn}Troop Types"), 
             LocDescription("{=06KnYhyh}Whether to allow bandit units when UseHeroesCultureUnits is disabled"), 
             PropertyOrder(2), UsedImplicitly]
            public bool IncludeBanditUnits { get; set; }

            [LocDisplayName("{=E2RBmb1K}Use Basic Troops"),
             LocCategory("Troop Types", "{=qYhM3gcn}Troop Types"), 
             LocDescription("{=uPwaOKdT}Whether to allow basic troops"), 
             PropertyOrder(3), UsedImplicitly]
            public bool UseBasicTroops { get; set; } = true;

            [LocDisplayName("{=lnz7d1BI}Use Elite Troops"),
             LocCategory("Troop Types", "{=qYhM3gcn}Troop Types"), 
             LocDescription("{=EPr2clqT}Whether to allow elite troops"), 
             PropertyOrder(4), UsedImplicitly]
            public bool UseEliteTroops { get; set; } = true;

            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                generator.PropertyValuePair("{=UhUpH8C8}Max retinue".Translate(), $"{MaxRetinueSize}");
                generator.PropertyValuePair("{=VBuncBq5}Tier costs".Translate(), $"1={CostTier1}{Naming.Gold}, 2={CostTier2}{Naming.Gold}, 3={CostTier3}{Naming.Gold}, 4={CostTier4}{Naming.Gold}, 5={CostTier5}{Naming.Gold}, 5={CostTier5}{Naming.Gold}, 6={CostTier6}{Naming.Gold}");
                var allowed = new List<string>();
                if(UseHeroesCultureUnits) allowed.Add("{=R7rU0TbD}Same culture only".Translate());
                if(IncludeBanditUnits) allowed.Add("{=c2qOsXvs}Bandits".Translate());
                if(UseBasicTroops) allowed.Add("{=RmTwEFzy}Basic troops".Translate());
                if(UseEliteTroops) allowed.Add("{=3gumlthG}Elite troops".Translate());
                generator.PropertyValuePair("{=uL7MfYPc}Allowed".Translate(), string.Join(", ", allowed));
            }
        }

        public (bool success, string status) UpgradeRetinue(Hero hero, RetinueSettings settings, int maxToUpgrade)
        {
            var availableTroops = CampaignHelpers.AllCultures
                .Where(c => settings.IncludeBanditUnits || c.IsMainCulture)
                .SelectMany(c =>
                {
                    var troopTypes = new List<CharacterObject>();
                    if (settings.UseBasicTroops && c.BasicTroop != null) troopTypes.Add(c.BasicTroop);
                    if (settings.UseEliteTroops && c.EliteBasicTroop != null) troopTypes.Add(c.EliteBasicTroop);
                    return troopTypes;
                })
                // At least 2 upgrade tiers available
                .Where(c => c.UpgradeTargets?.FirstOrDefault()?.UpgradeTargets?.Any() == true)
                .ToList();

            if (!availableTroops.Any())
            {
                return (false, "{=bBCyH0vV}No valid troop types could be found, please check out settings".Translate());
            }
            
            var heroRetinue = GetHeroData(hero).Retinue;

            var retinueChanges = new Dictionary<HeroData.RetinueData, (CharacterObject oldTroopType, int totalSpent)>();

            int heroGold = GetHeroGold(hero);
            int totalCost = 0;

            var results = new List<string>();
            
            while (maxToUpgrade-- > 0)
            {
                // first fill in any missing ones
                if (heroRetinue.Count < settings.MaxRetinueSize)
                {
                    var troopType = availableTroops
                        .Shuffle()
                        // Sort same culture units to the front if required, but still include other units in-case the hero
                        // culture doesn't contain the requires units
                        .OrderBy(c => settings.UseHeroesCultureUnits && c.Culture != hero.Culture)
                        .FirstOrDefault();
                    
                    int cost = settings.GetTierCost(0);
                    if (totalCost + cost > heroGold)
                    {
                        results.Add(retinueChanges.IsEmpty()
                            ? Naming.NotEnoughGold(cost, heroGold)
                            : "{=zcbOq6Tb}Spent {TotalCost}{GoldIcon}, {RemainingGold}{GoldIcon} remaining"
                                .Translate(
                                    ("TotalCost", totalCost),
                                    ("GoldIcon", Naming.Gold),
                                    ("RemainingGold", heroGold - totalCost)));
                        break;
                    }
                    totalCost += cost;

                    var retinue = new HeroData.RetinueData { TroopType = troopType, Level = 1 };
                    heroRetinue.Add(retinue);
                    retinueChanges.Add(retinue, (null, cost));
                }
                else
                {
                    // upgrade the lowest tier unit
                    var retinueToUpgrade = heroRetinue
                        .OrderBy(h => h.TroopType.Tier)
                        .FirstOrDefault(t => t.TroopType.UpgradeTargets?.Any() == true);

                    if (retinueToUpgrade != null)
                    {
                        int cost = settings.GetTierCost(retinueToUpgrade.Level);
                        if (totalCost + cost > heroGold)
                        {
                            results.Add(retinueChanges.IsEmpty()
                                ? Naming.NotEnoughGold(cost, heroGold)
                                : "{=zcbOq6Tb}Spent {TotalCost}{GoldIcon}, {RemainingGold}{GoldIcon} remaining"
                                    .Translate(
                                        ("TotalCost", totalCost),
                                        ("GoldIcon", Naming.Gold),
                                        ("RemainingGold", heroGold - totalCost)));
                            break;
                        }

                        totalCost += cost;

                        var oldTroopType = retinueToUpgrade.TroopType;
                        retinueToUpgrade.TroopType = oldTroopType.UpgradeTargets.SelectRandom();
                        retinueToUpgrade.Level++;
                        if (retinueChanges.TryGetValue(retinueToUpgrade, out var upgradeRecord))
                        {
                            retinueChanges[retinueToUpgrade] =
                                (upgradeRecord.oldTroopType ?? oldTroopType, upgradeRecord.totalSpent + cost);
                        }
                        else
                        {
                            retinueChanges.Add(retinueToUpgrade, (oldTroopType, cost));
                        }
                    }
                    else
                    {
                        results.Add("{=PQRLJ04i}Can't upgrade retinue any further!".Translate());
                        break;
                    }
                }
            }

            var troopUpgradeSummary = new List<string>();
            foreach ((var oldTroopType, var newTroopType, int cost, int num) in retinueChanges
                .GroupBy(r 
                    => (r.Value.oldTroopType, newTroopType: r.Key.TroopType))
                .Select(g => (
                        g.Key.oldTroopType, 
                        g.Key.newTroopType, 
                        cost: g.Sum(f => f.Value.totalSpent), 
                        num: g.Count()))
                .OrderBy(g => g.oldTroopType == null)
                .ThenBy(g => g.num)
            )
            {
                if (oldTroopType != null)
                {
                    troopUpgradeSummary.Add($"{oldTroopType}{Naming.To}{newTroopType}" +
                                            (num > 1 ? $" x{num}" : "") +
                                            $" ({Naming.Dec}{cost}{Naming.Gold})");
                }
                else
                {
                    troopUpgradeSummary.Add($"{newTroopType}" +
                                            (num > 1 ? $" x{num}" : "") +
                                            $" ({Naming.Dec}{cost}{Naming.Gold})");

                }
            }

            if (totalCost > 0)
            {
                ChangeHeroGold(hero, -totalCost, isSpending: true);
            }
            
            return (retinueChanges.Any(), Naming.JoinList(troopUpgradeSummary.Concat(results)));
        }

        public void KillRetinue(Hero retinueOwnerHero, BasicCharacterObject retinueCharacterObject)
        {
            var heroRetinue = GetHeroData(retinueOwnerHero).Retinue;
            var matchingRetinue = heroRetinue.FirstOrDefault(r => r.TroopType == retinueCharacterObject);
            if (matchingRetinue != null)
            {
                heroRetinue.Remove(matchingRetinue);
            }
            else
            {
                Log.Error($"Couldn't find matching retinue type {retinueCharacterObject} " +
                          $"for {retinueOwnerHero} to remove");
            }
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
            CampaignHelpers.AliveHeroes.Where(h =>
                    // Some buggy mods can result in null heroes
                    h != null &&
                    // Some buggy mods can result in heroes with out valid names
                    h.Name != null &&
                    // Not the player of course
                    h != Hero.MainHero
                    // Don't want notables ever
                    && !h.IsNotable
                    // Only of age characters can be used
                    && h.Age >= Campaign.Current.Models.AgeModel.HeroComesOfAge)
                .Where(filter ?? (_ => true))
                .Where(n => !n.Name.Contains(BLTAdoptAHeroModule.Tag));

        public static IEnumerable<Hero> GetAllAdoptedHeroes() => CampaignHelpers.AliveHeroes.Where(n => n.Name?.Contains(BLTAdoptAHeroModule.Tag) == true);

        public static string GetFullName(string name) => $"{name} {BLTAdoptAHeroModule.Tag}";

        public static void SetHeroAdoptedName(Hero hero, string userName)
        {
            CampaignHelpers.SetHeroName(hero, new (GetFullName(userName)), new (userName));
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
                    return "{=LhHul2lV}was murdered".Translate();
                case KillCharacterAction.KillCharacterActionDetail.DiedInLabor:
                    return "{=HwjR45XN}died in labor".Translate();
                case KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge:
                    return "{=5GOjfkzW}died of old age".Translate();
                case KillCharacterAction.KillCharacterActionDetail.DiedInBattle:
                    return "{=ZKrgqWav}died in battle".Translate();
                case KillCharacterAction.KillCharacterActionDetail.WoundedInBattle:
                    return "{=jp4sldTL}was wounded in battle".Translate();
                case KillCharacterAction.KillCharacterActionDetail.Executed:
                    return "{=SkFFXsI1}was executed".Translate();
                case KillCharacterAction.KillCharacterActionDetail.Lost:
                    return "{=HMHdXDaK}was lost".Translate();
                default:
                case KillCharacterAction.KillCharacterActionDetail.None:
                    return "{=lrOJnThZ}was ended".Translate();
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

        #region Console Commands

        [CommandLineFunctionality.CommandLineArgumentFunction("addstat", "blt")]
        [UsedImplicitly]
        public static string SetHeroStat(List<string> strings)
        {
            var parts = string.Join(" ", strings).Split(',').Select(p => p.Trim()).ToList();

            if (parts.Count != 3)
            {
                return "Arguments: hero,stat,amount";
            }
            
            var hero = Current.GetAdoptedHero(parts[0]);
            if (hero == null)
            {
                return $"Couldn't find hero {parts[0]}";
            }

            if (!Enum.TryParse(parts[1], out AchievementStatsData.Statistic stat))
            {
                return $"Couldn't find stat {parts[1]}";
            }

            if (!int.TryParse(parts[2], out int amount))
            {
                return $"Couldn't parse amount {parts[2]}";
            }
            
            Current.GetHeroData(hero).AchievementStats.UpdateValue(stat, hero.GetClass()?.ID ?? Guid.Empty, amount);

            return $"Added {amount} to {stat} stat of {hero.Name}";
        }

        #endregion
    }
}