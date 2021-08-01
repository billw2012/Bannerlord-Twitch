using System;
using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace BLTAdoptAHero.Achievements
{
    public class AchievementStatsData
    {
        public enum Statistic
        {
            None,
            TotalKills,
            TotalHeroKills,
            TotalViewerKills,
            TotalStreamerKills,
            TotalMountKills,
            TotalDeaths,
            TotalHeroDeaths,
            TotalViewerDeaths,
            TotalStreamerDeaths,
            TotalMountDeaths,
            Summons,
            Attacks,
            ConsecutiveSummons,
            ConsecutiveAttacks,
            TotalTournamentRoundWins,
            TotalTournamentRoundLosses,
            TotalTournamentFinalWins,
        }

        [SaveableProperty(0)]
        public Dictionary<Statistic, int> TotalStats { get; set; } = new();

        [SaveableProperty(1)]
        public Dictionary<(Guid, Statistic), int> ClassStats { get; set; } = new();

        [SaveableProperty(2)]
        public List<Guid> Achievements { get; set; } = new();

        // Update class and total stats together
        public void UpdateValue(Statistic type, Guid classId, int amount)
        {
            TotalStats[type] += amount;
            ClassStats[(classId, type)] += amount;

            if (type == Statistic.Summons)
            {
                TotalStats[Statistic.ConsecutiveSummons] += amount;
                ClassStats[(classId, Statistic.ConsecutiveSummons)] += amount;
                TotalStats[Statistic.ConsecutiveAttacks] = 0;
                ClassStats[(classId, Statistic.ConsecutiveAttacks)] = 0;
            }
            else if (type == Statistic.Attacks)
            {
                TotalStats[Statistic.ConsecutiveAttacks] += amount;
                ClassStats[(classId, Statistic.ConsecutiveAttacks)] += amount;
                TotalStats[Statistic.ConsecutiveSummons] = 0;
                ClassStats[(classId, Statistic.ConsecutiveSummons)] = 0;
            }
        }

        public int GetTotalValue(Statistic type) => TotalStats[type];

        public int GetClassValue(Statistic type, Guid classId) => ClassStats[(classId, type)];
    }
}