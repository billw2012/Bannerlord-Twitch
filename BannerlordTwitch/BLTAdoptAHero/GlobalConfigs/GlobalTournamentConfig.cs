using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    [CategoryOrder("General", 1)]
    [CategoryOrder("Equipment", 2)]
    [CategoryOrder("Balancing", 3)]
    [CategoryOrder("Round Type", 4)]
    [CategoryOrder("Round Rewards", 5)] 
    [CategoryOrder("Rewards", 6)]
    [CategoryOrder("Betting", 7)]
    [CategoryOrder("Prize", 8)]
    [CategoryOrder("Prize Tier", 9)]
    [CategoryOrder("Custom Prize", 10)]
    internal class GlobalTournamentConfig : IDocumentable
    {
        private const string ID = "Adopt A Hero - Tournament Config";
        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalTournamentConfig));
        internal static GlobalTournamentConfig Get() => ActionManager.GetGlobalConfig<GlobalTournamentConfig>(ID);
        internal static GlobalTournamentConfig Get(Settings fromSettings) => fromSettings.GetGlobalConfig<GlobalTournamentConfig>(ID);

        #region General
        [Category("General"), 
         Description("Amount to multiply normal starting health by"), PropertyOrder(1), UsedImplicitly, Document]
        public float StartHealthMultiplier { get; set; } = 2;
        [Category("General"),
         Description("Heroes won't get any kill rewards in tournaments"),
         PropertyOrder(2), Document, UsedImplicitly]
        public bool DisableKillRewardsInTournament { get; set; } = true;
        [Category("General"),
         Description("Tournament kills / deaths won't be counted towards achievements or kill streaks"),
         PropertyOrder(3), Document, UsedImplicitly]
        public bool DisableTrackingKillsTournament { get; set; } = true;
        #endregion

        #region Equipment
        [Category("Equipment"), 
         Description("Remove horses completely from the BLT tournaments (the horse AI is terrible)"), 
         PropertyOrder(2), UsedImplicitly, Document]
        public bool NoHorses { get; set; } = true;
        
        [Category("Equipment"), 
         Description("Replaces all lances and spears with swords, because lance and spear combat is terrible"), 
         PropertyOrder(3), UsedImplicitly, Document]
        public bool NoSpears { get; set; } = true;
        
        [Category("Equipment"), 
         Description("Replaces all armor with fixed tier armor, based on Culture if possible " +
                     "(tier specified by Normalized Armor Tier below)"), 
         PropertyOrder(4), UsedImplicitly, Document]
        public bool NormalizeArmor { get; set; }

        [Category("Equipment"),
         Description("Armor tier to set all contenstants to (1 to 6), if Normalize Armor is enabled"),
         PropertyOrder(5), UsedImplicitly, Document]
        public int NormalizeArmorTier { get; set; } = 3;

        [Category("Equipment"),
         Description("Randomizes the weapons used in each round, weighted based on the classes of the participants"),
         PropertyOrder(6), UsedImplicitly, Document]
        public bool RandomizeWeaponTypes { get; set; } = true;
        #endregion
        
        #region Balancing

        public class SkillModifierDef
        {
            [Description("Skill or skill group to modifer (all skills in a group will be modified)"),
             PropertyOrder(1), UsedImplicitly, Document]
            public SkillsEnum Skill { get; set; } = SkillsEnum.All;

            [Description("Reduction to the skill per win (in %). See https://www.desmos.com/calculator/ajydvitcer " +
                         "for visualization of how skill will be modified."),
             PropertyOrder(2), UsedImplicitly, Document]
            public float SkillReductionPercentPerWin { get; set; } = 3.2f;
            
            [Description("The lower limit (in %) that the skill(s) can be reduced to."),
             PropertyOrder(2), UsedImplicitly, Document]
            public float FloorPercent { get; set; } = 65f;
        }

        [Category("Balancing"),
         Description("Applies skill debuffers to previous tournament winners"),
         PropertyOrder(1), UsedImplicitly, Document]
        public List<SkillModifierDef> PreviousWinnerDebuffs { get; set; } = new() { new() };
        
        #endregion
        
        #region Round Types
        public class Round1Def
        {
            [Description("Allow the vanilla round setup"), PropertyOrder(1), UsedImplicitly, Document]
            public bool EnableVanilla { get; set; } = true;
            [Description("Allow 1 match with 4 teams of 4"), PropertyOrder(3), UsedImplicitly, Document]
            public bool Enable1Match4Teams { get; set; }
            [Description("Allow 2 matches with 2 teams of 4"), PropertyOrder(4), UsedImplicitly, Document]
            public bool Enable2Match2Teams { get; set; } 
            [Description("Allow 2 matches with 4 teams of 2"), PropertyOrder(5), UsedImplicitly, Document]
            public bool Enable2Match4Teams { get; set; }
            [Description("Allow 4 matches with 2 teams of 2"), PropertyOrder(6), UsedImplicitly, Document]
            public bool Enable4Match2Teams { get; set; }
            [Description("Allow 4 matches with 4 teams of 1"), PropertyOrder(7), UsedImplicitly, Document]
            public bool Enable4Match4Teams { get; set; }
            [Description("Allow 8 matches with 2 teams of 1"), PropertyOrder(8), UsedImplicitly, Document]
            public bool Enable8Match2Teams { get; set; }

            public const int ParticipantCount = 16;
            public const int WinnerCount = ParticipantCount / 2;
            
            public TournamentRound GetRandomRound(TournamentRound vanilla, TournamentGame.QualificationMode qualificationMode)
            {
                var matches = new List<TournamentRound>();
                if(Enable1Match4Teams) 
                    matches.Add(new (ParticipantCount, 1, 4, WinnerCount, qualificationMode));
                if(Enable2Match2Teams) 
                    matches.Add(new (ParticipantCount, 2, 2, WinnerCount, qualificationMode)); 
                if(Enable2Match4Teams) 
                    matches.Add(new (ParticipantCount, 2, 4, WinnerCount, qualificationMode));
                if(Enable4Match2Teams) 
                    matches.Add(new (ParticipantCount, 4, 2, WinnerCount, qualificationMode));
                if(Enable4Match4Teams) 
                    matches.Add(new (ParticipantCount, 4, 4, WinnerCount, qualificationMode));
                if(Enable8Match2Teams) 
                    matches.Add(new (ParticipantCount, 8, 2, WinnerCount, qualificationMode));
                if(EnableVanilla || !matches.Any())
                    matches.Add(vanilla);
                return matches.SelectRandom();
            }
        }
        
        public class Round2Def
        {
            [Description("Allow the vanilla round setup"), PropertyOrder(1), UsedImplicitly, Document]
            public bool EnableVanilla { get; set; } = true;
            [Description("Allow 1 match with 2 teams of 4"), PropertyOrder(2), UsedImplicitly, Document]
            public bool Enable1Match2Teams { get; set; }
            [Description("Allow 1 match with 4 teams of 2"), PropertyOrder(3), UsedImplicitly, Document]
            public bool Enable1Match4Teams { get; set; }
            [Description("Allow 2 matches with 2 teams of 2"), PropertyOrder(4), UsedImplicitly, Document]
            public bool Enable2Match2Teams { get; set; } 
            [Description("Allow 2 matches with 4 teams of 1"), PropertyOrder(5), UsedImplicitly, Document]
            public bool Enable2Match4Teams { get; set; }
            [Description("Allow 4 matches with 2 teams of 1"), PropertyOrder(6), UsedImplicitly, Document]
            public bool Enable4Match2Teams { get; set; }

            public const int ParticipantCount = 8;
            public const int WinnerCount = ParticipantCount / 2;

            public TournamentRound GetRandomRound(TournamentRound vanilla, TournamentGame.QualificationMode qualificationMode)
            {
                var matches = new List<TournamentRound>();
                if(Enable1Match2Teams) 
                    matches.Add(new (ParticipantCount, 1, 2, WinnerCount, qualificationMode));
                if(Enable1Match4Teams) 
                    matches.Add(new (ParticipantCount, 1, 4, WinnerCount, qualificationMode));
                if(Enable2Match2Teams) 
                    matches.Add(new (ParticipantCount, 2, 2, WinnerCount, qualificationMode)); 
                if(Enable2Match4Teams) 
                    matches.Add(new (ParticipantCount, 2, 4, WinnerCount, qualificationMode));
                if(Enable4Match2Teams) 
                    matches.Add(new (ParticipantCount, 4, 2, WinnerCount, qualificationMode));
                if(EnableVanilla || !matches.Any())
                    matches.Add(vanilla);
                return matches.SelectRandom();
            }
        }
        
        public class Round3Def
        {
            [Description("Allow the vanilla round setup"), PropertyOrder(1), UsedImplicitly, Document]
            public bool EnableVanilla { get; set; } = true;
            [Description("Allow 1 match with 2 teams of 2"), PropertyOrder(2), UsedImplicitly, Document]
            public bool Enable1Match2Teams { get; set; }
            [Description("Allow 1 match with 4 teams of 1"), PropertyOrder(3), UsedImplicitly, Document]
            public bool Enable1Match4Teams { get; set; }
            [Description("Allow 2 matches with 2 teams of 1"), PropertyOrder(4), UsedImplicitly, Document]
            public bool Enable2Match2Teams { get; set; } 

            public const int ParticipantCount = 4;
            public const int WinnerCount = ParticipantCount / 2;

            public TournamentRound GetRandomRound(TournamentRound vanilla, TournamentGame.QualificationMode qualificationMode)
            {
                var matches = new List<TournamentRound>();
                if(Enable1Match2Teams) 
                    matches.Add(new (ParticipantCount, 1, 2, WinnerCount, qualificationMode));
                if(Enable1Match4Teams) 
                    matches.Add(new (ParticipantCount, 1, 4, WinnerCount, qualificationMode));
                if(Enable2Match2Teams) 
                    matches.Add(new (ParticipantCount, 2, 2, WinnerCount, qualificationMode)); 
                if(EnableVanilla || !matches.Any())
                    matches.Add(vanilla);
                return matches.SelectRandom();
            }
        }

        [Category("Round Type"), Description("Round 1 Type"), PropertyOrder(1), ExpandableObject, UsedImplicitly, Document]
        public Round1Def Round1Type { get; set; } = new();
        [Category("Round Type"), Description("Round 2 Type"), PropertyOrder(2), ExpandableObject,UsedImplicitly, Document]
        public Round2Def Round2Type { get; set; } = new();
        [Category("Round Type"), Description("Round 3 Type"), PropertyOrder(3), ExpandableObject,UsedImplicitly, Document]
        public Round3Def Round3Type { get; set; } = new();
        
        #endregion
        
        #region Round Rewards
        public class RoundRewardsDef
        {
            [Description("Gold won if the hero wins thier match in the round"), PropertyOrder(1), UsedImplicitly, Document]
            public int WinGold { get; set; } = 10000;

            [Description("XP given if the hero wins thier match in the round"), PropertyOrder(2), UsedImplicitly, Document]
            public int WinXP { get; set; } = 10000;

            [Description("XP given if the hero loses thier match in a round"), PropertyOrder(3), UsedImplicitly, Document]
            public int LoseXP { get; set; } = 2500;
            
            public override string ToString() =>
                $"Win Gold {WinGold}, Win XP {WinXP}, Lose XP {LoseXP}";
        }
        
        [Category("Round Rewards"), Description("Round 1 Rewards"), PropertyOrder(1),
         ExpandableObject, UsedImplicitly, Document]
        public RoundRewardsDef Round1Rewards { get; set; } = new() { WinGold = 5000, WinXP = 5000, LoseXP = 5000 };
        [Category("Round Rewards"), Description("Round 2 Rewards"), PropertyOrder(2), 
         ExpandableObject,UsedImplicitly, Document]
        public RoundRewardsDef Round2Rewards { get; set; } = new() { WinGold = 7500, WinXP = 7500, LoseXP = 7500 };
        [Category("Round Rewards"), Description("Round 3 Rewards"), PropertyOrder(3), 
         ExpandableObject,UsedImplicitly, Document]
        public RoundRewardsDef Round3Rewards { get; set; } = new() { WinGold = 10000, WinXP = 10000, LoseXP = 10000 };
        [Category("Round Rewards"), Description("Round 4 Rewards"), PropertyOrder(4), 
         ExpandableObject,UsedImplicitly, Document]
        public RoundRewardsDef Round4Rewards { get; set; } = new() { WinGold = 0, WinXP = 0, LoseXP = 0 };

        public RoundRewardsDef[] RoundRewards => new[] { Round1Rewards, Round2Rewards, Round3Rewards, Round4Rewards };
        #endregion

        #region Rewards
        [Category("Rewards"), Description("Gold won if the hero wins the tournaments"), 
         PropertyOrder(1), UsedImplicitly, Document]
        public int WinGold { get; set; } = 50000;

        [Category("Rewards"), Description("XP given if the hero wins the tournaments"), 
         PropertyOrder(2), UsedImplicitly, Document]
        public int WinXP { get; set; } = 50000;

        [Category("Rewards"), Description("XP given if the hero participates in a tournament but doesn't win"), 
         PropertyOrder(3), UsedImplicitly, Document]
        public int ParticipateXP { get; set; } = 10000;
        #endregion

        #region Betting
        [Category("Betting"), Description("Enable betting"), PropertyOrder(1), UsedImplicitly, Document]
        public bool EnableBetting { get; set; } = true;

        [Category("Betting"), 
         Description("Only allow betting on the final betting"), PropertyOrder(2), UsedImplicitly, Document]
        public bool BettingOnFinalOnly { get; set; }
        #endregion

        #region Prize
        [Category("Prize"), 
         Description("Relative proportion of prizes that will be weapons. " +
                     "This includes all one handed, two handed, ranged and ammo."), 
         PropertyOrder(1), UsedImplicitly, Document]
        public float PrizeWeaponWeight { get; set; } = 1f;

        [Category("Prize"), Description("Relative proportion of prizes that will be armor"), 
         PropertyOrder(2), UsedImplicitly, Document]
        public float PrizeArmorWeight { get; set; } = 1f;

        [Category("Prize"), Description("Relative proportion of prizes that will be mounts"), 
         PropertyOrder(3), UsedImplicitly, Document]
        public float PrizeMountWeight { get; set; } = 0.1f;
        #endregion
        
        #region Prize Tier
        // Prizes:
        // Random vanilla equipment, chance for each tier
        // Generated vanilla equip,ent

        [Category("Prize Tier"), Description("Relative proportion of prizes that will be Tier 1"), 
         PropertyOrder(1), UsedImplicitly, Document]
        public float PrizeTier1Weight { get; set; }

        [Category("Prize Tier"), Description("Relative proportion of prizes that will be Tier 2"), 
         PropertyOrder(2), UsedImplicitly, Document]
        public float PrizeTier2Weight { get; set; }

        [Category("Prize Tier"), Description("Relative proportion of prizes that will be Tier 3"), 
         PropertyOrder(3), UsedImplicitly, Document]
        public float PrizeTier3Weight { get; set; }

        [Category("Prize Tier"), Description("Relative proportion of prizes that will be Tier 4"), 
         PropertyOrder(4), UsedImplicitly, Document]
        public float PrizeTier4Weight { get; set; }

        [Category("Prize Tier"), Description("Relative proportion of prizes that will be Tier 5"), 
         PropertyOrder(5), UsedImplicitly, Document]
        public float PrizeTier5Weight { get; set; } = 3f;

        [Category("Prize Tier"), Description("Relative proportion of prizes that will be Tier 6"), 
         PropertyOrder(6), UsedImplicitly, Document]
        public float PrizeTier6Weight { get; set; } = 2f;

        [Category("Prize Tier"), 
         Description("Relative proportion of prizes that will be Custom (Tier 6 with modifiers as per the Custom " +
                     "Prize settings below)"), PropertyOrder(7), UsedImplicitly, Document]
        public float PrizeCustomWeight { get; set; } = 1f;

        [Browsable(false), YamlIgnore]
        public IEnumerable<(int tier, float weight)> PrizeTierWeights
        {
            get
            {
                yield return (tier: 0, weight: PrizeTier1Weight);
                yield return (tier: 1, weight: PrizeTier2Weight);
                yield return (tier: 2, weight: PrizeTier3Weight);
                yield return (tier: 3, weight: PrizeTier4Weight);
                yield return (tier: 4, weight: PrizeTier5Weight);
                yield return (tier: 5, weight: PrizeTier6Weight);
                yield return (tier: 6, weight: PrizeCustomWeight);
            }
        }
        #endregion

        #region Custom Prize
        public class CustomPrizeConfig
        {
            [Description("Custom prize power, a global multiplier for the values below"), PropertyOrder(1), UsedImplicitly]
            public float Power { get; set; } = 1f;

            [Description("Weapon damage modifier for custom weapon prize"), PropertyOrder(2), UsedImplicitly, ExpandableObject]
            public RangeInt WeaponDamage { get; set; } = new(25, 50);
            
            [Description("Speed modifier for custom weapon prize"), PropertyOrder(3), UsedImplicitly, ExpandableObject]
            public RangeInt WeaponSpeed { get; set; } = new(25, 50);
            
            [Description("Missile speed modifier for custom weapon prize"), PropertyOrder(4), UsedImplicitly, ExpandableObject]
            public RangeInt WeaponMissileSpeed { get; set; } = new(25, 50);
            
            [Description("Ammo damage modifier for custom ammo prize"), PropertyOrder(5), UsedImplicitly, ExpandableObject]
            public RangeInt AmmoDamage { get; set; } = new (10, 30);
              
            [Description("Arrow stack size modifier for custom arrow prize"), PropertyOrder(6), UsedImplicitly, ExpandableObject]
            public RangeInt ArrowStack { get; set; } = new(25, 50);
              
            [Description("Throwing stack size modifier for custom throwing prize"), PropertyOrder(7), UsedImplicitly, ExpandableObject]
            public RangeInt ThrowingStack { get; set; } = new(2, 6);
            
            [Description("Armor modifier for custom armor prize"), PropertyOrder(8), UsedImplicitly, ExpandableObject]
            public RangeInt Armor { get; set; } = new(10, 20);
            
            [Description("Maneuver multiplier for custom mount prize"), PropertyOrder(9), UsedImplicitly, ExpandableObject]
            public RangeFloat MountManeuver { get; set; } = new(1.25f, 2f);
            
            [Description("Speed multiplier for custom mount prize"), PropertyOrder(10), UsedImplicitly, ExpandableObject]
            public RangeFloat MountSpeed { get; set; } = new(1.25f, 2f);
              
            [Description("Charge damage multiplier for custom mount prize"), PropertyOrder(11), UsedImplicitly, ExpandableObject]
            public RangeFloat MountChargeDamage { get; set; } = new(1.25f, 2f);

            [Description("Hitpoints multiplier for custom mount prize"), PropertyOrder(12), UsedImplicitly, ExpandableObject]
            public RangeFloat MountHitPoints { get; set; } = new(1.25f, 2f);
        }

        [Category("Custom Prize"), 
         Description("Custom prize configuration"), PropertyOrder(1), UsedImplicitly, ExpandableObject]
        public CustomPrizeConfig CustomPrize { get; set; } = new();

        public enum PrizeType
        {
            Weapon,
            Armor,
            Mount
        }

        [Browsable(false), YamlIgnore]
        public IEnumerable<(PrizeType type, float weight)> PrizeTypeWeights {
            get
            {
                yield return (type: PrizeType.Weapon, weight: PrizeWeaponWeight);
                yield return (type: PrizeType.Armor, weight: PrizeArmorWeight);
                yield return (type: PrizeType.Mount, weight: PrizeMountWeight);
            }
        }
        #endregion

        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.Div("tournament-config", () =>
            {
                generator.H1("Global Tournament Config");
                DocumentationHelpers.AutoDocument(generator, this);
            });
        }
        #endregion
    }
}