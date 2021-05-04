using System;
using System.Linq;

namespace BLTAdoptAHero
{
    
    [Flags]
    internal enum Skills
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
                    return new[] { skills };
            }
        }

        public static string[] SkillsToStrings(Skills skills)
        {
            return ExpandSkills(skills).Select(s => s.ToString()).ToArray();
        }
    }
}