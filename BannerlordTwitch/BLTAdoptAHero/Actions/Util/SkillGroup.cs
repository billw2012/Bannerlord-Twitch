﻿using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    
    [Flags]
    public enum Skills
    {
        None,
        All,
        
        Melee,
        OneHanded, TwoHanded, Polearm,
        
        Ranged,
        Bow, Throwing, Crossbow,
        
        Movement,
        Riding, Athletics,
        
        Support,
        Scouting, Trade, Steward, Medicine, Engineering,
        
        Personal,
        Crafting, Tactics, Roguery, Charm, Leadership,
    }
    
    public class SingleSkillsItemSource : IItemsSource
    {
        public ItemCollection GetValues() => new() {
                Skills.OneHanded, Skills.TwoHanded, Skills.Polearm,
                Skills.Bow, Skills.Throwing, Skills.Crossbow,
                Skills.Riding, Skills.Athletics,
                Skills.Scouting, Skills.Trade, Skills.Steward, Skills.Medicine, Skills.Engineering,
                Skills.Crafting, Skills.Tactics, Skills.Roguery, Skills.Charm, Skills.Leadership,
            };
    }
    
    internal static class SkillGroup
    {
        public static Skills[] ExpandSkills(Skills skills)
        {
            switch (skills)
            {
                case Skills.Melee: return new[] { Skills.OneHanded, Skills.TwoHanded, Skills.Polearm };
                case Skills.Ranged: return new[] { Skills.Bow , Skills.Throwing , Skills.Crossbow };
                case Skills.Movement: return new[] { Skills.Riding , Skills.Athletics };
                case Skills.Support: return new[] { Skills.Scouting , Skills.Trade , Skills.Steward , Skills.Medicine , Skills.Engineering };
                case Skills.Personal: return new[] { Skills.Crafting ,Skills.Tactics , Skills.Roguery , Skills.Charm ,  Skills.Leadership };
                case Skills.All: return new[] {Skills.OneHanded , Skills.TwoHanded , Skills.Polearm , Skills.Bow , Skills.Throwing ,
                    Skills.Crossbow , Skills.Riding , Skills.Athletics , Skills.Crafting , Skills.Tactics , 
                    Skills.Scouting , Skills.Roguery , Skills.Charm , Skills.Trade , Skills.Steward ,
                    Skills.Medicine , Skills.Engineering , Skills.Leadership};
                case Skills.None: return new Skills[] { };
                default:
                    return new[] {skills};
            }
        }

        public static SkillObject GetSkill(Skills skill) =>
            skill switch
            {
                Skills.OneHanded => DefaultSkills.OneHanded,
                Skills.TwoHanded => DefaultSkills.TwoHanded,
                Skills.Polearm => DefaultSkills.Polearm,
                Skills.Bow => DefaultSkills.Bow,
                Skills.Throwing => DefaultSkills.Throwing,
                Skills.Crossbow => DefaultSkills.Crossbow,
                Skills.Riding => DefaultSkills.Riding,
                Skills.Athletics => DefaultSkills.Athletics,
                Skills.Scouting => DefaultSkills.Scouting,
                Skills.Trade => DefaultSkills.Trade,
                Skills.Steward => DefaultSkills.Steward,
                Skills.Medicine => DefaultSkills.Medicine,
                Skills.Engineering => DefaultSkills.Engineering,
                Skills.Crafting => DefaultSkills.Crafting,
                Skills.Tactics => DefaultSkills.Tactics,
                Skills.Roguery => DefaultSkills.Roguery,
                Skills.Charm => DefaultSkills.Charm,
                Skills.Leadership => DefaultSkills.Leadership,
                _ => throw new ArgumentOutOfRangeException(nameof(skill), skill, "Only single skill values are valid")
            };

        public static string[] SkillsToStrings(Skills skills)
        {
            return ExpandSkills(skills).Select(s => s.ToString()).ToArray();
        }

        // These must be properties not fields, as these values are dynamic
        public static SkillObject[] MeleeSkills => new [] {
            DefaultSkills.OneHanded,
            DefaultSkills.TwoHanded,
            DefaultSkills.Polearm,
        };

        public static ItemObject.ItemTypeEnum[] MeleeItems => new [] {
            ItemObject.ItemTypeEnum.OneHandedWeapon,
            ItemObject.ItemTypeEnum.TwoHandedWeapon,
            ItemObject.ItemTypeEnum.Polearm,
        };

        public static SkillObject[] RangedSkills => new [] {
            DefaultSkills.Bow,
            DefaultSkills.Crossbow,
            DefaultSkills.Throwing,
        };

        public static ItemObject.ItemTypeEnum[] RangedItems => new [] {
            ItemObject.ItemTypeEnum.Bow,
            ItemObject.ItemTypeEnum.Crossbow,
            ItemObject.ItemTypeEnum.Thrown,
        };
        
        public static (SkillObject skill, ItemObject.ItemTypeEnum itemType)[] MeleeSkillItemPairs => new[]
        {
            (DefaultSkills.OneHanded, ItemObject.ItemTypeEnum.OneHandedWeapon),
            (DefaultSkills.TwoHanded, ItemObject.ItemTypeEnum.TwoHandedWeapon),
            (DefaultSkills.Polearm, ItemObject.ItemTypeEnum.Polearm),
        };
        
        public static (SkillObject skill, ItemObject.ItemTypeEnum itemType)[] RangedSkillItemPairs => new[]
        {
            (DefaultSkills.Bow, ItemObject.ItemTypeEnum.Bow),
            (DefaultSkills.Crossbow, ItemObject.ItemTypeEnum.Crossbow),
            (DefaultSkills.Throwing, ItemObject.ItemTypeEnum.Thrown),
        };
        public static (SkillObject skill, ItemObject.ItemTypeEnum itemType)[] SkillItemPairs => new[]
        {
            (DefaultSkills.OneHanded, ItemObject.ItemTypeEnum.OneHandedWeapon),
            (DefaultSkills.TwoHanded, ItemObject.ItemTypeEnum.TwoHandedWeapon),
            (DefaultSkills.Polearm, ItemObject.ItemTypeEnum.Polearm),
            (DefaultSkills.Bow, ItemObject.ItemTypeEnum.Bow),
            (DefaultSkills.Crossbow, ItemObject.ItemTypeEnum.Crossbow),
            (DefaultSkills.Throwing, ItemObject.ItemTypeEnum.Thrown),
        };

        public static SkillObject[] MovementSkills => new [] {
            DefaultSkills.Riding,
            DefaultSkills.Athletics,
        };

        public static (EquipmentIndex, ItemObject.ItemTypeEnum)[] ArmorIndexType => new[] {
            (EquipmentIndex.Head, ItemObject.ItemTypeEnum.HeadArmor),
            (EquipmentIndex.Body, ItemObject.ItemTypeEnum.BodyArmor),
            (EquipmentIndex.Leg, ItemObject.ItemTypeEnum.LegArmor),
            (EquipmentIndex.Gloves, ItemObject.ItemTypeEnum.HandArmor),
            (EquipmentIndex.Cape, ItemObject.ItemTypeEnum.Cape),
        };

        public static IEnumerable<SkillObject> GetSkills(params Skills[] sk)
            => GetSkills((IEnumerable<Skills>)sk);

        public static IEnumerable<SkillObject> GetSkills(IEnumerable<Skills> sk)
            => sk.SelectMany(ExpandSkills).Distinct().Select(GetSkill);
        
        public static IEnumerable<SkillObject> GetSkills(IEnumerable<string> sk) 
            => sk.Select(sn 
                => DefaultSkills.GetAllSkills().FirstOrDefault(so 
                    => string.Equals(so.StringId, sn, StringComparison.CurrentCultureIgnoreCase)));
        
        public static IEnumerable<ItemObject.ItemTypeEnum> GetItemsForSkills(params Skills[] sk)
            => GetItemsForSkills((IEnumerable<Skills>)sk);

        public static IEnumerable<ItemObject.ItemTypeEnum> GetItemsForSkills(IEnumerable<Skills> sk)
        {
            foreach (var s in sk)
            {
                switch (s)
                {
                    case Skills.OneHanded:
                        yield return ItemObject.ItemTypeEnum.OneHandedWeapon;
                        break;
                    case Skills.TwoHanded:
                        yield return ItemObject.ItemTypeEnum.TwoHandedWeapon;
                        break;
                    case Skills.Polearm:
                        yield return ItemObject.ItemTypeEnum.Polearm;
                        break;
                    case Skills.Bow:
                        yield return ItemObject.ItemTypeEnum.Bow;
                        break;
                    case Skills.Throwing:
                        yield return ItemObject.ItemTypeEnum.Thrown;
                        break;
                    case Skills.Crossbow:
                        yield return ItemObject.ItemTypeEnum.Crossbow;
                        break;
                }
            }
        }

        public static IEnumerable<Skills> GetSkillsForItem(params ItemObject.ItemTypeEnum[] items)
            => GetSkillsForItem((IEnumerable<ItemObject.ItemTypeEnum>)items);
        
        public static IEnumerable<Skills> GetSkillsForItem(IEnumerable<ItemObject.ItemTypeEnum> items)
        {
            foreach (var i in items)
            {
                switch (i)
                {
                    case ItemObject.ItemTypeEnum.OneHandedWeapon:
                        yield return Skills.OneHanded;
                        break;
                    case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                        yield return Skills.TwoHanded;
                        break;
                    case ItemObject.ItemTypeEnum.Polearm:
                        yield return Skills.Polearm;
                        break;
                    case ItemObject.ItemTypeEnum.Bow:
                        yield return Skills.Bow;
                        break;
                    case ItemObject.ItemTypeEnum.Thrown:
                        yield return Skills.Throwing;
                        break;
                    case ItemObject.ItemTypeEnum.Crossbow:
                        yield return Skills.Crossbow;
                        break;
                }
            }
        }
    }
}