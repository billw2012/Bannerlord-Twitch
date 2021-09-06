using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Achievements;
using BLTAdoptAHero.Actions.Util;
using BLTAdoptAHero.Powers;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=cj68a0l9}Show Hero Info"),
     LocDescription("{=QsTQzceq}Will write various hero stats to chat"),
     UsedImplicitly]
    internal class HeroInfoCommand : ICommandHandler
    {
        private class Settings : IDocumentable
        {
            [LocDisplayName("{=oWtshHwx}Show Gold"),
             LocDescription("{=O2mUbcue}Show gold"), 
             PropertyOrder(1), UsedImplicitly]
            public bool ShowGold { get; set; } = true;
            [LocDisplayName("{=VmQECrnc}Show General"),
             LocDescription("{=JWbk5Oko}Show personal info: health, location, age"), 
             PropertyOrder(1), UsedImplicitly]
            public bool ShowGeneral { get; set; } = true;
            [LocDisplayName("{=tMKmlYeR}Show Top Skills"),
             LocDescription("{=dayx7JEJ}Shows skills (and focuse values) above the specified MinSkillToShow value"), 
             PropertyOrder(2), UsedImplicitly]
            public bool ShowTopSkills { get; set; } = true;
            [LocDisplayName("{=5VW8HXxS}Min Skill To Show"),
             LocDescription("{=4819Fxyv}If ShowTopSkills is specified, this defines what skills are shown"), 
             PropertyOrder(3), UsedImplicitly]
            public int MinSkillToShow { get; set; } = 100;
            [LocDisplayName("{=lSM7JkvB}Show Attributes"),
             LocDescription("{=co5TLkOw}Shows all hero attributes"), 
             PropertyOrder(4), UsedImplicitly]
            public bool ShowAttributes { get; set; } = true;
            [LocDisplayName("{=uLUxOyp2}Show Equipment"),
             LocDescription("{=CnTMaEPC}Shows the equipment tier of the hero"), 
             PropertyOrder(5), UsedImplicitly]
            public bool ShowEquipment { get; set; }
            [LocDisplayName("{=sp1iuH1y}Show Inventory"),
             LocDescription("{=uhAC3hOZ}Shows the full battle inventory of the hero"), 
             PropertyOrder(5), UsedImplicitly]
            public bool ShowInventory { get; set; }
            [LocDisplayName("{=aRA1V1Jp}Show Storage"),
             LocDescription("{=Wjr33ERJ}Shows the heroes storage (all their custom items)"), 
             PropertyOrder(6), UsedImplicitly]
            public bool ShowStorage { get; set; }
            [LocDisplayName("{=ecvBeN44}Show Civilian Inventory"),
             LocDescription("{=fUggutW6}Shows the full civilian inventory of the hero"), 
             PropertyOrder(7), UsedImplicitly]
            public bool ShowCivilianInventory { get; set; }
            [LocDisplayName("{=p0WEhay8}Show Retinue"),
             LocDescription("{=AXnWeTzh}Shows a summary of the retinue of the hero (count and tier)"), 
             PropertyOrder(8), UsedImplicitly]
            public bool ShowRetinue { get; set; }
            [LocDisplayName("{=Vyatyyuh}Show Retinue List"),
             LocDescription("{=CSTqzcOi}Shows the exact classes and counts of the retinue of the hero"), 
             PropertyOrder(9), UsedImplicitly]
            public bool ShowRetinueList { get; set; }
            [LocDisplayName("{=1F3utCWA}Show Achievements"),
             LocDescription("{=CKJd7KC9}Shows all hero achievements"), 
             PropertyOrder(10), UsedImplicitly]
            public bool ShowAchievements { get; set; }
            [LocDisplayName("{=LNvYSMZj}Show Tracked Stats"),
             LocDescription("{=FaUbhgTR}Shows all hero tracked stats (kills, deaths, summons, attacks, tournament wins etc.)"), 
             PropertyOrder(11), UsedImplicitly]
            public bool ShowTrackedStats { get; set; }
            [LocDisplayName("{=cHaiwygJ}Show Powers"),
             LocDescription("{=YW8HsNEF}Shows the heroes unlocked powers"), 
             PropertyOrder(12), UsedImplicitly]
            public bool ShowPowers { get; set; }

            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                var shows = new List<string>();
                if(ShowGold) shows.Add("{=YVhcZatv}Gold".Translate());
                if(ShowGeneral) shows.Add("{=jZigiKXG}Age, Clan, Culture, Health".Translate());
                if(ShowTopSkills) shows.Add("{=6wVce2iI}Skills greater than {MinSkillToShow}".Translate());
                if(ShowAttributes) shows.Add("{=74kcLopo}Attributes".Translate());
                if(ShowEquipment) shows.Add("{=PeDxGcu7}Equipment tier".Translate());
                if(ShowInventory) shows.Add("{=EVvlMCru}Battle equipment inventory".Translate());
                if(ShowCivilianInventory) shows.Add("{=DeffOla6}Civilian equipment inventory".Translate());
                if(ShowStorage) shows.Add("{=VSDDQdmJ}Custom item storage".Translate());
                if(ShowRetinue) shows.Add("{=C0mkGXlK}Retinue count and average tier".Translate());
                if(ShowRetinueList) shows.Add("{=L4Rh6vFE}Retinue unit list".Translate());
                if(ShowAchievements) shows.Add("{=ZW9XlwY7}Achievements".Translate());
                if(ShowTrackedStats) shows.Add("{=Xmo7pOpj}Tracked stats".Translate());
                if(ShowPowers) shows.Add("{=xVDOsWPq}Powers".Translate());
                generator.PropertyValuePair("{=UB1bAtSI}Shows".Translate(), string.Join(", ", shows));
            }
        }
        
        // One Handed, Two Handed, Polearm, Bow, Crossbow, Throwing, Riding, Athletics, Smithing
        // Scouting, Tactics, Roguery, Charm, Leadership, Trade, Steward, Medicine, Engineering

        Type ICommandHandler.HandlerConfigType => typeof(Settings);
        void ICommandHandler.Execute(ReplyContext context, object config)
        {
            var settings = config as Settings ?? new Settings();
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            var infoStrings = new List<string>();
            if (adoptedHero == null)
            {
                infoStrings.Add(AdoptAHero.NoHeroMessage);
            }
            else
            {
                if (settings.ShowGold)
                {
                    int gold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(adoptedHero);
                    infoStrings.Add($"{gold}{Naming.Gold}");
                }
                
                if (settings.ShowGeneral)
                {
                    var cl = BLTAdoptAHeroCampaignBehavior.Current.GetClass(adoptedHero);
                    infoStrings.Add($"{cl?.Name ?? "{=ZI2UKbNp}No Class".Translate()}");
                    if (adoptedHero.Clan != null)
                    {
                        infoStrings.Add($"Clan {adoptedHero.Clan.Name}");
                    }
                    infoStrings.Add($"{adoptedHero.Culture}");
                    infoStrings.Add("{=4TVRrlOw}{Age} yrs".Translate(("Age", (int)Math.Ceiling(adoptedHero.Age))));
                    infoStrings.Add("{=jY2QJdA3}{HP} / {MaxHP} HP".Translate(
                        ("HP", adoptedHero.HitPoints), ("MaxHP", adoptedHero.CharacterObject.MaxHitPoints())));
                    if (adoptedHero.LastSeenPlace != null)
                    {
                        infoStrings.Add("{=B2xDasDx}Last seen near {Place}"
                            .Translate(("Place", adoptedHero.LastSeenPlace.Name)));
                    }
                }
                
                if (settings.ShowTopSkills)
                {
                    infoStrings.Add("{=fRwyY6ms}[LVL]".Translate() +
                                    $" {adoptedHero.Level}");
                    infoStrings.Add("{=rTId8pBy}[SKILLS]".Translate() +
                                    " " + string.Join(Naming.Sep, 
                        CampaignHelpers.AllSkillObjects
                            .Where(s => adoptedHero.GetSkillValue(s) >= settings.MinSkillToShow)
                            .OrderByDescending(s => adoptedHero.GetSkillValue(s))
                            .Select(skill => $"{SkillXP.GetShortSkillName(skill)} {adoptedHero.GetSkillValue(skill)} " +
                                             $"[f{adoptedHero.HeroDeveloper.GetFocus(skill)}]")
                    ));
                }
                
                if (settings.ShowAttributes)
                {
                    infoStrings.Add("{=RSlhbJzO}[ATTR]".Translate() +
                                    " " + string.Join(Naming.Sep, CampaignHelpers.AllAttributes
                        .Select(a 
                            => $"{CampaignHelpers.GetShortAttributeName(a)} {adoptedHero.GetAttributeValue(a)}")));
                }
                
                if (settings.ShowEquipment)
                {
                    infoStrings.Add(
                        "{=64yw2YD0}[TIER]".Translate() +
                        $" {BLTAdoptAHeroCampaignBehavior.Current.GetEquipmentTier(adoptedHero) + 1}");
                    var cl = BLTAdoptAHeroCampaignBehavior.Current.GetEquipmentClass(adoptedHero);
                    infoStrings.Add(cl?.Name.ToString() ?? "{=u32KRqz8}No Equip Class".Translate());
                }

                if (settings.ShowInventory)
                {
                    infoStrings.Add("{=YVVlcDSK}[BATTLE]".Translate() +
                                    " " + string.Join(Naming.Sep, adoptedHero.BattleEquipment
                        .YieldFilledEquipmentSlots().Select(e => e.element)
                        .Select(e => $"{e.GetModifiedItemName()}")
                    ));
                }

                if(settings.ShowCivilianInventory)
                {
                    infoStrings.Add("{=zaVtcDWB}[CIV]".Translate() +
                                    " " + string.Join(Naming.Sep, adoptedHero.CivilianEquipment
                        .YieldFilledEquipmentSlots().Select(e => e.element)
                        .Select(e => $"{e.GetModifiedItemName()}")
                    ));
                }
                
                if (settings.ShowStorage)
                {
                    var customItems 
                        = BLTAdoptAHeroCampaignBehavior.Current.GetCustomItems(adoptedHero);
                    infoStrings.Add("{=U4HnxTCj}[STORED]".Translate() +
                                    " " + (customItems.Any() ? string.Join(Naming.Sep, customItems
                        .Select(e => e.GetModifiedItemName())) : "{=4IOefqsW}(nothing)".Translate()));
                }

                if (settings.ShowRetinue)
                {
                    var retinue
                        = BLTAdoptAHeroCampaignBehavior.Current.GetRetinue(adoptedHero).ToList();
                    if (retinue.Count > 0)
                    {
                        double tier = retinue.Average(r => r.Tier);
                        infoStrings.Add("{=hMBF1zLr}[RETINUE]".Translate() + " " +
                                        "{RetinueCount} (avg Tier {Tier})".Translate(
                                            ("RetinueCount", retinue.Count),
                                            ("Tier", tier.ToString("0.#"))));
                    }
                    else
                    {
                        infoStrings.Add("{=hMBF1zLr}[RETINUE]".Translate() + " " +
                                        "{=FNK3LD2p}None".Translate());
                    }
                }

                if (settings.ShowRetinueList)
                {
                    var retinue = BLTAdoptAHeroCampaignBehavior.Current
                        .GetRetinue(adoptedHero)
                        .GroupBy(r => r)
                        .OrderBy(r => r.Key.Tier)
                        .ToList();
                    foreach (var r in retinue)
                    {
                        infoStrings.Add(r.Count() > 1 ? $"{r.Key.Name} x {r.Count()}" : $"{r.Key.Name}");
                    }
                }
                
                if (settings.ShowAchievements)
                {
                    var achievements = BLTAdoptAHeroCampaignBehavior.Current
                        .GetAchievements(adoptedHero)
                        .ToList();
                    if (achievements.Any())
                    {
                        infoStrings.Add("{=giS3vq1V}[ACHIEV]".Translate() +
                                        $" " + string.Join(Naming.Sep, achievements
                            .Select(e => e.Name)));
                    }
                    else
                    {
                        infoStrings.Add("{=giS3vq1V}[ACHIEV]".Translate() + $" " +
                                        "{=FNK3LD2p}None".Translate());
                    }
                }

                if (settings.ShowTrackedStats)
                {
                    var achievementList = new List<(string shortName, AchievementStatsData.Statistic id)>
                    {
                        ("{=ADjhFwlz}K".Translate(), AchievementStatsData.Statistic.TotalKills),
                        ("{=aUj96cVC}D".Translate(), AchievementStatsData.Statistic.TotalDeaths),
                        ("{=i02EMVP8}KVwr".Translate(), AchievementStatsData.Statistic.TotalViewerKills),
                        ("{=iGXwhVja}KStrmr".Translate(), AchievementStatsData.Statistic.TotalStreamerKills),
                        ("{=APUC6wGt}Battles".Translate(), AchievementStatsData.Statistic.Battles),
                        ("{=6nwK1UF9}Sums".Translate(), AchievementStatsData.Statistic.Summons),
                        ("{=uDFykstd}CSums".Translate(), AchievementStatsData.Statistic.ConsecutiveSummons),
                        ("{=wtmfNCIj}Atks".Translate(), AchievementStatsData.Statistic.Attacks),
                        ("{=T29akAtY}CAtks".Translate(), AchievementStatsData.Statistic.ConsecutiveAttacks),
                        ("{=NOkWgftX}TourRndW".Translate(), AchievementStatsData.Statistic.TotalTournamentRoundWins),
                        ("{=k5O3x52V}TourRndL".Translate(), AchievementStatsData.Statistic.TotalTournamentRoundLosses),
                        ("{=J6yoXowD}TourW".Translate(), AchievementStatsData.Statistic.TotalTournamentFinalWins),
                    };
                    infoStrings.Add(
                        "{=nL2E16fj}[STATS]".Translate() 
                        + " " + string.Join(Naming.Sep,
                            achievementList.Select(a =>
                                $"{a.shortName}:" +
                                $"{BLTAdoptAHeroCampaignBehavior.Current.GetAchievementTotalStat(adoptedHero, a.id)}" +
                                $"({BLTAdoptAHeroCampaignBehavior.Current.GetAchievementClassStat(adoptedHero, a.id)})"
                        )));
                }

                if (settings.ShowPowers)
                {
                    var heroClass = adoptedHero.GetClass();
                    if(heroClass != null)
                    {
                        var activePowers 
                            = heroClass.ActivePower.GetUnlockedPowers(adoptedHero).OfType<HeroPowerDefBase>();
                        infoStrings.Add("{=gV1s8Ffw}[ACTIVE]".Translate() +
                                        " " + 
                                        string.Join(Naming.Sep, activePowers.Select(p => p.Name)));
                        
                        var passivePowers 
                            = heroClass.PassivePower.GetUnlockedPowers(adoptedHero).OfType<HeroPowerDefBase>();
                        infoStrings.Add("{=z82jxnmF}[PASSIVE]".Translate() +
                                        " " + 
                                        string.Join(Naming.Sep, passivePowers.Select(p => p.Name)));
                    }
                }
            }
            ActionManager.SendReply(context, infoStrings.ToArray());
        }
    }
}