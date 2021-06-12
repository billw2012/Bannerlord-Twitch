using System;
using System.Collections.Generic;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;

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
            try
            {
                RemoveHeroes();
            }
            catch (Exception ex)
            {
                Log.Exception($"{nameof(BLTRemoveAgentsBehavior)}.{nameof(OnEndMission)}", ex);
            }
        }

        public override void OnMissionDeactivate()
        {
            try
            {
                RemoveHeroes();
            }
            catch (Exception ex)
            {
                Log.Exception($"{nameof(BLTRemoveAgentsBehavior)}.{nameof(OnMissionDeactivate)}", ex);
            }
        }

        public override void OnMissionRestart()
        {
            try
            {
                RemoveHeroes();
            }
            catch (Exception ex)
            {
                Log.Exception($"{nameof(BLTRemoveAgentsBehavior)}.{nameof(OnMissionRestart)}", ex);
            }
        }
    }
}