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
        public void UpdateValue(Statistic type, Guid classId, int amount, bool forced = false)
        {
            TotalStats.AddInt(type, amount);
            ClassStats.AddInt((classId, type), amount);

            // Here we update the consecutive streak, optionally resetting if the viewer is switching sides.
            // If the summon/attack is forced (i.e. the viewer hero was already in the battle parties) then we don't
            // change the consecutive stats if it would cause a reset.
            void UpdateStatPair(Statistic stat, Statistic otherStat)
            {
                if (!forced || TotalStats.GetInt(otherStat) == 0)
                {
                    TotalStats.AddInt(stat, amount);
                    TotalStats[otherStat] = 0;
                }
                if (!forced || ClassStats.GetInt((classId, otherStat)) == 0)
                {
                    ClassStats.AddInt((classId, stat), amount);
                    ClassStats[(classId, otherStat)] = 0;
                }
            }

            if (type is Statistic.Summons)
            {
                UpdateStatPair(Statistic.ConsecutiveSummons, Statistic.ConsecutiveAttacks);
            }
            else if (type is Statistic.Attacks)
            {
                UpdateStatPair(Statistic.ConsecutiveAttacks, Statistic.ConsecutiveSummons);
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