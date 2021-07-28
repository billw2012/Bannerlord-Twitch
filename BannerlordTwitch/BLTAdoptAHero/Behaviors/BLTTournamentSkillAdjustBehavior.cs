using System;
using System.Linq;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Actions.Util;
using BLTAdoptAHero.Annotations;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    [HarmonyPatch]
    public class BLTTournamentSkillAdjustBehavior : AutoMissionBehavior<BLTTournamentSkillAdjustBehavior>
    {
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            // Can modify skills here for the tournament
            
            // if (agent.IsHuman && UnarmedRound)
            // {
            //     agent.AgentDrivenProperties.SetStat(DrivenProperty,
            //         agent.AgentDrivenProperties.SetStat(DrivenProperty.SwingSpeedMultiplier) * 10
            //         );
            // }
        }
        
        public bool UnarmedRound { get; set; }

        public override void OnRegisterBlow(Agent attacker, Agent victim, GameEntity realHitEntity, Blow b,
            ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon)
        {
            if (UnarmedRound && victim != null && attacker != null)
            {
                var blow = new Blow(attacker.Index)
                {
                    DamageType = DamageTypes.Blunt,
                    BoneIndex = victim.Monster.HeadLookDirectionBoneIndex,
                    Position = victim.Position,
                    BaseMagnitude = collisionData.BaseMagnitude * 10,
                    SwingDirection = collisionData.WeaponBlowDir,
                    Direction = collisionData.WeaponBlowDir,
                    DamageCalculated = true,
                    VictimBodyPart = BoneBodyPartType.Chest,
                };

                blow.InflictedDamage = (int) blow.BaseMagnitude;

                victim.RegisterBlow(blow);
            }
        }

        public static int GetModifiedSkill(Hero hero, SkillObject skill, int baseModifiedSkill)
        {
            if (baseModifiedSkill == 0) return 0;
            
            var debuff = BLTAdoptAHeroModule.TournamentConfig.PreviousWinnerDebuffs
                .FirstOrDefault(d => SkillGroup.GetSkills(d.Skill).Contains(skill));
            if (debuff != null)
            {
                int tournamentWins = BLTAdoptAHeroCampaignBehavior.Current.GetAchievementStat(hero,
                    AchievementSystem.AchievementTypes.TotalTournamentChampionships);
                if (tournamentWins > 0)
                {
                    return (int) (baseModifiedSkill * debuff.SkillModifier(tournamentWins));
                }
            }

            return baseModifiedSkill;
        }
        
                
        [UsedImplicitly, HarmonyPostfix, HarmonyPatch(typeof(CharacterObject), nameof(CharacterObject.GetSimulationAttackPower))]
        public static void GetSimulationAttackPowerPostfix(ref float attackPoints, ref float defencePoints)
        {
            // Make sure simulation of unarmed rounds doesn't break (simulation can happen if 
            // tournament round is skipped at the start or part way through)
            // We won't bother to actually confirm that we are in an Unarmed round, as the game
            // will enter an infinite loop if attackPoints is EVER 0, so we will correct it regardless.
            if (attackPoints == 0)
            {
                attackPoints = 10;
            }
        }
    }
}