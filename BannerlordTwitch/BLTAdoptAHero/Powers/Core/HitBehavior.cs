using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BannerlordTwitch;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Powers
{
    public struct HitBehavior : IDocumentable
    {
        [Description("Chance (0 to 1) for target to get knocked back"), PropertyOrder(1),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float KnockBackChance { get; set; }
        [Description("Chance (0 to 1) for target to get knocked down"), PropertyOrder(2),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float KnockDownChance { get; set; }
        // [Description("Chance (0 to 1) for hit to crush through targets block"), PropertyOrder(3), UsedImplicitly]
        // public float CrushThroughChance { get; set; }
        [Description("Chance (0 to 1) for target to shrug off blow (not visably reacting to it, " +
                     "damage will still apply)"), PropertyOrder(4),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float ShrugOffChance { get; set; }
        [Description("Chance (0 to 1) for target to rear (if it is a mount)"), PropertyOrder(5),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float MakesRearChance { get; set; }
        [Description("Chance (0 to 1) for target to be dismounted"), PropertyOrder(6),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         UsedImplicitly]
        public float DismountChance { get; set; }

        public BlowFlags Generate(Agent agent)
        {
            var flags = BlowFlags.None;
            if(MBRandom.RandomFloat < KnockBackChance && !agent.IsMount) flags |= BlowFlags.KnockBack;
            if(MBRandom.RandomFloat < KnockDownChance && !agent.IsMount) flags |= BlowFlags.KnockDown;
            //if(MBRandom.RandomFloat < CrushThroughChance) flags |= BlowFlags.CrushThrough;
            if(MBRandom.RandomFloat < ShrugOffChance && !agent.IsMount) flags |= BlowFlags.ShrugOff;
            if(MBRandom.RandomFloat < MakesRearChance && agent.IsMount) flags |= BlowFlags.MakesRear;
            if(MBRandom.RandomFloat < DismountChance && agent.HasMount) flags |= BlowFlags.CanDismount;
            return flags;
        }

        public override string ToString()
        {
            var flags = new List<string>();
            if(KnockBackChance > 0) flags.Add($"{KnockBackChance*100:0}% {nameof(BlowFlags.KnockBack).SplitCamelCase()}");
            if(KnockDownChance > 0) flags.Add($"{KnockDownChance*100:0}% {nameof(BlowFlags.KnockDown).SplitCamelCase()}");
            //if(CrushThroughChance > 0) flags.Add($"{CrushThroughChance*100:0}% {nameof(BlowFlags.CrushThrough).SplitCamelCase()}");
            if(ShrugOffChance > 0) flags.Add($"{ShrugOffChance*100:0}% {nameof(BlowFlags.ShrugOff).SplitCamelCase()}");
            if(MakesRearChance > 0) flags.Add($"{MakesRearChance*100:0}% {nameof(BlowFlags.MakesRear).SplitCamelCase()}");
            if(DismountChance > 0) flags.Add($"{DismountChance*100:0}% {nameof(BlowFlags.CanDismount).SplitCamelCase()}");
            return string.Join(", ", flags);
        }

        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            if(KnockBackChance > 0) generator.PropertyValuePair(nameof(KnockBackChance).SplitCamelCase(), $"{KnockBackChance * 100:0}%");
            if(KnockDownChance > 0) generator.PropertyValuePair(nameof(KnockDownChance).SplitCamelCase(), $"{KnockDownChance * 100:0}%");
            //if(CrushThroughChance > 0) generator.PropertyValuePair(nameof(CrushThroughChance).SplitCamelCase(), $"{CrushThroughChance * 100:0}%");
            if(ShrugOffChance > 0) generator.PropertyValuePair(nameof(ShrugOffChance).SplitCamelCase(), $"{ShrugOffChance * 100:0}%");
            if(MakesRearChance > 0) generator.PropertyValuePair(nameof(MakesRearChance).SplitCamelCase(), $"{MakesRearChance * 100:0}%");
            if(DismountChance > 0) generator.PropertyValuePair(nameof(DismountChance).SplitCamelCase(), $"{DismountChance * 100:0}%");
        }
    }
}