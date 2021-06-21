using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox;
using SandBox.Source.Missions.Handlers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Actions
{
    [Description("Spawns the adopted hero's retinue into the current active mission")]
    internal class SummonRetinue : HeroActionHandlerBase
    {
        private delegate Agent MissionAgentHandler_SpawnWanderingAgentDelegate(
            MissionAgentHandler instance,
            LocationCharacter locationCharacter,
            MatrixFrame spawnPointFrame,
            bool hasTorch,
            bool noHorses);

        private static readonly MissionAgentHandler_SpawnWanderingAgentDelegate MissionAgentHandler_SpawnWanderingAgent
            = (MissionAgentHandler_SpawnWanderingAgentDelegate)AccessTools.Method(typeof(MissionAgentHandler),
                    "SpawnWanderingAgent", new[] { typeof(LocationCharacter), typeof(MatrixFrame), typeof(bool), typeof(bool) })
                .CreateDelegate(typeof(MissionAgentHandler_SpawnWanderingAgentDelegate));

        private delegate MatrixFrame ArenaPracticeFightMissionController_GetSpawnFrameDelegate(
            ArenaPracticeFightMissionController instance, bool considerPlayerDistance, bool isInitialSpawn);

        private static readonly ArenaPracticeFightMissionController_GetSpawnFrameDelegate ArenaPracticeFightMissionController_GetSpawnFrame = (ArenaPracticeFightMissionController_GetSpawnFrameDelegate)
            AccessTools.Method(typeof(ArenaPracticeFightMissionController), "GetSpawnFrame", new[] { typeof(bool), typeof(bool) })
                .CreateDelegate(typeof(ArenaPracticeFightMissionController_GetSpawnFrameDelegate));
        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            var existingHero = BLTSummonBehavior.Current.GetSummonedHero(adoptedHero);
            bool firstSummon = existingHero == null;
            var activeAgent = Mission.Current?.Agents?.FirstOrDefault(a =>
                                        a.IsActive() && a.Character == adoptedHero.CharacterObject);
            if (activeAgent == null || activeAgent.IsActive() == false) { return; }
            if (!firstSummon) { return; }
            float actualBoost = context.IsSubscriber ? Math.Max(2, 1) : 1;
            var retinueTroops = BLTAdoptAHeroCampaignBehavior.Current.GetRetinue(adoptedHero).ToList();
            var agent_name = AccessTools.Field(typeof(Agent), "_name");

            var party = true switch
            {
                true when Mission.Current?.PlayerTeam != null &&
                          Mission.Current?.PlayerTeam?.ActiveAgents.Any() == true => PartyBase.MainParty,
                false when Mission.Current?.PlayerEnemyTeam != null &&
                           Mission.Current?.PlayerEnemyTeam.ActiveAgents.Any() == true => Mission.Current
                    .PlayerEnemyTeam?.TeamAgents?.Select(a => a.Origin?.BattleCombatant as PartyBase)
                    .Where(p => p != null)
                    .SelectRandom(),
                _ => null
            };
            existingHero = BLTSummonBehavior.Current.AddSummonedHero(adoptedHero, true, FormationClass.Infantry, party);

            if (existingHero is { State: AgentState.Active })
            {
                onFailure($"You cannot be summoned, you are already here!");
                return;
            }
            foreach (var retinueTroop in retinueTroops)
            {
                bool isMounted = Mission.Current.Mode != MissionMode.Stealth
                                     && !MissionHelpers.InSiegeMission();
                // Don't modify formation for non-player side spawn as we don't really care
                bool hasPrevFormation = Campaign.Current.PlayerFormationPreferences
                                            .TryGetValue(retinueTroop, out var prevFormation)
                                        && BLTAdoptAHeroModule.CommonConfig.RetinueUseHeroesFormation;
                int formationTroopIdx = 0;
                int totalTroopsCount = retinueTroops.Count;
                var troopOrigin = existingHero.Party;

                existingHero.Party.MemberRoster.AddToCounts(retinueTroop, 1);
                var retinueAgent = Mission.Current.SpawnTroop(
                    new PartyAgentOrigin(troopOrigin, retinueTroop),
                    isPlayerSide: true,
                    hasFormation: true,
                    spawnWithHorse: retinueTroop.IsMounted
                                    && (isMounted || !BLTAdoptAHeroModule.CommonConfig.RetinueUseHeroesFormation),
                    isReinforcement: true,
                    enforceSpawningOnInitialPoint: false,
                    formationTroopCount: totalTroopsCount,
                    formationTroopIndex: formationTroopIdx++,
                    isAlarmed: true,
                    wieldInitialWeapons: true);

                existingHero.Retinue.Add(new BLTSummonBehavior.RetinueState
                {
                    Troop = retinueTroop,
                    Agent = retinueAgent,
                    State = AgentState.Active,
                });
                agent_name.SetValue(retinueAgent,
                                new TextObject($"{retinueAgent.Name} ({context.UserName})"));

                retinueAgent.BaseHealthLimit *= Math.Max(1,
                    BLTAdoptAHeroModule.CommonConfig.StartRetinueHealthMultiplier);
                retinueAgent.HealthLimit *= Math.Max(1,
                    BLTAdoptAHeroModule.CommonConfig.StartRetinueHealthMultiplier);
                retinueAgent.Health *= Math.Max(1,
                    BLTAdoptAHeroModule.CommonConfig.StartRetinueHealthMultiplier);

                BLTAdoptAHeroCustomMissionBehavior.Current.AddListeners(retinueAgent,
                    onGotAKill: (killer, killed, state) =>
                    {
                        Log.Trace($"[{nameof(SummonHero)}] {retinueAgent.Name} killed {killed?.ToString() ?? "unknown"}");
                        BLTAdoptAHeroCommonMissionBehavior.Current.ApplyKillEffects(
                            adoptedHero, killer, killed, state,
                            BLTAdoptAHeroModule.CommonConfig.RetinueGoldPerKill,
                            BLTAdoptAHeroModule.CommonConfig.RetinueHealPerKill,
                            0,
                            actualBoost,
                            BLTAdoptAHeroModule.CommonConfig.RelativeLevelScaling,
                            BLTAdoptAHeroModule.CommonConfig.LevelScalingCap
                        );
                    }
                );

                if (hasPrevFormation)
                {
                    Campaign.Current.SetPlayerFormationPreference(retinueTroop, prevFormation);
                }
            }
        }
    }
}