using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    public struct HitBehavior : IDocumentable
    {
        [Description("Percent chance (0 to 100) for target to get knocked back"), PropertyOrder(1),
         UIRange(0, 100, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float KnockBackChancePercent { get; set; }
        [Description("Percent chance (0 to 100) for target to get knocked down"), PropertyOrder(2),
         UIRange(0, 100, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float KnockDownChancePercent { get; set; }
        [Description("Percent chance (0 to 100) for target to shrug off blow (not visably reacting to it, " +
                     "damage will still apply)"), PropertyOrder(4),
         UIRange(0, 100, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float ShrugOffChancePercent { get; set; }
        [Description("Percent chance (0 to 100) for target to rear (if it is a mount)"), PropertyOrder(5),
         UIRange(0, 100, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float MakesRearChancePercent { get; set; }
        [Description("Percent chance (0 to 100) for target to be dismounted"), PropertyOrder(6),
         UIRange(0, 100, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float DismountChance { get; set; }

        [YamlIgnore, Browsable(false)]
        public bool IsEnabled => KnockBackChancePercent > 0 ||
                                 KnockDownChancePercent > 0 ||
                                 ShrugOffChancePercent > 0 ||
                                 MakesRearChancePercent > 0 ||
                                 DismountChance > 0;

        private const BlowFlags BehaviorFlags = BlowFlags.KnockBack | BlowFlags.KnockDown | BlowFlags.ShrugOff |
                                                BlowFlags.MakesRear | BlowFlags.CanDismount;

        public BlowFlags AddFlags(Agent agent, BlowFlags flags)
        {
            if(MBRandom.RandomFloat * 100f < KnockBackChancePercent && !agent.IsMount) flags = flags & ~BehaviorFlags | BlowFlags.KnockBack;
            else if(MBRandom.RandomFloat * 100f < KnockDownChancePercent && !agent.IsMount) flags = flags & ~BehaviorFlags | BlowFlags.KnockDown;
            else if(MBRandom.RandomFloat * 100f < ShrugOffChancePercent && !agent.IsMount) flags = flags & ~BehaviorFlags | BlowFlags.ShrugOff;
            else if(MBRandom.RandomFloat * 100f < MakesRearChancePercent && agent.IsMount) flags = flags & ~BehaviorFlags | BlowFlags.MakesRear;
            else if(MBRandom.RandomFloat * 100f < DismountChance && agent.HasMount) flags = flags & ~BehaviorFlags | BlowFlags.CanDismount;
            return flags;
        }
        
        public BlowFlags RemoveFlags(Agent agent, BlowFlags flags)
        {
            if(MBRandom.RandomFloat * 100f < KnockBackChancePercent && !agent.IsMount) flags &= ~BlowFlags.KnockBack;
            if(MBRandom.RandomFloat * 100f < KnockDownChancePercent && !agent.IsMount) flags &= ~BlowFlags.KnockDown;
            if(MBRandom.RandomFloat * 100f < ShrugOffChancePercent && !agent.IsMount) flags &= ~BlowFlags.ShrugOff;
            if(MBRandom.RandomFloat * 100f < MakesRearChancePercent && agent.IsMount) flags &= ~BlowFlags.MakesRear;
            if(MBRandom.RandomFloat * 100f < DismountChance && agent.HasMount) flags &= ~BlowFlags.CanDismount;
            return flags;
        }

        public override string ToString()
        {
            var flags = new List<string>();
            if(KnockBackChancePercent > 0) flags.Add($"{KnockBackChancePercent:0}% {nameof(BlowFlags.KnockBack).SplitCamelCase()}");
            if(KnockDownChancePercent > 0) flags.Add($"{KnockDownChancePercent:0}% {nameof(BlowFlags.KnockDown).SplitCamelCase()}");
            if(ShrugOffChancePercent > 0) flags.Add($"{ShrugOffChancePercent:0}% {nameof(BlowFlags.ShrugOff).SplitCamelCase()}");
            if(MakesRearChancePercent > 0) flags.Add($"{MakesRearChancePercent:0}% {nameof(BlowFlags.MakesRear).SplitCamelCase()}");
            if(DismountChance > 0) flags.Add($"{DismountChance:0}% {nameof(BlowFlags.CanDismount).SplitCamelCase()}");
            return !flags.Any() ? "(inactive)" : string.Join(", ", flags);
        }

        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            if(KnockBackChancePercent > 0) generator.PropertyValuePair(nameof(KnockBackChancePercent).SplitCamelCase(), $"{KnockBackChancePercent:0}%");
            if(KnockDownChancePercent > 0) generator.PropertyValuePair(nameof(KnockDownChancePercent).SplitCamelCase(), $"{KnockDownChancePercent:0}%");
            if(ShrugOffChancePercent > 0) generator.PropertyValuePair(nameof(ShrugOffChancePercent).SplitCamelCase(), $"{ShrugOffChancePercent:0}%");
            if(MakesRearChancePercent > 0) generator.PropertyValuePair(nameof(MakesRearChancePercent).SplitCamelCase(), $"{MakesRearChancePercent:0}%");
            if(DismountChance > 0) generator.PropertyValuePair(nameof(DismountChance).SplitCamelCase(), $"{DismountChance:0}%");
        }
    }
}