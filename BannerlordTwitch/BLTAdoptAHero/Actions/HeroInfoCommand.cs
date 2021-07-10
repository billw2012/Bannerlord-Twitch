using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Actions.Util;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Will write various hero stats to chat")]
    internal class HeroInfoCommand : ICommandHandler
    {
        private class Settings : IDocumentable
        {
            [Description("Show gold"), PropertyOrder(1), UsedImplicitly]
            public bool ShowGold { get; set; } = true;
            [Description("Show personal info: health, location, age"), PropertyOrder(1), UsedImplicitly]
            public bool ShowGeneral { get; set; } = true;
            [Description("Shows skills (and focuse values) above the specified MinSkillToShow value"), PropertyOrder(2), UsedImplicitly]
            public bool ShowTopSkills { get; set; } = true;
            [Description("If ShowTopSkills is specified, this defines what skills are shown"), PropertyOrder(3), UsedImplicitly]
            public int MinSkillToShow { get; set; } = 100;
            [Description("Shows all hero attributes"), PropertyOrder(4), UsedImplicitly]
            public bool ShowAttributes { get; set; } = true;
            [Description("Shows the equipment tier of the hero"), PropertyOrder(5), UsedImplicitly]
            public bool ShowEquipment { get; set; }
            [Description("Shows the full battle inventory of the hero"), PropertyOrder(5), UsedImplicitly]
            public bool ShowInventory { get; set; }
            [Description("Shows the heroes storage (all their custom items)"), PropertyOrder(6), UsedImplicitly]
            public bool ShowStorage { get; set; }
            [Description("Shows the full civilian inventory of the hero"), PropertyOrder(7), UsedImplicitly]
            public bool ShowCivilianInventory { get; set; }
            [Description("Shows a summary of the retinue of the hero (count and tier)"), PropertyOrder(8), UsedImplicitly]
            public bool ShowRetinue { get; set; }
            [Description("Shows the exact classes and counts of the retinue of the hero"), PropertyOrder(9), UsedImplicitly]
            public bool ShowRetinueList { get; set; }
            
            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                var shows = new List<string>();
                if(ShowGold) shows.Add($"Gold");
                if(ShowGeneral) shows.Add($"Age, Clan, Culture, Health");
                if(ShowTopSkills) shows.Add($"Skills greater than {MinSkillToShow}");
                if(ShowAttributes) shows.Add($"Attributes");
                if(ShowEquipment) shows.Add($"Equipment tier");
                if(ShowInventory) shows.Add($"Battle equipment inventory");
                if(ShowCivilianInventory) shows.Add($"Civilian equipment inventory");
                if(ShowStorage) shows.Add($"Custom item storage");
                if(ShowRetinue) shows.Add($"Retinue count and average tier");
                if(ShowRetinueList) shows.Add($"Retinue unit list");
                generator.P($"Shows: {string.Join(", ", shows)}");
            }
        }
        
        // One Handed, Two Handed, Polearm, Bow, Crossbow, Throwing, Riding, Athletics, Smithing
        // Scouting, Tactics, Roguery, Charm, Leadership, Trade, Steward, Medicine, Engineering

        Type ICommandHandler.HandlerConfigType => typeof(Settings);
        void ICommandHandler.Execute(ReplyContext context, object config)
        {
            var settings = config as Settings ?? new Settings();
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            var infoStrings = new List<string>{};
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
                    infoStrings.Add($"{cl?.Name ?? "No Class"}");
                    if (adoptedHero.Clan != null)
                    {
                        infoStrings.Add($"Clan {adoptedHero.Clan.Name}");
                    }
                    infoStrings.Add($"{adoptedHero.Culture}");
                    infoStrings.Add($"{adoptedHero.Age:0} yrs");
                    infoStrings.Add($"{adoptedHero.HitPoints} / {adoptedHero.CharacterObject.MaxHitPoints()} HP");
                    if (adoptedHero.LastSeenPlace != null)
                    {
                        infoStrings.Add($"Last seen near {adoptedHero.LastSeenPlace.Name}");
                    }
                }
                
                if (settings.ShowTopSkills)
                {
                    infoStrings.Add($"Lvl {adoptedHero.Level}");
                    infoStrings.Add("Skills " + string.Join("■", 
                        HeroHelpers.AllSkillObjects
                            .Where(s => adoptedHero.GetSkillValue(s) >= settings.MinSkillToShow)
                            .OrderByDescending(s => adoptedHero.GetSkillValue(s))
                            .Select(skill => $"{SkillXP.GetShortSkillName(skill)} {adoptedHero.GetSkillValue(skill)} " +
                                             $"[f{adoptedHero.HeroDeveloper.GetFocus(skill)}]")
                    ));
                }
                
                if (settings.ShowAttributes)
                {
                    infoStrings.Add("Attr " + string.Join("■", HeroHelpers.AllAttributes
                        .Select(a => $"{HeroHelpers.GetShortAttributeName(a)} {adoptedHero.GetAttributeValue(a)}")));
                }
                
                if (settings.ShowEquipment)
                {
                    infoStrings.Add($"Equip Tier {BLTAdoptAHeroCampaignBehavior.Current.GetEquipmentTier(adoptedHero) + 1}");
                    var cl = BLTAdoptAHeroCampaignBehavior.Current.GetEquipmentClass(adoptedHero);
                    infoStrings.Add($"{cl?.Name ?? "No Equip Class"}");
                }

                if (settings.ShowInventory)
                {
                    infoStrings.Add("Battle " + string.Join("■", adoptedHero.BattleEquipment
                        .YieldFilledEquipmentSlots()
                        .Select(e => $"{e.GetModifiedItemName()}")
                    ));
                }

                if(settings.ShowCivilianInventory)
                {
                    infoStrings.Add("Civ " + string.Join("■", adoptedHero.CivilianEquipment
                        .YieldFilledEquipmentSlots()
                        .Select(e => $"{e.GetModifiedItemName()}")
                    ));
                }
                
                if (settings.ShowStorage)
                {
                    var customItems = BLTAdoptAHeroCampaignBehavior.Current.GetCustomItems(adoptedHero);
                    infoStrings.Add("Stored " + string.Join("■", customItems
                        .Select(e => e.GetModifiedItemName())
                    ));
                }

                if (settings.ShowRetinue)
                {
                    var retinue = BLTAdoptAHeroCampaignBehavior.Current.GetRetinue(adoptedHero).ToList();
                    if (retinue.Count > 0)
                    {
                        double tier = retinue.Average(r => r.Tier);
                        infoStrings.Add($"Retinue {retinue.Count} (avg Tier {tier:0.#})");
                    }
                    else
                    {
                        infoStrings.Add($"Retinue None");
                    }
                }

                if (settings.ShowRetinueList)
                {
                    var retinue = BLTAdoptAHeroCampaignBehavior.Current.GetRetinue(adoptedHero).GroupBy(r => r).ToList();
                    foreach (var r in retinue.OrderBy(r => r.Key.Tier))
                    {
                        infoStrings.Add(r.Count() > 1 ? $"{r.Key.Name} x {r.Count()}" : $"{r.Key.Name}");
                    }
                }
            }
            ActionManager.SendReply(context, infoStrings.ToArray());
        }
    }
}