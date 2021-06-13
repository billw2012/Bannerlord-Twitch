using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BLTAdoptAHero.Annotations;
using TaleWorlds.Library;
using Color = System.Windows.Media.Color;

namespace BLTAdoptAHero.Behaviors
{
    public partial class MissionInfoPanel
    {
        public MissionInfoPanel()
        {
            InitializeComponent();
            // HeroList.ItemsSource = new List<HeroViewModel>
            // {
            //     new()
            //     {
            //         Name = "TestHero",
            //         IsPlayerSide = true,
            //         IsRouted = false,
            //         IsUnconscious = false,
            //         IsKilled = false,
            //         MaxHP = 100,
            //         HP = 50,
            //         Kills = 12,
            //         Retinue = 7,
            //         RetinueKills = 42
            //     }
            // };
        }
    }

    internal class HeroViewModel : IComparable<HeroViewModel>, IComparable, INotifyPropertyChanged
    {
        public int CompareTo(HeroViewModel other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            int isPlayerSideComparison = -IsPlayerSide.CompareTo(other.IsPlayerSide);
            if (isPlayerSideComparison != 0) return isPlayerSideComparison;
            int killsComparison = other.Kills.CompareTo(Kills);
            if (killsComparison != 0) return killsComparison;
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is HeroViewModel other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(HeroViewModel)}");
        }

        public string Name { get; set; }

        public bool IsPlayerSide { get; set; }

        public bool IsRouted { get; set; }

        public bool IsUnconscious { get; set; }

        public bool IsKilled { get; set; }

        public float MaxHP { get; set; }

        public float HP { get; set; }

        public int Kills { get; set; }

        public int GlobalSortKey => (IsPlayerSide ? 10000 : 0) + Kills;

        public string KillsText => Kills == 0 ? string.Empty : Kills.ToString();
        public Visibility KillsVisibility => Kills > 0 ? Visibility.Visible : Visibility.Hidden;

        public int Retinue { get; set; }

        public List<object> RetinueList => Enumerable.Repeat<object>(null, Retinue).ToList();

        public int RetinueKills { get; set; }

        public string RetinueKillsText => RetinueKills == 0 ? string.Empty : $"+{RetinueKills}";
        public Visibility RetinueKillsVisibility => RetinueKills > 0 ? Visibility.Visible : Visibility.Hidden;

        public float CooldownFractionRemaining { get; set; }
        public float CooldownSecondsRemaining { get; set; }

        public Visibility CooldownClockVisibility =>
            CooldownSecondsRemaining >= 3 ? Visibility.Visible : Visibility.Hidden;
        public float CooldownEndAngle => CooldownFractionRemaining * 360;

        public string CooldownTimeoutText => MathF.Ceiling(CooldownSecondsRemaining).ToString();
        public Visibility CooldownTextVisibility =>
            CooldownSecondsRemaining is > 0 and < 3 ? Visibility.Visible : Visibility.Hidden;

        public float CooldownTextScale => 1 + CooldownSecondsRemaining % 1;  
        public int GoldEarned { get; set; }
        public string GoldEarnedText
        {
            get
            {
                int goldEarnedK = GoldEarned / 1000;
                return goldEarnedK == 0 ? string.Empty : $"{goldEarnedK}k";
            }
        }

        public int XPEarned { get; set; }
        public string XPEarnedText
        {
            get
            {
                int xpEarnedK = XPEarned / 1000;
                return xpEarnedK == 0 ? string.Empty : $"{xpEarnedK}k";
            }
        }

        public Brush TextColor => IsRouted
            ? Brushes.Yellow
            : IsKilled
                ? Brushes.Crimson
                : IsUnconscious
                    ? Brushes.Orange
                    : Brushes.Azure;

        public Brush ProgressBarForeground => IsPlayerSide 
            ? new SolidColorBrush(Color.FromArgb(0xFF, 0x66, 0x66, 0xCC))
            : new SolidColorBrush(Color.FromArgb(0xFF, 0xAA, 0x32, 0x77))
        ;
        public Brush ProgressBarBackground => IsPlayerSide 
            ? new SolidColorBrush(Color.FromArgb(0xFF, 0x20, 0x20, 0x50))
            : new SolidColorBrush(Color.FromArgb(0xFF, 0x40, 0x11, 0x22))
        ;
        
        [UsedImplicitly]
        #pragma warning disable 0067
        public event PropertyChangedEventHandler PropertyChanged;
    }
}