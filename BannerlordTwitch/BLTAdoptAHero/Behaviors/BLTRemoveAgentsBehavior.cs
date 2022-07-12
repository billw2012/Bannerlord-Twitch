﻿using System.Collections.Generic;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace BLTAdoptAHero
{
    internal class BLTRemoveAgentsBehavior : AutoMissionBehavior<BLTRemoveAgentsBehavior>
    {
        private readonly List<Hero> heroesAdded = new();
 
        public void Add(Hero hero)
        {
            heroesAdded.Add(hero);
        }

        private void RemoveHeroes()
        {
            foreach (var hero in heroesAdded)
            {
                Log.Trace($"[SummonHero] Removing hero {hero}");
                LocationComplex.Current?.RemoveCharacterIfExists(hero);
                if (CampaignMission.Current?.Location?.ContainsCharacter(hero) == true)
                    CampaignMission.Current.Location.RemoveCharacter(hero);
            }

            heroesAdded.Clear();
        }

        protected override void OnEndMission()
        {
            SafeCall(RemoveHeroes);
        }

        public override void OnMissionDeactivate()
        {
            SafeCall(RemoveHeroes);
        }

        public override void OnMissionRestart()
        {
            SafeCall(RemoveHeroes);
        }
    }
}