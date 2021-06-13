using System;
using System.Collections.Generic;
using System.Linq;
using BLTAdoptAHero.Actions.Util;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    
    [Flags]
    public enum SkillsEnum
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
                SkillsEnum.OneHanded, SkillsEnum.TwoHanded, SkillsEnum.Polearm,
                SkillsEnum.Bow, SkillsEnum.Throwing, SkillsEnum.Crossbow,
                SkillsEnum.Riding, SkillsEnum.Athletics,
                SkillsEnum.Scouting, SkillsEnum.Trade, SkillsEnum.Steward, SkillsEnum.Medicine, SkillsEnum.Engineering,
                SkillsEnum.Crafting, SkillsEnum.Tactics, SkillsEnum.Roguery, SkillsEnum.Charm, SkillsEnum.Leadership,
            };
    }
    
    internal static class SkillGroup
    {
        public static SkillsEnum[] ExpandSkills(SkillsEnum skills)
        {
            switch (skills)
            {
                case SkillsEnum.Melee: return new[] { SkillsEnum.OneHanded, SkillsEnum.TwoHanded, SkillsEnum.Polearm };
                case SkillsEnum.Ranged: return new[] { SkillsEnum.Bow , SkillsEnum.Throwing , SkillsEnum.Crossbow };
                case SkillsEnum.Movement: return new[] { SkillsEnum.Riding , SkillsEnum.Athletics };
                case SkillsEnum.Support: return new[] { SkillsEnum.Scouting , SkillsEnum.Trade , SkillsEnum.Steward , SkillsEnum.Medicine , SkillsEnum.Engineering };
                case SkillsEnum.Personal: return new[] { SkillsEnum.Crafting ,SkillsEnum.Tactics , SkillsEnum.Roguery , SkillsEnum.Charm ,  SkillsEnum.Leadership };
                case SkillsEnum.All: return new[] {SkillsEnum.OneHanded , SkillsEnum.TwoHanded , SkillsEnum.Polearm , SkillsEnum.Bow , SkillsEnum.Throwing ,
                    SkillsEnum.Crossbow , SkillsEnum.Riding , SkillsEnum.Athletics , SkillsEnum.Crafting , SkillsEnum.Tactics , 
                    SkillsEnum.Scouting , SkillsEnum.Roguery , SkillsEnum.Charm , SkillsEnum.Trade , SkillsEnum.Steward ,
                    SkillsEnum.Medicine , SkillsEnum.Engineering , SkillsEnum.Leadership};
                case SkillsEnum.None: return new SkillsEnum[] { };
                default:
                    return new[] {skills};
            }
        }

        public static SkillObject GetSkill(SkillsEnum skill) =>
            skill switch
            {
                SkillsEnum.OneHanded => DefaultSkills.OneHanded,
                SkillsEnum.TwoHanded => DefaultSkills.TwoHanded,
                SkillsEnum.Polearm => DefaultSkills.Polearm,
                SkillsEnum.Bow => DefaultSkills.Bow,
                SkillsEnum.Throwing => DefaultSkills.Throwing,
                SkillsEnum.Crossbow => DefaultSkills.Crossbow,
                SkillsEnum.Riding => DefaultSkills.Riding,
                SkillsEnum.Athletics => DefaultSkills.Athletics,
                SkillsEnum.Scouting => DefaultSkills.Scouting,
                SkillsEnum.Trade => DefaultSkills.Trade,
                SkillsEnum.Steward => DefaultSkills.Steward,
                SkillsEnum.Medicine => DefaultSkills.Medicine,
                SkillsEnum.Engineering => DefaultSkills.Engineering,
                SkillsEnum.Crafting => DefaultSkills.Crafting,
                SkillsEnum.Tactics => DefaultSkills.Tactics,
                SkillsEnum.Roguery => DefaultSkills.Roguery,
                SkillsEnum.Charm => DefaultSkills.Charm,
                SkillsEnum.Leadership => DefaultSkills.Leadership,
                _ => throw new ArgumentOutOfRangeException(nameof(skill), skill, "Only single skill values are valid")
            };

        public static string[] SkillsToStrings(SkillsEnum skills)
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

        public static IEnumerable<SkillObject> GetSkills(params SkillsEnum[] sk)
            => GetSkills((IEnumerable<SkillsEnum>)sk);

        public static IEnumerable<SkillObject> GetSkills(IEnumerable<SkillsEnum> sk)
            => sk.SelectMany(ExpandSkills).Distinct().Select(GetSkill);
        
        public static IEnumerable<SkillObject> GetSkills(IEnumerable<string> sk) 
            => sk.Select(sn 
                => HeroHelpers.AllSkillObjects.FirstOrDefault(so 
                    => string.Equals(so.StringId, sn, StringComparison.CurrentCultureIgnoreCase)));
        
        public static IEnumerable<ItemObject.ItemTypeEnum> GetItemsForSkills(params SkillsEnum[] sk)
            => GetItemsForSkills((IEnumerable<SkillsEnum>)sk);

        public static IEnumerable<ItemObject.ItemTypeEnum> GetItemsForSkills(IEnumerable<SkillsEnum> sk)
        {
            foreach (var s in sk)
            {
                switch (s)
                {
                    case SkillsEnum.OneHanded:
                        yield return ItemObject.ItemTypeEnum.OneHandedWeapon;
                        break;
                    case SkillsEnum.TwoHanded:
                        yield return ItemObject.ItemTypeEnum.TwoHandedWeapon;
                        break;
                    case SkillsEnum.Polearm:
                        yield return ItemObject.ItemTypeEnum.Polearm;
                        break;
                    case SkillsEnum.Bow:
                        yield return ItemObject.ItemTypeEnum.Bow;
                        break;
                    case SkillsEnum.Throwing:
                        yield return ItemObject.ItemTypeEnum.Thrown;
                        break;
                    case SkillsEnum.Crossbow:
                        yield return ItemObject.ItemTypeEnum.Crossbow;
                        break;
                }
            }
        }

        public static IEnumerable<SkillsEnum> GetSkillsForItem(params ItemObject.ItemTypeEnum[] items)
            => GetSkillsForItem((IEnumerable<ItemObject.ItemTypeEnum>)items);
        
        public static IEnumerable<SkillsEnum> GetSkillsForItem(IEnumerable<ItemObject.ItemTypeEnum> items)
        {
            foreach (var i in items)
            {
                switch (i)
                {
                    case ItemObject.ItemTypeEnum.OneHandedWeapon:
                        yield return SkillsEnum.OneHanded;
                        break;
                    case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                        yield return SkillsEnum.TwoHanded;
                        break;
                    case ItemObject.ItemTypeEnum.Polearm:
                        yield return SkillsEnum.Polearm;
                        break;
                    case ItemObject.ItemTypeEnum.Bow:
                        yield return SkillsEnum.Bow;
                        break;
                    case ItemObject.ItemTypeEnum.Thrown:
                        yield return SkillsEnum.Throwing;
                        break;
                    case ItemObject.ItemTypeEnum.Crossbow:
                        yield return SkillsEnum.Crossbow;
                        break;
                }
            }
        }
    }
}