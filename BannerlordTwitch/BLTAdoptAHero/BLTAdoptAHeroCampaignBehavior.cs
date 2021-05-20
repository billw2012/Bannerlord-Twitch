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
using Newtonsoft.Json;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    public class BLTAdoptAHeroCampaignBehavior : CampaignBehaviorBase
    {
        public static BLTAdoptAHeroCampaignBehavior Get() => GetCampaignBehavior<BLTAdoptAHeroCampaignBehavior>();

        private class HeroData
        {
            [SaveableProperty(0)]
            public int Gold { get; set; }

            public class RetinueData
            {
                [SaveableProperty(0)]
                public CharacterObject TroopType { get; set; }
            }
            [SaveableProperty(1)]
            public List<RetinueData> Retinue { get; set; } = new();
        }
        private Dictionary<Hero, HeroData> heroData = new();
        
        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsLoading)
            {
                Dictionary<Hero, int> heroGold = null;
                //dataStore.SyncData("AdoptedHeroes", ref adoptedHeroes);
                dataStore.SyncData("HeroGold", ref heroGold);
                dataStore.SyncDataAsJson("HeroData", ref heroData);
                if (heroGold != null)
                {
                    heroData = new Dictionary<Hero, HeroData>();

                    // Upgrade code
                    foreach ((var hero, int gold) in heroGold)
                    {
                        heroData.Add(hero, new HeroData
                        {
                            Gold = gold
                        });
                    }
                }
            }
            else
            {
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

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, () =>
            {
                // Clean up legacy hero names
                var heroes = GetAllBLTHeroes().GroupBy(h => h.Name.ToLower());
                foreach (var heroGroup in heroes)
                {
                    // hero to keep is living and preferably lowercase
                    var heroToKeep = heroGroup.FirstOrDefault(h => h.IsAlive && h.FirstName == h.FirstName.ToLower()) 
                                     ?? heroGroup.FirstOrDefault(h => h.IsAlive);

                    // all other heroes with the same name, living or dead are made lowercase, and have the tags removed
                    foreach(var otherOnes in heroGroup.Where(h => h != heroToKeep))
                    {
                        // Removing the tag and lower casing the name for neatness
                        otherOnes.FirstName = otherOnes.FirstName.ToLower();
                        otherOnes.Name = otherOnes.FirstName.ToLower();
                        Campaign.Current.EncyclopediaManager.BookmarksTracker.RemoveBookmarkFromItem(otherOnes);
                    }
                    if (heroToKeep != null)
                    {
                        heroToKeep.FirstName = heroToKeep.FirstName.ToLower();
                        heroToKeep.Name = new TextObject(GetFullName(heroToKeep.FirstName.ToString()));
                    }
                }
                
                // Clean up hero data
                int randomSeed = Environment.TickCount;
                foreach (var (hero, data) in heroData)
                {
                    // Remove invalid troop types
                    data.Retinue.RemoveAll(r => r.TroopType == null);
                    
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
        public int GetHeroGold(Hero hero)
        {
            return GetHeroData(hero).Gold;
        }

        public void SetHeroGold(Hero hero, int gold)
        {
            GetHeroData(hero).Gold = gold;
        }
        
        public int ChangeHeroGold(Hero hero, int change)
        {
            int newGold = Math.Max(0, change + GetHeroGold(hero));
            GetHeroData(hero).Gold = newGold;
            return newGold;
        }
        #endregion

        #region Retinue
        public IEnumerable<CharacterObject> GetRetinue(Hero hero) => GetHeroData(hero).Retinue.Select(r => r.TroopType);

        public class RetinueSettings
        {
            [Description("Maximum number of units in the retinue"), PropertyOrder(1)]
            public int MaxRetinueSize { get; set; }
            [Description("Cost to buy a new unit, or upgrade one"), PropertyOrder(2)]
            public int CostPerTier { get; set; }
            [Description("Additional cost per tier multiplier (final cost for an upgrade is Cost Per Tier + Cost Per Tier x Current Tier x Cost Scaling Per Tier"), PropertyOrder(3)]
            public float CostScalingPerTier { get; set; }
            [Description("Whether to use the adopted hero's culture (if not enabled then a random one is used)"), PropertyOrder(4)]
            public bool UseHeroesCultureUnits { get; set; }
            [Description("Whether to allow bandit units when UseHeroesCultureUnits is disabled"), PropertyOrder(4)]
            public bool IncludeBanditUnits { get; set; }
            [Description("Whether to use elite troops (if not enabled then basic troops are used)"), PropertyOrder(5)]
            public bool UseEliteTroops { get; set; }
        }
        
        public (bool success, string status) UpgradeRetinue(Hero hero, RetinueSettings settings)
        {
            // Somewhat based on RecruitmentCampaignBehavior.UpdateVolunteersOfNotables
            
            var heroRetinue = GetHeroData(hero).Retinue;
            
            var culture = settings.UseHeroesCultureUnits
                ? hero.Culture
                : MBObjectManager.Instance.GetObjectTypeList<CultureObject>()
                    .Where(c => settings.IncludeBanditUnits || c.IsMainCulture)
                    .SelectRandom();
            var troopType = settings.UseEliteTroops ? culture.EliteBasicTroop : culture.BasicTroop;
            
            // first fill in any missing ones
            if (heroRetinue.Count < settings.MaxRetinueSize)
            {
                int heroGold = GetHeroGold(hero);
                if (GetHeroGold(hero) < settings.CostPerTier)
                {
                    return (false, $"You need {settings.CostPerTier} gold, you have {heroGold}");
                }
                ChangeHeroGold(hero, -settings.CostPerTier);
                heroRetinue.Add(new HeroData.RetinueData { TroopType = troopType });
                return (true, $"{troopType} added to your retinue (-{settings.CostPerTier} gold)");
            }

            // upgrade the lowest tier unit
            var retinueToUpgrade = heroRetinue
                .OrderBy(h => h.TroopType.Tier)
                .FirstOrDefault(t => t.TroopType.UpgradeTargets?.Any() == true);
            if (retinueToUpgrade != null)
            {
                int upgradeCost = (int) (settings.CostPerTier + settings.CostPerTier * (retinueToUpgrade.TroopType.Tier - troopType.Tier + 1) * settings.CostScalingPerTier);
                int heroGold = GetHeroGold(hero);
                if (GetHeroGold(hero) < upgradeCost)
                {
                    return (false, $"You need {upgradeCost} gold, you have {heroGold}");
                }
                ChangeHeroGold(hero, -upgradeCost);

                var oldTroopType = retinueToUpgrade.TroopType;
                retinueToUpgrade.TroopType = oldTroopType.UpgradeTargets.SelectRandom();
                return (true, $"{oldTroopType} was upgraded to {retinueToUpgrade.TroopType} (-{upgradeCost} gold)");
            }
            return (false, $"Can't upgrade retinue any further!");
        }
        #endregion

        #region Helper Functions
        public static IEnumerable<Hero> GetAvailableHeroes(Func<Hero, bool> filter = null)
        {
            var tagText = new TextObject(BLTAdoptAHeroModule.Tag);
            return Campaign.Current?.AliveHeroes?.Where(h =>
                // Not the player of course
                h != Hero.MainHero
                // Don't want notables ever
                && !h.IsNotable && h.Age >= 18f)
                .Where(filter ?? (_ => true))
                .Where(n => !n.Name.Contains(tagText));
        }
        
        public static IEnumerable<Hero> GetAllBLTHeroes()
        {
            var tagText = new TextObject(BLTAdoptAHeroModule.Tag);
            return Hero.All.Where(n => n.Name.Contains(tagText));
        }

        public static string GetFullName(string name) => $"{name} {BLTAdoptAHeroModule.Tag}";

        public static Hero GetAdoptedHero(string name)
        {
            return Campaign.Current?
                .AliveHeroes?
                .FirstOrDefault(h => h.Name?.Contains(BLTAdoptAHeroModule.Tag) == true 
                                     && h.FirstName?.Contains(name) == true 
                                     && h.FirstName?.ToString() == name);
        }
        #endregion
    }
}