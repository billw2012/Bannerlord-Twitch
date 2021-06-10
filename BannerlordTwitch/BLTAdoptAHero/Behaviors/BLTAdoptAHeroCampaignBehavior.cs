using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Bannerlord.ButterLib.Common.Extensions;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using Bannerlord.ButterLib.SaveSystem.Extensions;
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
        public static BLTAdoptAHeroCampaignBehavior Get() => GetCampaignBehavior<BLTAdoptAHeroCampaignBehavior>();

        private class HeroData
        {
            public class RetinueData
            {
                [SaveableProperty(0)]
                public CharacterObject TroopType { get; set; }
                [SaveableProperty(1)]
                public int Level { get; set; }
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
        }

        private Dictionary<Hero, HeroData> heroData = new();
        
        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsLoading)
            {
                Dictionary<Hero, int> heroGold = null;
                dataStore.SyncData("HeroGold", ref heroGold);
                dataStore.SyncDataAsJson("HeroData", ref heroData);

                // Upgrade code
                if (heroGold != null)
                {
                    Log.Trace($"Converting HeroGold to HeroData");
                    heroData = new Dictionary<Hero, HeroData>();

                    foreach ((var hero, int gold) in heroGold)
                    {
                        heroData.Add(hero, new HeroData
                        {
                            Gold = gold
                        });
                    }
                }

                foreach (var (hero, data) in heroData)
                {
                    // Try and find an appropriate character to replace the missing retinue with
                    foreach (var r in data.Retinue.Where(r => r.TroopType == null))
                    {
                        r.TroopType = hero.Culture.EliteBasicTroop?.UpgradeTargets?.SelectRandom()?.UpgradeTargets?.SelectRandom();
                    }

                    // Remove any we couldn't replace
                    int count = data.Retinue.RemoveAll(r => r.TroopType == null);
                    // Compensate with gold for each one lost
                    data.Gold += count * 50000;

                    // Update EquipmentTier if it isn't set
                    if (data.EquipmentTier == -2)
                    {
                        data.EquipmentTier = EquipHero.GetHeroEquipmentTier(hero);
                    }
                }
            }
            else
            {
                // Need to explicitly write out the CharacterObjects so that they are referenced at least once in the primary object index
                var usedCharList = heroData.Values.SelectMany(h => h.Retinue.Select(r => r.TroopType)).Distinct().ToList();
                dataStore.SyncData("UsedCharacterObjectList", ref usedCharList);
                // Do the same for heroes, just in case! Shouldn't be necessary as Heroes MUST exist elsewhere in the save or they wouldn't load...
                var usedHeroList = heroData.Keys.ToList();
                dataStore.SyncData("UsedHeroObjectList", ref usedHeroList);
                
                dataStore.SyncDataAsJson("HeroData", ref heroData);
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

        // public static string GetHeroName(Hero hero)
        // {
        //     if(!reverseNameMap.TryGetValue(h))
        // }

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

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, () =>
            {
                // Clean up hero data
                int randomSeed = Environment.TickCount;
                foreach (var (hero, data) in heroData)
                {
                    // Remove invalid troop types
                    data.Retinue.RemoveAll(r => r.TroopType == null);
                    
                    // Ensure Level is set
                    foreach (var r in data.Retinue.Where(r => r.Level == 0))
                    {
                        var rootUnit = CharacterHelper.FindUpgradeRootOf(r.TroopType);
                        r.Level = rootUnit != null 
                            ? r.TroopType.Tier - rootUnit.Tier + 1 
                            : 1;
                    }

                    // Make sure heroes are active, and in real locations
                    if(hero.HeroState is Hero.CharacterStates.NotSpawned && hero.CurrentSettlement == null)
                    {
                        // Activate them and put them in a random town
                        hero.ChangeState(Hero.CharacterStates.Active);
                        var targetSettlement = Settlement.All.Where(s => s.IsTown).SelectRandom(++randomSeed);
                        EnterSettlementAction.ApplyForCharacterOnly(hero, targetSettlement);
                        Log.Info($"Placed unspawned hero {hero.Name} at {targetSettlement.Name}");
                    }                
                }
                
                // Clean up dead character names
                foreach (var deadHero in Campaign.Current.DeadAndDisabledHeroes
                    .Where(h => h.IsDead && h.Name?.Contains(BLTAdoptAHeroModule.Tag) == true))
                {
                    RetireHero(deadHero);
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
                    Log.LogFeedEvent($"{hero.Name} is now a member of {clan?.Name.ToString() ?? "no clan"}!");
            });
            
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, JoinTournament.SetupGameMenus);
            
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, mission =>
            {
                if (mission is not Mission actualMission)
                    return;
                actualMission.AddMissionBehaviour(new BLTAdoptAHeroCommonMissionBehavior());
            });
        }

        public static string ToRoman(int number)
        {
            return number switch
            {
                < 0 => throw new ArgumentOutOfRangeException("insert value betwheen 1 and 3999"),
                > 3999 => throw new ArgumentOutOfRangeException("insert value betwheen 1 and 3999"),
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
        
        public static void RetireHero(Hero hero)
        {
            string heroName = hero.FirstName?.Raw().ToLower();
            // Retired heroes
            int count = Campaign.Current.Heroes.Count(h
                => h.FirstName?.Raw().ToLower() == heroName 
                   && h.Name?.Contains(BLTAdoptAHeroModule.Tag) == false);
            var oldName = hero.Name;
            hero.Name = new TextObject(hero.FirstName + $" {ToRoman(count + 1)} ({(hero.IsDead ? "deceased" : "retired")})");
            Campaign.Current.EncyclopediaManager.BookmarksTracker.RemoveBookmarkFromItem(hero);
            Log.Info($"Dead or retired hero {oldName} renamed to {hero.Name}");
        }

        private HeroData GetHeroData(Hero hero)
        {
            // Better create it now if it doesn't exist
            if (!heroData.TryGetValue(hero, out var hd))
            {
                hd = new HeroData { Gold = hero.Gold };
                heroData.Add(hero, hd);
            }
            return hd;
        }

        #region Gold
        public int GetHeroGold(Hero hero) => GetHeroData(hero).Gold;

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

        #region Equipment
        public int GetEquipmentTier(Hero hero) => GetHeroData(hero).EquipmentTier;
        public void SetEquipmentTier(Hero hero, int tier) => GetHeroData(hero).EquipmentTier = tier;
        public HeroClassDef GetEquipmentClass(Hero hero) => BLTAdoptAHeroModule.HeroClassConfig.GetClass(GetHeroData(hero).EquipmentClassID);
        public void SetEquipmentClass(Hero hero, HeroClassDef classDef) => GetHeroData(hero).EquipmentClassID = classDef?.ID ?? Guid.Empty;
        #endregion

        #region Class
        public HeroClassDef GetClass(Hero hero) => BLTAdoptAHeroModule.HeroClassConfig.GetClass(GetHeroData(hero).ClassID);

        public void SetClass(Hero hero, HeroClassDef classDef) => GetHeroData(hero).ClassID = classDef?.ID ?? Guid.Empty;
        #endregion

        #region Retinue
        public IEnumerable<CharacterObject> GetRetinue(Hero hero) => GetHeroData(hero).Retinue.Select(r => r.TroopType);

        public class RetinueSettings
        {
            [Description("Maximum number of units in the retinue. Recommend less than 20, summons to NOT obey the games unit limits."), PropertyOrder(1)]
            public int MaxRetinueSize { get; set; } = 5;

            [Description("Cost to buy a new unit, or upgrade one"), PropertyOrder(2)]
            public int CostPerTier { get; set; } = 50000;

            [Description(
                 "Additional cost per tier multiplier (final cost for an upgrade is Cost Per Tier + Cost Per Tier x Current Tier x Cost Scaling Per Tier"),
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
            Campaign.Current?.AliveHeroes?.Where(h =>
                    // Not the player of course
                    h != Hero.MainHero
                    // Don't want notables ever
                    && !h.IsNotable && h.Age >= 18f)
                .Where(filter ?? (_ => true))
                .Where(n => !n.Name.Contains(BLTAdoptAHeroModule.Tag));

        public static IEnumerable<Hero> GetAllAdoptedHeroes() => Hero.All.Where(n => n.Name.Contains(BLTAdoptAHeroModule.Tag));

        public static string GetFullName(string name) => $"{name} {BLTAdoptAHeroModule.Tag}";

        public static Hero GetDeadHero(string name)
        {
            string nameToFind = name.ToLower();
            return Campaign.Current?
                .DeadAndDisabledHeroes?
                .FirstOrDefault(h => h.IsDead
                                     && h.Name?.Contains(BLTAdoptAHeroModule.Tag) == true
                                     && h.FirstName?.Raw().ToLower() == nameToFind);
        }

        public static void SetHeroAdoptedName(Hero hero, string userName)
        {
            hero.FirstName = new TextObject(userName);
            hero.Name = new TextObject(GetFullName(userName));
        }
        
        public static Hero GetAdoptedHero(string name)
        {
            string nameToFind = name.ToLower();
            var foundHero = Campaign.Current?
                .AliveHeroes?
                .FirstOrDefault(h => h.Name?.Contains(BLTAdoptAHeroModule.Tag) == true
                                     && h.FirstName?.Raw().ToLower() == nameToFind);

            // correct the name to match the viewer name casing
            if (foundHero != null && foundHero.FirstName?.Raw() != name)
            {
                SetHeroAdoptedName(foundHero, name);
            }

            return foundHero;
        }

        #endregion
    }
}