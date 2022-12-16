using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Localization;
using TaleWorlds.Core;

namespace BannerlordTwitch.Helpers
{
    [Flags]
    public enum SkillsEnum
    {
        [LocDisplayName("{=p2ImRezI}None")] None,
        [LocDisplayName("{=ZJUJQl8V}All")] All,
        [LocDisplayName("{=Mtc9BiNI}Melee")] Melee,
        [LocDisplayName("{=NRNrLcBt}One Handed")] OneHanded, 
        [LocDisplayName("{=93kpWTwA}Two Handed")] TwoHanded, 
        [LocDisplayName("{=PWibGbJ4}Polearm")] Polearm,
        [LocDisplayName("{=oidnJJjF}Ranged")] Ranged,
        [LocDisplayName("{=HW64G8xr}Bow")] Bow, 
        [LocDisplayName("{=yR8vmGlW}Throwing")] Throwing, 
        [LocDisplayName("{=UG5KpbT7}Crossbow")] Crossbow,
        [LocDisplayName("{=tVQs3vuO}Movement")] Movement,
        [LocDisplayName("{=14Unsmk1}Riding")] Riding, 
        [LocDisplayName("{=4zlH32Z5}Athletics")] Athletics,
        [LocDisplayName("{=AZD2nxRg}Support")] Support,
        [LocDisplayName("{=pOYKEhhf}Scouting")] Scouting, 
        [LocDisplayName("{=ijZZGbRE}Trade")] Trade, 
        [LocDisplayName("{=CQqpGRtH}Steward")] Steward, 
        [LocDisplayName("{=X6rHMhsQ}Medicine")] Medicine, 
        [LocDisplayName("{=tNC2NHRC}Engineering")] Engineering,
        [LocDisplayName("{=eYTQ1TD7}Personal")] Personal,
        [LocDisplayName("{=PrE6W0WX}Crafting")] Crafting, 
        [LocDisplayName("{=BfNX6sak}Tactics")] Tactics, 
        [LocDisplayName("{=OvyNv1El}Roguery")] Roguery, 
        [LocDisplayName("{=1NcKYexo}Charm")] Charm, 
        [LocDisplayName("{=4684bsD7}Leadership")] Leadership,
    }

    public static class SkillGroup
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

        public static (SkillObject skill, ItemObject.ItemTypeEnum itemType)[] MeleeSkillItemPairs => new[]
        {
            (DefaultSkills.OneHanded, ItemObject.ItemTypeEnum.OneHandedWeapon),
            (DefaultSkills.TwoHanded, ItemObject.ItemTypeEnum.TwoHandedWeapon),
            (DefaultSkills.Polearm, ItemObject.ItemTypeEnum.Polearm),
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
        
        // notice that ChestArmor is NOT here, its not currently used by the game, 
        // and including it will result in a failure to find any matching items
        public static readonly (EquipmentIndex slot, ItemObject.ItemTypeEnum itemType)[] ArmorIndexType = {
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
                => CampaignHelpers.AllSkillObjects.FirstOrDefault(so 
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
        
        public static IEnumerable<SkillsEnum> GetSkillsForEquipmentType(params EquipmentType[] equipTypes)
            => GetSkillsForEquipmentType((IEnumerable<EquipmentType>)equipTypes);
        
        public static IEnumerable<SkillsEnum> GetSkillsForEquipmentType(IEnumerable<EquipmentType> equipTypes)
        {
            foreach (var i in equipTypes)
            {
                switch (i)
                {
                    case EquipmentType.Dagger:
                    case EquipmentType.OneHandedSword:
                    case EquipmentType.OneHandedAxe:
                    case EquipmentType.OneHandedMace:
                        yield return SkillsEnum.OneHanded;
                        break;
                    case EquipmentType.TwoHandedSword:
                    case EquipmentType.TwoHandedAxe:
                    case EquipmentType.TwoHandedMace:
                        yield return SkillsEnum.TwoHanded;
                        break;
                    case EquipmentType.OneHandedLance:
                    case EquipmentType.TwoHandedLance:
                    case EquipmentType.OneHandedGlaive:
                    case EquipmentType.TwoHandedGlaive:
                        yield return SkillsEnum.Polearm;
                        break;
                    case EquipmentType.Bow:
                    case EquipmentType.Arrows:
                        yield return SkillsEnum.Bow;
                        break;
                    case EquipmentType.Crossbow:
                    case EquipmentType.Bolts:
                        yield return SkillsEnum.Crossbow;
                        break;
                    case EquipmentType.ThrowingKnives:
                    case EquipmentType.ThrowingAxes:
                    case EquipmentType.ThrowingJavelins:
                        yield return SkillsEnum.Throwing;
                        break;
                    // case EquipmentType.Shield:
                    default:
                        yield return SkillsEnum.None;
                        break;
                }
            }
        }
        
        public static IEnumerable<EquipmentType> GetEquipmentTypeForSkills(params SkillObject[] skills)
            => GetEquipmentTypeForSkills((IEnumerable<SkillObject>)skills);
        
        public static IEnumerable<EquipmentType> GetEquipmentTypeForSkills(IEnumerable<SkillObject> skills)
        {
            foreach (var i in skills)
            {
                if (i == DefaultSkills.OneHanded)
                {
                    yield return EquipmentType.OneHandedSword;
                    yield return EquipmentType.OneHandedAxe;
                    yield return EquipmentType.OneHandedMace;
                }
                else if(i == DefaultSkills.TwoHanded)
                {
                    yield return EquipmentType.TwoHandedSword;
                    yield return EquipmentType.TwoHandedAxe;
                    yield return EquipmentType.TwoHandedMace;
                }
                else if(i == DefaultSkills.Polearm)
                {
                    yield return EquipmentType.OneHandedLance;
                    yield return EquipmentType.TwoHandedLance;
                    yield return EquipmentType.OneHandedGlaive;
                    yield return EquipmentType.TwoHandedGlaive;
                }
                else if(i == DefaultSkills.Bow)
                {
                    yield return EquipmentType.Bow;
                    yield return EquipmentType.Arrows;
                }
                else if(i == DefaultSkills.Crossbow)
                {
                    yield return EquipmentType.Crossbow;
                    yield return EquipmentType.Bolts;
                }
                else if(i == DefaultSkills.Throwing)
                {
                    yield return EquipmentType.ThrowingKnives;
                    yield return EquipmentType.ThrowingAxes;
                    yield return EquipmentType.ThrowingJavelins;
                }
            }
        }
    }
}