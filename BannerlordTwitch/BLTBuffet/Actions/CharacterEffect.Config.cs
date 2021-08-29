using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTBuffet
{
    public partial class CharacterEffect
    {
        internal enum Target
        {
            [LocDisplayName("{=92tZqqRy}Player")]
            Player,
            [LocDisplayName("{=jyNFgLUD}Adopted Hero")]
            AdoptedHero,
            [LocDisplayName("{=qVaXXiEf}Any")]
            Any,
            [LocDisplayName("{=Dc99H7LD}Enemy Team")]
            EnemyTeam,
            [LocDisplayName("{=mWL1kGwD}Player Team")]
            PlayerTeam,
            [LocDisplayName("{=8rqexAvY}Ally Team")]
            AllyTeam,
            [LocDisplayName("{=o3jTIv2S}Random")]
            Random,
        }
        
        [LocDisplayName("{=F9u231DX}Particle Effect Definition")]
        internal class ParticleEffectDef
        {
            [LocDisplayName("{=uUzmy7Lh}Name"),
             LocDescription("{=8FGAgHIX}Particle effect system name, see ParticleEffects.txt for the full vanilla list"),
             ItemsSource(typeof(LoopingParticleEffectItemSource)),
             PropertyOrder(1), UsedImplicitly]
            public string Name { get; set; }

            public enum AttachPointEnum
            {
                [LocDisplayName("{=ry3Fo3Ph}On Weapon")] 
                OnWeapon,
                [LocDisplayName("{=zfQ43utW}On Hands")] 
                OnHands,
                [LocDisplayName("{=P9e57ey1}On Head")] 
                OnHead,
                [LocDisplayName("{=GZC5VUo0}On Body")] 
                OnBody,
            }

            [LocDisplayName("{=7MIO0dPq}Attach Point"),
             LocDescription("{=jEEtBaKs}Where to attach the particles"), 
             PropertyOrder(2), UsedImplicitly]
            public AttachPointEnum AttachPoint { get; set; }

            // [Description("Apply the effect to the weapon")]
            // public bool OnWeapon { get; set; }
            // [Description("Apply the effect to the hands")]
            // public bool OnHands { get; set; }
            // [Description("Apply the effect to the head")]
            // public bool OnHead { get; set; }
            // [Description("Apply the effect to the whole body")]
            // public bool OnBody { get; set; }
            public override string ToString() => $"{Name} {AttachPoint}";
        }

        internal class PropertyDef
        {
            [LocDisplayName("{=uUzmy7Lh}Name"),
             LocDescription("{=Dx5HUb1Q}The property to modify"), 
             PropertyOrder(1), UsedImplicitly]
            public DrivenProperty Name { get; set; }

            [LocDisplayName("{=Qu3sbJvR}Add"),
             LocDescription("{=anOigmfO}Add to the property value"), 
             PropertyOrder(2), UsedImplicitly]
            public float? Add { get; set; }

            [LocDisplayName("{=AQ7g1RuJ}Multiply"),
             LocDescription("{=35zcHc2g}Multiply the property value"), 
             PropertyOrder(3), UsedImplicitly]
            public float? Multiply { get; set; }

            public override string ToString()
            {
                var parts = new List<string> {Name.ToString()};
                if (Multiply.HasValue && Multiply.Value != 0)
                {
                    parts.Add($"* {Multiply}");
                }

                if (Add.HasValue && Add.Value != 0)
                {
                    parts.Add(Add > 0 ? $"+ {Add}" : $"{Add}");
                }

                return string.Join(" ", parts);
            }
        }

        internal class Config
        {
            [LocDisplayName("{=uUzmy7Lh}Name"),
             LocDescription("{=KdxwhYu6}Name to use when referring to this effect"), 
             PropertyOrder(1), UsedImplicitly]
            public string Name { get; set; }

            [LocDisplayName("{=hCfdaELu}Target"),
             LocDescription("{=5bWknsD5}Target of the effect"), 
             PropertyOrder(2), UsedImplicitly]
            public Target Target { get; set; }

            [LocDisplayName("{=wbj9ztiI}Target On Foot Only"),
             LocDescription("{=RptjkipN}Will target unmounted soldiers only"), 
             PropertyOrder(3), UsedImplicitly]
            public bool TargetOnFootOnly { get; set; }

            [LocDisplayName("{=j7t8cjxA}Particle Effects"),
             LocDescription("{=YiUto6gZ}Particle effects to apply"),
             Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
             PropertyOrder(5), UsedImplicitly]
            public ObservableCollection<ParticleEffectDef> ParticleEffects { get; set; } = new();

            [LocDisplayName("{=CN0a7XhW}Properties"),
             LocDescription("{=JrmttiBW}Properties to change, and how much by"),
             Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
             PropertyOrder(6), UsedImplicitly]
            public ObservableCollection<PropertyDef> Properties { get; set; } = new();
            
            [LocDisplayName("{=trYxwNFg}Heal Per Second"),
             LocDescription("{=J19JbB4E}Heal amount per second"), 
             PropertyOrder(8), UsedImplicitly]
            public float HealPerSecond { get; set; }

            [LocDisplayName("{=6Ikuo9Yl}Damage Per Second"),
             LocDescription("{=cQBHZWwK}Damage amount per second"), 
             PropertyOrder(9), UsedImplicitly]
            public float DamagePerSecond { get; set; }

            [LocDisplayName("{=KxP51isR}Duration"),
             LocDescription("{=1pG6eydt}Duration the effect will last for, if not specified the effect will last until the end of the mission"),
             DefaultValue(null), PropertyOrder(10), UsedImplicitly]
            public float? Duration { get; set; }

            [LocDisplayName("{=rFxwAzgd}Force Drop Weapons"),
             LocDescription("{=ZW0x13Y8}Force agent to drop weapons"), 
             PropertyOrder(11), UsedImplicitly]
            public bool ForceDropWeapons { get; set; }
            
            [LocDisplayName("{=vZIAY076}Remove Armor"),
             LocDescription("{=4kiY7cUb}Remove all armor"), 
             PropertyOrder(13), UsedImplicitly]
            public bool RemoveArmor { get; set; }

            [LocDisplayName("{=jJpRuWKR}Damage Multiplier"),
             LocDescription("{=FlfyDMlc}Raw damage multiplier"), 
             PropertyOrder(14), UsedImplicitly]
            public float? DamageMultiplier { get; set; }

            [LocDisplayName("{=qWGAG5l8}Activate Particle Effect"),
             LocDescription("{=6HT3lQbV}One shot vfx to apply when the effect is activated"),
             ItemsSource(typeof(OneShotParticleEffectItemSource)), 
             PropertyOrder(15), UsedImplicitly]
            public string ActivateParticleEffect { get; set; }

            [LocDisplayName("{=deboEOcD}Activate Sound"),
             LocDescription("{=zZskrilN}Sound to play when effect is activated, see Sounds.txt for the full vanilla list"),
             ItemsSource(typeof(SoundEffectItemSource)), 
             PropertyOrder(16), UsedImplicitly]
            public string ActivateSound { get; set; }

            [LocDisplayName("{=FBH0P3a9}Deactivate Particle Effect"),
             LocDescription("{=u1eFzwLY}One shot vfx to apply when the effect is deactivated, see ParticleEffects.txt for the full vanilla list"),
             ItemsSource(typeof(OneShotParticleEffectItemSource)), 
             PropertyOrder(17), UsedImplicitly]
            public string DeactivateParticleEffect { get; set; }

            [LocDisplayName("{=zfSYXHms}Deactivate Sound"),
             LocDescription("{=ziMmWmmY}Sound to play when effect is deactivated, see Sounds.txt for the full vanilla list"),
             ItemsSource(typeof(SoundEffectItemSource)), 
             PropertyOrder(18), UsedImplicitly]
            public string DeactivateSound { get; set; }
        }
    }
}