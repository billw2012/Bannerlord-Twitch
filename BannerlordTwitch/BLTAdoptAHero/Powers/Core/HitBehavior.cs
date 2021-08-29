using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
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
        [LocDisplayName("{=mtZMAwbM}Knock Back Chance Percent"),
         LocDescription("{=wPgAMF92}Percent chance (0 to 100) for target to get knocked back"), 
         UIRange(0, 100, 1),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         PropertyOrder(1), UsedImplicitly]
        public float KnockBackChancePercent { get; set; }
        [LocDisplayName("{=9dla8vV1}Knock Down Chance Percent"),
         LocDescription("{=MTqyFfDc}Percent chance (0 to 100) for target to get knocked down"), 
         UIRange(0, 100, 1),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         PropertyOrder(2), UsedImplicitly]
        public float KnockDownChancePercent { get; set; }
        [LocDisplayName("{=dCRbuRf8}Shrug Off Chance Percent"),
         LocDescription("{=adHeasi7}Percent chance (0 to 100) for target to shrug off blow (not visably reacting to it, damage will still apply)"), 
         UIRange(0, 100, 1),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         PropertyOrder(4), UsedImplicitly]
        public float ShrugOffChancePercent { get; set; }
        [LocDisplayName("{=okiEp8HU}Makes Rear Chance Percent"),
         LocDescription("{=Cp9IZN6J}Percent chance (0 to 100) for target to rear (if it is a mount)"), 
         UIRange(0, 100, 1),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         PropertyOrder(5), UsedImplicitly]
        public float MakesRearChancePercent { get; set; }
        [LocDisplayName("{=lVXn7z6U}Dismount Chance"),
         LocDescription("{=RrhP0L2t}Percent chance (0 to 100) for target to be dismounted"), 
         UIRange(0, 100, 1),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), 
         PropertyOrder(6), UsedImplicitly]
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
            if(KnockBackChancePercent > 0) 
                flags.Add($"{KnockBackChancePercent:0}% " + "{=2UuwaRcc}Knock Back".Translate());
            if(KnockDownChancePercent > 0) 
                flags.Add($"{KnockDownChancePercent:0}% " + "{=4iroX5zj}Knock Down".Translate());
            if(ShrugOffChancePercent > 0) 
                flags.Add($"{ShrugOffChancePercent:0}% " + "{=DNqblCEf}Shrug Off".Translate());
            if(MakesRearChancePercent > 0) 
                flags.Add($"{MakesRearChancePercent:0}% " + "{=F0o4A3mp}Mount Rear".Translate());
            if(DismountChance > 0) 
                flags.Add($"{DismountChance:0}% " + "{=bEO48IR4}Dismount".Translate());
            return !flags.Any() ? "{=cM8NOj2B}(inactive)".Translate() : string.Join(", ", flags);
        }

        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            if(KnockBackChancePercent > 0) 
                generator.PropertyValuePair("{=2UuwaRcc}Knock Back".Translate(), $"{KnockBackChancePercent:0}%");
            if(KnockDownChancePercent > 0) 
                generator.PropertyValuePair("{=4iroX5zj}Knock Down".Translate(), $"{KnockDownChancePercent:0}%");
            if(ShrugOffChancePercent > 0) 
                generator.PropertyValuePair("{=DNqblCEf}Shrug Off".Translate(), $"{ShrugOffChancePercent:0}%");
            if(MakesRearChancePercent > 0) 
                generator.PropertyValuePair("{=F0o4A3mp}Mount Rear".Translate(), $"{MakesRearChancePercent:0}%");
            if(DismountChance > 0) 
                generator.PropertyValuePair("{=bEO48IR4}Dismount".Translate(), $"{DismountChance:0}%");
        }
    }
}