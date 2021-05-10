using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
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
            [Description("Show general info: gold, health, location, age"), PropertyOrder(1)]
            public bool ShowGeneral { get; set; }
            [Description("Shows skills (and focuse values) above the specified MinSkillToShow value"), PropertyOrder(2)]
            public bool ShowTopSkills { get; set; }
            [Description("If ShowTopSkills is specified, this defines what skills are shown"), PropertyOrder(3)]
            public int MinSkillToShow { get; set; }
            [Description("Shows all hero attributes"), PropertyOrder(4)]
            public bool ShowAttributes { get; set; }
            [Description("Shows the battle and civilian equipment of the hero"), PropertyOrder(5)]
            public bool ShowEquipment { get; set; }
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
                if (settings.ShowGeneral)
                {
                    if (adoptedHero.Clan != null)
                    {
                        infoStrings.Add($"Clan {adoptedHero.Clan.Name}");
                    }
                    infoStrings.Add($"{adoptedHero.Culture}");
                    int gold = BLTAdoptAHeroCampaignBehavior.Get().GetHeroGold(adoptedHero);
                    infoStrings.Add($"{gold} gold");
                    infoStrings.Add($"{adoptedHero.Age:0} yrs");
                    infoStrings.Add($"{adoptedHero.HitPoints} / {adoptedHero.CharacterObject.MaxHitPoints()} HP");
                    if (adoptedHero.LastSeenPlace != null)
                    {
                        infoStrings.Add($"Last seen near {adoptedHero.LastSeenPlace.Name}");
                    }
                }
                if (settings.ShowTopSkills)
                {
                    infoStrings.Add($"Level {adoptedHero.Level}");
                    infoStrings.Add("Skills ■ " + string.Join(" ■ ", 
                        SkillObject.All
                            .Where(s => adoptedHero.GetSkillValue(s) >= settings.MinSkillToShow)
                            .OrderByDescending(s => adoptedHero.GetSkillValue(s))
                            .Select(skill => $"{AdoptAHero.SkillMapping[skill.Name.ToString()]} {adoptedHero.GetSkillValue(skill)} " +
                                             $"[f{adoptedHero.HeroDeveloper.GetFocus(skill)}]")
                    ));
                }
                if (settings.ShowAttributes)
                {
                    infoStrings.Add("Attr ■ " + string.Join(" ■ ", AdoptAHero.CharAttributes
                        .Select(a => $"{a.shortName} {adoptedHero.GetAttributeValue(a.val)}")));
                }
                if (settings.ShowEquipment)
                {
                    infoStrings.Add($"Equip Tier {EquipHero.GetHeroEquipmentTier(adoptedHero) + 1}");
                    infoStrings.Add("Battle ■ " + string.Join(" ■ ", adoptedHero.BattleEquipment
                        .YieldEquipmentSlots()
                        .Where(e => !e.element.IsEmpty)
                        .Select(e => $"{e.element.Item.Name}")
                    ));
                    infoStrings.Add("Civ ■ " + string.Join(" ■ ", adoptedHero.CivilianEquipment
                        .YieldEquipmentSlots()
                        .Where(e => !e.element.IsEmpty)
                        .Select(e => $"{e.element.Item.Name}")
                    ));
                }
            }

            ActionManager.SendReply(context, infoStrings.ToArray());
        }
    }
}