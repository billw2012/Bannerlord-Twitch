using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;

namespace BLTAdoptAHero
{
    public class BLTAdoptAHeroCampaignBehavior : CampaignBehaviorBase
    {
        public static BLTAdoptAHeroCampaignBehavior Get() => GetCampaignBehavior<BLTAdoptAHeroCampaignBehavior>();
        
        //private Dictionary<string, Hero> nameMap = new();
        //private Dictionary<Hero, string> reverseNameMap = new();
        
        //private Dictionary<string, Hero> adoptedHeroes = new();
        private Dictionary<Hero, int> heroGold = new();
        
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
            
        public override void SyncData(IDataStore dataStore)
        {
            //dataStore.SyncData("AdoptedHeroes", ref adoptedHeroes);
            dataStore.SyncData("HeroGold", ref heroGold);
        }

        public int GetHeroGold(Hero hero)
        {
            // Better create it now
            heroGold ??= new Dictionary<Hero, int>();
            if (!heroGold.TryGetValue(hero, out int gold))
            {
                heroGold.Add(hero, hero.Gold);
                return hero.Gold;
            }

            return gold;
        }

        public void SetHeroGold(Hero hero, int gold)
        {
            heroGold[hero] = gold;
        }
        
        public int ChangeHeroGold(Hero hero, int change)
        {
            int newGold = Math.Max(0, change + GetHeroGold(hero));
            heroGold[hero] = newGold;
            return newGold;
        }
        
        //private

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
            var tagObject = new TextObject(BLTAdoptAHeroModule.Tag);
            var nameObject = new TextObject(name);
            return Campaign.Current?
                .AliveHeroes?
                .FirstOrDefault(h => h.Name?.Contains(tagObject) == true 
                                     && h.FirstName?.Contains(nameObject) == true 
                                     && h.FirstName?.ToString() == name);
        }
    }
}