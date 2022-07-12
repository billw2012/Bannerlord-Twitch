﻿using System;
using System.Linq;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    public static class HeroExtensions
    {
        public static bool IsAdopted(this Hero hero) => hero.FirstName != null && hero.Name?.Contains(BLTAdoptAHeroModule.Tag) == true;
        public static bool IsAdoptedBy(this Hero hero, string user) 
            => hero.IsAdopted()
            && string.Equals(hero.FirstName?.Raw(), user, StringComparison.CurrentCultureIgnoreCase);

        public static HeroClassDef GetClass(this Hero hero) 
            => BLTAdoptAHeroCampaignBehavior.Current.GetClass(hero);
        
        public static Agent GetAgent(this Hero hero)
            => Mission.Current?.Agents?.FirstOrDefault(a => a.Character == hero.CharacterObject);
        
        public static PartyBase GetMapEventParty(this Hero hero)
        {
            return (PartyBase.MainParty.MapEvent?.InvolvedParties ?? PartyBase.MainParty.SiegeEvent?.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege))?
                .FirstOrDefault(p => p.MemberRoster.GetTroopCount(hero.CharacterObject) != 0);
        }
    }
}