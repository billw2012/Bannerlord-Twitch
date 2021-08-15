using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    [CategoryOrder("General", 1), 
     CategoryOrder("Equipment", 2), 
     CategoryOrder("Balancing", 3),
     CategoryOrder("Round Type", 4), 
     CategoryOrder("Round Rewards", 5), 
     CategoryOrder("Rewards", 6),
     CategoryOrder("Betting", 7), 
     CategoryOrder("Prize", 8), 
     CategoryOrder("Prize Tier", 9),
     CategoryOrder("Custom Prize", 10),
     LocDisplayName("{=AkDCrLgg}Tournament Config")]
    internal class GlobalTournamentConfig : IDocumentable
    {
        #region Static
        private const string ID = "Adopt A Hero - Tournament Config";
        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalTournamentConfig));
        internal static GlobalTournamentConfig Get() => ActionManager.GetGlobalConfig<GlobalTournamentConfig>(ID);
        internal static GlobalTournamentConfig Get(Settings fromSettings) => fromSettings.GetGlobalConfig<GlobalTournamentConfig>(ID);
        #endregion

        #region User Editable
        #region General
        [LocDisplayName("{=P1ZCMZbp}Start Health Multiplier"), 
         LocCategory("General", "{=C5T5nnix}General"),
         LocDescription("{=n6Bc5M3s}Amount to multiply normal starting health by"),
         PropertyOrder(1), Range(0.5, 10),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), UsedImplicitly, Document]
        public float StartHealthMultiplier { get; set; } = 2;
        
        [LocDisplayName("{=x3wiU1LY}Disable Kill Rewards In Tournament"),
         LocCategory("General", "{=C5T5nnix}General"),
         LocDescription("{=j63d8DuH}Heroes won't get any kill rewards in tournaments"), 
         PropertyOrder(2), Document, UsedImplicitly]
        public bool DisableKillRewardsInTournament { get; set; } = true;
        
        [LocDisplayName("{=ksB1FmZV}Disable Tracking Kills Tournament"),
         LocCategory("General", "{=C5T5nnix}General"),
         LocDescription("{=o5aH9rTO}Tournament kills / deaths won't be counted towards achievements or kill streaks"), 
         PropertyOrder(3), Document, UsedImplicitly]
        public bool DisableTrackingKillsTournament { get; set; } = true;
        #endregion

        #region Equipment
        [LocDisplayName("{=bBipANlh}No Horses"), 
         LocCategory("Equipment", "{=i7ZDVTaw}Equipment"), 
         LocDescription("{=sJ68yZs1}Remove horses completely from the BLT tournaments (the horse AI is terrible)"), 
         PropertyOrder(2), UsedImplicitly, Document]
        public bool NoHorses { get; set; } = true;
        
        [LocDisplayName("{=CYVy4Hhx}No Spears"), 
         LocCategory("Equipment", "{=i7ZDVTaw}Equipment"), 
         LocDescription("{=BjMC8Htn}Replaces all lances and spears with swords, because lance and spear combat is terrible"), 
         PropertyOrder(3), UsedImplicitly, Document]
        public bool NoSpears { get; set; } = true;
        
        [LocDisplayName("{=Ed55OyZ5}Normalize Armor"), 
         LocCategory("Equipment", "{=i7ZDVTaw}Equipment"), 
         LocDescription("{=kYFdZ8Yx}Replaces all armor with fixed tier armor, based on Culture if possible (tier specified by Normalized Armor Tier below)"), 
         PropertyOrder(4), UsedImplicitly, Document]
        public bool NormalizeArmor { get; set; }

        [LocDisplayName("{=xHJbcOaV}Normalize Armor Tier"),
         LocCategory("Equipment", "{=i7ZDVTaw}Equipment"),
         LocDescription("{=HnqCyrDD}Armor tier to set all contenstants to (1 to 6), if Normalize Armor is enabled"),
         PropertyOrder(5), Range(1, 6), UsedImplicitly, Document]
        public int NormalizeArmorTier { get; set; } = 3;

        [LocDisplayName("{=5Y08IsDl}Randomize Weapon Types"),
         LocCategory("Equipment", "{=i7ZDVTaw}Equipment"),
         LocDescription("{=oSCPOoqi}Randomizes the weapons used in each round, weighted based on the classes of the participants"), 
         PropertyOrder(6), UsedImplicitly, Document]
        public bool RandomizeWeaponTypes { get; set; } = true;
        #endregion
        
        #region Balancing
        [LocDisplayName("{=UCAbZYqU}Previous Winner Debuffs"),
         LocCategory("Balancing", "{=Zwh9GYUE}Balancing"),
         LocDescription("{=FrloGGew}Applies skill debuffers to previous tournament winners"),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)), PropertyOrder(1), UsedImplicitly, Document]
        public ObservableCollection<SkillDebuffDef> PreviousWinnerDebuffs { get; set; } = new() { new() };
        #endregion
        
        #region Round Types
        public class Round1Def
        {
            [LocDisplayName("{=WUQRg4yA}Vanilla"), 
             LocDescription("{=nfYBftnO}Allow the vanilla round setup"), 
             PropertyOrder(1), UsedImplicitly, Document]
            public bool EnableVanilla { get; set; } = true;
            [LocDisplayName("{=ErBd0iuP}1 Match 4 Teams"), 
             LocDescription("{=5E1lXDYh}Allow 1 match with 4 teams of 4"), 
             PropertyOrder(3), UsedImplicitly, Document]
            public bool Enable1Match4Teams { get; set; }
            [LocDisplayName("{=SsArdTT9}2 Matches 2 Teams"), 
             LocDescription("{=jEghMKUV}Allow 2 matches with 2 teams of 4"), 
             PropertyOrder(4), UsedImplicitly, Document]
            public bool Enable2Match2Teams { get; set; } 
            [LocDisplayName("{=qb1yo7DZ}2 Matches 4 Teams"), 
             LocDescription("{=f8rXSGwN}Allow 2 matches with 4 teams of 2"), 
             PropertyOrder(5), UsedImplicitly, Document]
            public bool Enable2Match4Teams { get; set; }
            [LocDisplayName("{=WAzvdvAJ}4 Matches 2 Teams"), 
             LocDescription("{=GPCYzshA}Allow 4 matches with 2 teams of 2"), 
             PropertyOrder(6), UsedImplicitly, Document]
            public bool Enable4Match2Teams { get; set; }
            [LocDisplayName("{=sMCb8Xeu}4 Matches 4 Teams"), 
             LocDescription("{=MjsTFwUv}Allow 4 matches with 4 teams of 1"), 
             PropertyOrder(7), UsedImplicitly, Document]
            public bool Enable4Match4Teams { get; set; }
            [LocDisplayName("{=URNM9UqS}8 Matches 2 Teams"), 
             LocDescription("{=otwzXPTQ}Allow 8 matches with 2 teams of 1"), 
             PropertyOrder(8), UsedImplicitly, Document]
            public bool Enable8Match2Teams { get; set; }

            public override string ToString()
            {
                var enabled = new List<string>();
                if(EnableVanilla) enabled.Add("{=WUQRg4yA}Vanilla".Translate());
                if(Enable1Match4Teams) enabled.Add("{=ErBd0iuP}1 Match 4 Teams".Translate());
                if(Enable2Match2Teams) enabled.Add("{=SsArdTT9}2 Matches 2 Teams".Translate()); 
                if(Enable2Match4Teams) enabled.Add("{=qb1yo7DZ}2 Matches 4 Teams".Translate());
                if(Enable4Match2Teams) enabled.Add("{=WAzvdvAJ}4 Matches 2 Teams".Translate());
                if(Enable4Match4Teams) enabled.Add("{=sMCb8Xeu}4 Matches 4 Teams".Translate());
                if(Enable8Match2Teams) enabled.Add("{=URNM9UqS}8 Matches 2 Teams".Translate());
                return string.Join(", ", enabled);
            }

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
            [LocDisplayName("{=WUQRg4yA}Vanilla"),
             LocDescription("{=nfYBftnO}Allow the vanilla round setup"), 
             PropertyOrder(1), UsedImplicitly, Document]
            public bool EnableVanilla { get; set; } = true;
            [LocDisplayName("{=uuBzCFgM}1 Match 2 Teams"),
             LocDescription("{=5hLSHeuv}Allow 1 match with 2 teams of 4"), 
             PropertyOrder(2), UsedImplicitly, Document]
            public bool Enable1Match2Teams { get; set; }
            [LocDisplayName("{=ErBd0iuP}1 Match 4 Teams"),
             LocDescription("{=CLbRKpZp}Allow 1 match with 4 teams of 2"), 
             PropertyOrder(3), UsedImplicitly, Document]
            public bool Enable1Match4Teams { get; set; }
            [LocDisplayName("{=SsArdTT9}2 Matches 2 Teams"),
             LocDescription("{=Dr1Js5fB}Allow 2 matches with 2 teams of 2"), 
             PropertyOrder(4), UsedImplicitly, Document]
            public bool Enable2Match2Teams { get; set; } 
            [LocDisplayName("{=qb1yo7DZ}2 Matches 4 Teams"),
             LocDescription("{=nmMzoqeC}Allow 2 matches with 4 teams of 1"), 
             PropertyOrder(5), UsedImplicitly, Document]
            public bool Enable2Match4Teams { get; set; }
            [LocDisplayName("{=WAzvdvAJ}4 Matches 2 Teams"),
             LocDescription("{=xMaA76xu}Allow 4 matches with 2 teams of 1"), 
             PropertyOrder(6), UsedImplicitly, Document]
            public bool Enable4Match2Teams { get; set; }
            
            public override string ToString()
            {
                var enabled = new List<string>();
                if(EnableVanilla) enabled.Add("{=WUQRg4yA}Vanilla".Translate());
                if(Enable1Match2Teams) enabled.Add("{=uuBzCFgM}1 Match 2 Teams".Translate());
                if(Enable1Match4Teams) enabled.Add("{=ErBd0iuP}1 Match 4 Teams".Translate());
                if(Enable2Match2Teams) enabled.Add("{=SsArdTT9}2 Matches 2 Teams".Translate()); 
                if(Enable2Match4Teams) enabled.Add("{=qb1yo7DZ}2 Matches 4 Teams".Translate());
                if(Enable4Match2Teams) enabled.Add("{=WAzvdvAJ}4 Matches 2 Teams".Translate());
                return string.Join(", ", enabled);
            }

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
            [LocDisplayName("{=WUQRg4yA}Vanilla"),
             LocDescription("{=nfYBftnO}Allow the vanilla round setup"), 
             PropertyOrder(1), UsedImplicitly, Document]
            public bool EnableVanilla { get; set; } = true;
            [LocDisplayName("{=uuBzCFgM}1 Match 2 Teams"),
             LocDescription("{=t9yLB8gB}Allow 1 match with 2 teams of 2"), 
             PropertyOrder(2), UsedImplicitly, Document]
            public bool Enable1Match2Teams { get; set; }
            [LocDisplayName("{=ErBd0iuP}1 Match 4 Teams"),
             LocDescription("{=YfOnzOH6}Allow 1 match with 4 teams of 1"), 
             PropertyOrder(3), UsedImplicitly, Document]
            public bool Enable1Match4Teams { get; set; }
            [LocDisplayName("{=SsArdTT9}2 Matches 2 Teams"),
             LocDescription("{=OrZaS458}Allow 2 matches with 2 teams of 1"), 
             PropertyOrder(4), UsedImplicitly, Document]
            public bool Enable2Match2Teams { get; set; } 
            
            public override string ToString()
            {
                var enabled = new List<string>();
                if(EnableVanilla) enabled.Add("{=WUQRg4yA}Vanilla".Translate());
                if(Enable1Match2Teams) enabled.Add("{=uuBzCFgM}1 Match 2 Teams".Translate());
                if(Enable1Match4Teams) enabled.Add("{=ErBd0iuP}1 Match 4 Teams".Translate());
                if(Enable2Match2Teams) enabled.Add("{=SsArdTT9}2 Matches 2 Teams".Translate()); 
                return string.Join(", ", enabled);
            }

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

        [LocDisplayName("{=eivyBgAa}Round 1 Type"), 
         LocCategory("Round Type", "{=KOIu6Q7d}Round Type"), 
         LocDescription("{=eivyBgAa}Round 1 Type"), 
         PropertyOrder(1), ExpandableObject, UsedImplicitly, Document]
        public Round1Def Round1Type { get; set; } = new();
        [LocDisplayName("{=pefX6BdQ}Round 2 Type"), 
         LocCategory("Round Type", "{=KOIu6Q7d}Round Type"), 
         LocDescription("{=pefX6BdQ}Round 2 Type"), 
         PropertyOrder(2), ExpandableObject,UsedImplicitly, Document]
        public Round2Def Round2Type { get; set; } = new();
        [LocDisplayName("{=GpV7pKPY}Round 3 Type"), 
         LocCategory("Round Type", "{=KOIu6Q7d}Round Type"), 
         LocDescription("{=GpV7pKPY}Round 3 Type"), 
         PropertyOrder(3), ExpandableObject,UsedImplicitly, Document]
        public Round3Def Round3Type { get; set; } = new();
        
        #endregion
        
        #region Round Rewards
        public class RoundRewardsDef
        {
            [LocDisplayName("{=IQTT5vYE}Win Gold"), 
             LocDescription("{=i7Ns7K2i}Gold won if the hero wins thier match in the round"), 
             PropertyOrder(1), UsedImplicitly, Document]
            public int WinGold { get; set; } = 10000;

            [LocDisplayName("{=h8I3PWkV}Win XP"), 
             LocDescription("{=b0MeEmOS}XP given if the hero wins thier match in the round"), 
             PropertyOrder(2), UsedImplicitly, Document]
            public int WinXP { get; set; } = 10000;

            [LocDisplayName("{=Vobr36Bl}Lose XP"), 
             LocDescription("{=Oq7LMvoF}XP given if the hero loses thier match in a round"), 
             PropertyOrder(3), UsedImplicitly, Document]
            public int LoseXP { get; set; } = 2500;
            
            public override string ToString() =>
                "{=IQTT5vYE}Win Gold".Translate() +
                $" {WinGold}, " +
                "{=h8I3PWkV}Win XP".Translate() +
                $" {WinXP}, " +
                "{=Vobr36Bl}Lose XP".Translate() +
                $" {LoseXP}";
        }
        
        [LocDisplayName("{=VeSh8k7c}Round 1 Rewards"),
         LocCategory("Round Rewards", "{=g0A4pXY2}Round Rewards"), 
         LocDescription("{=VeSh8k7c}Round 1 Rewards"), 
         PropertyOrder(1), ExpandableObject, UsedImplicitly, Document]
        public RoundRewardsDef Round1Rewards { get; set; } = new() { WinGold = 5000, WinXP = 5000, LoseXP = 5000 };
        [LocDisplayName("{=gQ9YX7my}Round 2 Rewards"),
         LocCategory("Round Rewards", "{=g0A4pXY2}Round Rewards"), 
         LocDescription("{=gQ9YX7my}Round 2 Rewards"), 
         PropertyOrder(2), ExpandableObject, UsedImplicitly, Document]
        public RoundRewardsDef Round2Rewards { get; set; } = new() { WinGold = 7500, WinXP = 7500, LoseXP = 7500 };
        [LocDisplayName("{=17pTC7NS}Round 3 Rewards"),
         LocCategory("Round Rewards", "{=g0A4pXY2}Round Rewards"), 
         LocDescription("{=17pTC7NS}Round 3 Rewards"), 
         PropertyOrder(3), ExpandableObject,UsedImplicitly, Document]
        public RoundRewardsDef Round3Rewards { get; set; } = new() { WinGold = 10000, WinXP = 10000, LoseXP = 10000 };
        [LocDisplayName("{=pxMWYe5J}Round 4 Rewards"),
         LocCategory("Round Rewards", "{=g0A4pXY2}Round Rewards"), 
         LocDescription("{=pxMWYe5J}Round 4 Rewards"), 
         PropertyOrder(4), ExpandableObject,UsedImplicitly, Document]
        public RoundRewardsDef Round4Rewards { get; set; } = new() { WinGold = 0, WinXP = 0, LoseXP = 0 };

        [YamlIgnore, Browsable(false)]
        public RoundRewardsDef[] RoundRewards => new[] { Round1Rewards, Round2Rewards, Round3Rewards, Round4Rewards };
        #endregion

        #region Rewards
        [LocDisplayName("{=IQTT5vYE}Win Gold"), 
         LocCategory("Rewards", "{=FHkvQbcR}Rewards"), 
         LocDescription("{=F8g49VCy}Gold won if the hero wins the tournaments"), 
         PropertyOrder(1), UsedImplicitly, Document]
        public int WinGold { get; set; } = 50000;

        [LocDisplayName("{=h8I3PWkV}Win XP"), 
         LocCategory("Rewards", "{=FHkvQbcR}Rewards"), 
         LocDescription("{=3BudXy0O}XP given if the hero wins the tournaments"), 
         PropertyOrder(2), UsedImplicitly, Document]
        public int WinXP { get; set; } = 50000;

        [LocDisplayName("{=5vMTYqdu}Participate XP"), 
         LocCategory("Rewards", "{=FHkvQbcR}Rewards"), 
         LocDescription("{=TXqjPXh7}XP given if the hero participates in a tournament but doesn't win"), 
         PropertyOrder(3), UsedImplicitly, Document]
        public int ParticipateXP { get; set; } = 10000;

        [LocDisplayName("{=Xu2KD7Kc}Prize"),
         LocCategory("Rewards", "{=FHkvQbcR}Rewards"),
         LocDescription("{=wYzmyUCE}Winners prize"), 
         PropertyOrder(4), ExpandableObject, Expand, UsedImplicitly, Document]
        public GeneratedRewardDef Prize { get; set; } = new()
        {
            ArmorWeight = 0.3f,
            WeaponWeight = 1f,
            MountWeight = 0.1f,
            Tier1Weight = 0,
            Tier2Weight = 0,
            Tier3Weight = 0,
            Tier4Weight = 0,
            Tier5Weight = 0,
            Tier6Weight = 1,
            CustomWeight = 1,
            CustomItemName = "{=hCNpHVJY}Prize {ITEMNAME}",
            CustomItemPower = 1,
        };
        #endregion

        #region Betting
        [LocDisplayName("{=rne7aMUR}Enable Betting"), 
         LocCategory("Betting", "{=n1Agm9uJ}Betting"), 
         LocDescription("{=FOQEPZD5}Enable betting"), 
         PropertyOrder(1), UsedImplicitly, Document]
        public bool EnableBetting { get; set; } = true;

        [LocDisplayName("{=njh9b5GB}Betting On Final Only"), 
         LocCategory("Betting", "{=n1Agm9uJ}Betting"), 
         LocDescription("{=KGcz71VJ}Only allow betting on the final betting"), 
         PropertyOrder(2), UsedImplicitly, Document]
        public bool BettingOnFinalOnly { get; set; }
        #endregion
        #endregion
        
        #region IDocumentable
        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.Div("tournament-config", () =>
            {
                generator.H1("{=AkDCrLgg}Tournament Config".Translate());
                DocumentationHelpers.AutoDocument(generator, this);
            });
        }
        #endregion
    }

    public class SkillDebuffDef : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [LocDisplayName("{=OEMBeawy}Skill"),
         LocDescription("{=twgHWqU6}Skill or skill group to modifer (all skills in a group will be modified)"), 
         PropertyOrder(1), UsedImplicitly, Document]
        public SkillsEnum Skill { get; set; } = SkillsEnum.All;

        [LocDisplayName("{=5a40vmYi}Skill Reduction Percent Per Win"),
         LocDescription("{=zoMHI9O3}Reduction to the skill per win (in %). See https://www.desmos.com/calculator/ajydvitcer for visualization of how skill will be modified."),
         PropertyOrder(2), UIRangeAttribute(0, 50, 0.5f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), UsedImplicitly, Document]
        public float SkillReductionPercentPerWin { get; set; } = 3.2f;
            
        [LocDisplayName("{=hMB4oFmk}Floor Percent"),
         LocDescription("{=HebJIfaE}The lower limit (in %) that the skill(s) can be reduced to."), 
         PropertyOrder(2), UIRangeAttribute(0, 100, 0.5f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), UsedImplicitly, Document]
        public float FloorPercent { get; set; } = 65f;

        [LocDisplayName("{=L7AKFlpb}Example"), 
         LocDescription("{=vgXMv8oH}Shows the % reduction of the skill over 20 tournaments"),
         PropertyOrder(3), ReadOnly(true), YamlIgnore, UsedImplicitly] 
        public string Example => string.Join(", ", 
            Enumerable.Range(0, 20)
                .Select(i => $"{i}: {100 * SkillModifier(i):0}%"));
            
        public float SkillModifier(int wins)
        {
            return (float) (FloorPercent + (100 - FloorPercent) * Math.Pow(1f - SkillReductionPercentPerWin / 100f, wins * wins)) / 100f;
        }

        public SkillModifierDef ToModifier(int wins)
        {
            return new SkillModifierDef
            {
                Skill = Skill,
                ModifierPercent = SkillModifier(wins) * 100,
            };
        }
            
        public override string ToString()
        {
            return "{=OEMBeawy}Skill".Translate() +
                   $": {Skill}, " +
                   "{=5a40vmYi}Skill Reduction Percent Per Win".Translate() +
                   $": {SkillReductionPercentPerWin}%, " +
                   "{=hMB4oFmk}Floor Percent".Translate() +
                   $": {FloorPercent}%";
        }
    }
}