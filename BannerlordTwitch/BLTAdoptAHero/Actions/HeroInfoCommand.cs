using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [UsedImplicitly]
    [Description("Will write various hero stats to chat")]
    internal class HeroInfoCommand : ICommandHandler
    {
        private class Settings
        {
            [Description("Show gold"), PropertyOrder(1)]
            public bool ShowGold { get; set; } = true;
            [Description("Show personal info: health, location, age"), PropertyOrder(1)]
            public bool ShowGeneral { get; set; } = true;
            [Description("Shows skills (and focuse values) above the specified MinSkillToShow value"), PropertyOrder(2)]
            public bool ShowTopSkills { get; set; } = true;
            [Description("If ShowTopSkills is specified, this defines what skills are shown"), PropertyOrder(3)]
            public int MinSkillToShow { get; set; } = 100;
            [Description("Shows all hero attributes"), PropertyOrder(4)]
            public bool ShowAttributes { get; set; } = true;
            [Description("Shows the equipment tier of the hero"), PropertyOrder(5)]
            public bool ShowEquipment { get; set; }
            [Description("Shows the full battle and civilian inventory of the hero"), PropertyOrder(5)]
            public bool ShowInventory { get; set; }
            [Description("Shows the retinue of the hero"), PropertyOrder(6)]
            public bool ShowRetinue { get; set; }
        }
        
        // One Handed, Two Handed, Polearm, Bow, Crossbow, Throwing, Riding, Athletics, Smithing
        // Scouting, Tactics, Roguery, Charm, Leadership, Trade, Steward, Medicine, Engineering

        Type ICommandHandler.HandlerConfigType => typeof(Settings);
        void ICommandHandler.Execute(ReplyContext context, object config)
        {
            var settings = config as Settings ?? new Settings();
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.GetAdoptedHero(context.UserName);
            var infoStrings = new List<string>{};
            if (adoptedHero == null)
            {
                infoStrings.Add(Campaign.Current == null ? AdoptAHero.NotStartedMessage : AdoptAHero.NoHeroMessage);
            }
            else
            {
                if (settings.ShowGold)
                {
                    int gold = BLTAdoptAHeroCampaignBehavior.Get().GetHeroGold(adoptedHero);
                    infoStrings.Add($"{gold}{Naming.Gold}");
                }
                if (settings.ShowGeneral)
                {
                    var cl = BLTAdoptAHeroCampaignBehavior.Get().GetClass(adoptedHero);
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
                        SkillObject.All
                            .Where(s => adoptedHero.GetSkillValue(s) >= settings.MinSkillToShow)
                            .OrderByDescending(s => adoptedHero.GetSkillValue(s))
                            .Select(skill => $"{SkillXP.GetShortSkillName(skill)} {adoptedHero.GetSkillValue(skill)} " +
                                             $"[f{adoptedHero.HeroDeveloper.GetFocus(skill)}]")
                    ));
                }
                if (settings.ShowAttributes)
                {
                    infoStrings.Add("Attr " + string.Join("■", AdoptAHero.CharAttributes
                        .Select(a => $"{a.shortName} {adoptedHero.GetAttributeValue(a.val)}")));
                }
                if (settings.ShowEquipment)
                {
                    infoStrings.Add($"Equip Tier {BLTAdoptAHeroCampaignBehavior.Get().GetEquipmentTier(adoptedHero) + 1}");
                    var cl = BLTAdoptAHeroCampaignBehavior.Get().GetEquipmentClass(adoptedHero);
                    infoStrings.Add($"{cl?.Name ?? "No Equip Class"}");
                }
                if(settings.ShowInventory)
                {
                    infoStrings.Add("Battle " + string.Join("■", adoptedHero.BattleEquipment
                        .YieldEquipmentSlots()
                        .Where(e => !e.element.IsEmpty)
                        .Select(e => $"{e.element.Item.Name}")
                    ));
                    infoStrings.Add("Civ " + string.Join("■", adoptedHero.CivilianEquipment
                        .YieldEquipmentSlots()
                        .Where(e => !e.element.IsEmpty)
                        .Select(e => $"{e.element.Item.Name}")
                    ));
                }
                if (settings.ShowRetinue)
                {
                    var retinue = BLTAdoptAHeroCampaignBehavior.Get().GetRetinue(adoptedHero).ToList();
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
            }
            ActionManager.SendReply(context, infoStrings.ToArray());
        }
    }
}