using System;
using System.Collections.Generic;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;

namespace BLTAdoptAHero.Achievements
{
    public class AchievementStatsData
    {
        public enum Statistic
        {
            [LocDisplayName("{=FNK3LD2p}None")]
            None,
            [LocDisplayName("{=5RXxYH32}Total Kills")]
            TotalKills,
            [LocDisplayName("{=FqBQumbv}Total Hero Kills")]
            TotalHeroKills,
            [LocDisplayName("{=WMYfysby}Total Viewer Kills")]
            TotalViewerKills,
            [LocDisplayName("{=qPjwIfaF}Total Streamer Kills")]
            TotalStreamerKills,
            [LocDisplayName("{=ZTI6irGZ}Total Mount Kills")]
            TotalMountKills,
            [LocDisplayName("{=6IHF7HQb}Total Deaths")]
            TotalDeaths,
            [LocDisplayName("{=UHvCzzit}Total Hero Deaths")]
            TotalHeroDeaths,
            [LocDisplayName("{=mhcnLFBv}Total Viewer Deaths")]
            TotalViewerDeaths,
            [LocDisplayName("{=rKVsbROz}Total Streamer Deaths")]
            TotalStreamerDeaths,
            [LocDisplayName("{=6N8KiovH}Total Mount Deaths")]
            TotalMountDeaths,
            [LocDisplayName("{=YDiK48rp}Summons")]
            Summons,
            [LocDisplayName("{=facIgBFe}Attacks")]
            Attacks,
            [LocDisplayName("{=yTIq9zWS}Battles")]
            Battles,
            [LocDisplayName("{=VwyXMINi}Consecutive Summons")]
            ConsecutiveSummons,
            [LocDisplayName("{=Ju3c6Iz4}Consecutive Attacks")]
            ConsecutiveAttacks,
            [LocDisplayName("{=dRD94FMl}TotalTournament Round Wins")]
            TotalTournamentRoundWins,
            [LocDisplayName("{=GPMB5BPI}TotalTournament Round Losses")]
            TotalTournamentRoundLosses,
            [LocDisplayName("{=WdZWU9GV}TotalTournament Final Wins")]
            TotalTournamentFinalWins,
        }

        public Dictionary<Statistic, int> TotalStats { get; set; } = new();

        public Dictionary<(Guid, Statistic), int> ClassStats { get; set; } = new();

        public List<Guid> Achievements { get; set; } = new();

        // Update class and total stats together
        public void UpdateValue(Statistic type, Guid classId, int amount)
        {
            TotalStats.AddInt(type, amount);
            ClassStats.AddInt((classId, type), amount);

            if (type is Statistic.Summons)
            {
                TotalStats.AddInt(Statistic.ConsecutiveSummons, amount);
                ClassStats.AddInt((classId, Statistic.ConsecutiveSummons), amount);
                // Reset consecutive attacks, now that hero summoned
                TotalStats[Statistic.ConsecutiveAttacks] = 0;
                ClassStats[(classId, Statistic.ConsecutiveAttacks)] = 0;
            }
            else if (type is Statistic.Attacks)
            {
                TotalStats.AddInt(Statistic.ConsecutiveAttacks, amount);
                ClassStats.AddInt((classId, Statistic.ConsecutiveAttacks), amount);
                // Reset consecutive summons, now that hero attacked
                TotalStats[Statistic.ConsecutiveSummons] = 0;
                ClassStats[(classId, Statistic.ConsecutiveSummons)] = 0;
            }
            if (type is Statistic.Summons or Statistic.Attacks)
            {
                TotalStats.AddInt(Statistic.Battles, amount);
                ClassStats.AddInt((classId, Statistic.Battles), amount);
            }
        }

        public int GetTotalValue(Statistic type) => TotalStats.GetInt(type);

        public int GetClassValue(Statistic type, Guid classId) => ClassStats.GetInt((classId, type));
    }
}