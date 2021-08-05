using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using BannerlordTwitch.Util;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTBuffet
{
    public partial class CharacterEffect
    {
        internal enum Target
        {
            Player,
            AdoptedHero,
            Any,
            EnemyTeam,
            PlayerTeam,
            AllyTeam,
            Random
        }

        internal class LightDef
        {
            public float Radius { get; set; }
            public float Intensity { get; set; }
            public string Color { get; set; }

            public Vec3 ColorParsed
            {
                get
                {
                    if (string.IsNullOrEmpty(Color))
                        return new Vec3();
                    string[] parts = Color.Split(' ');
                    var color = new Vec3();
                    for (int index = 0; index < parts.Length && index < 3; index++)
                    {
                        if (!float.TryParse(parts[index], out float val))
                        {
                            return new Vec3();
                        }

                        color[index] = val;
                    }

                    return color;
                }
            }
        }

        internal class ParticleEffectDef
        {
            [Description("Particle effect system name, see ParticleEffects.txt for the full vanilla list"),
             ItemsSource(typeof(ParticleEffectItemSource)), PropertyOrder(1)]
            public string Name { get; set; }

            public enum AttachPointEnum
            {
                OnWeapon,
                OnHands,
                OnHead,
                OnBody,
            }

            [Description("Where to attach the particles"), PropertyOrder(2)]
            public AttachPointEnum AttachPoint { get; set; }

            // [Description("Apply the effect to the weapon")]
            // public bool OnWeapon { get; set; }
            // [Description("Apply the effect to the hands")]
            // public bool OnHands { get; set; }
            // [Description("Apply the effect to the head")]
            // public bool OnHead { get; set; }
            // [Description("Apply the effect to the whole body")]
            // public bool OnBody { get; set; }
            public override string ToString()
            {
                return $"{Name} {AttachPoint}";
            }
        }

        internal class PropertyDef
        {
            [Description("The property to modify"), PropertyOrder(1)]
            public DrivenProperty Name { get; set; }

            [Description("Add to the property value"), PropertyOrder(2)]
            public float? Add { get; set; }

            [Description("Multiply the property value"), PropertyOrder(3)]
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
            [Description("Name to use when referring to this effect"), PropertyOrder(1)]
            public string Name { get; set; }

            [Description("Target of the effect"), PropertyOrder(2)]
            public Target Target { get; set; }

            [Description("Will target unmounted soldiers only"), PropertyOrder(3)]
            public bool TargetOnFootOnly { get; set; }

            [Description("Particle effects to apply"), PropertyOrder(5)]
            public ObservableCollection<ParticleEffectDef> ParticleEffects { get; set; }

            [Description("Properties to change, and how much by"), PropertyOrder(6)]
            public ObservableCollection<PropertyDef> Properties { get; set; }

            // [Description("Creates a light attached to the target"), PropertyOrder(7)]
            // public LightDef Light { get; set; }

            [Description("Heal amount per second"), PropertyOrder(8)]
            public float HealPerSecond { get; set; }

            [Description("Damage amount per second"), PropertyOrder(9)]
            public float DamagePerSecond { get; set; }

            [Description(
                 "Duration the effect will last for, if not specified the effect will last until the end of the mission"),
             PropertyOrder(10), DefaultValue(null)]
            public float? Duration { get; set; }

            [Description("Force agent to drop weapons"), PropertyOrder(11)]
            public bool ForceDropWeapons { get; set; }

            // [Description("Force agent dismount"), PropertyOrder(12)]
            // public bool ForceDismount { get; set; }
            [Description("Remove all armor"), PropertyOrder(13)]
            public bool RemoveArmor { get; set; }

            [Description("Raw damage multiplier"), PropertyOrder(14)]
            public float? DamageMultiplier { get; set; }

            [Description("One shot vfx to apply when the effect is activated"),
             ItemsSource(typeof(ParticleEffectItemSource)), PropertyOrder(15)]
            public string ActivateParticleEffect { get; set; }

            [Description("Sound to play when effect is activated, see Sounds.txt for the full vanilla list"),
             ItemsSource(typeof(SoundEffectItemSource)), PropertyOrder(16)]
            public string ActivateSound { get; set; }

            [Description(
                 "One shot vfx to apply when the effect is deactivated, see ParticleEffects.txt for the full vanilla list"),
             ItemsSource(typeof(ParticleEffectItemSource)), PropertyOrder(17)]
            public string DeactivateParticleEffect { get; set; }

            [Description("Sound to play when effect is deactivated, see Sounds.txt for the full vanilla list"),
             ItemsSource(typeof(SoundEffectItemSource)), PropertyOrder(18)]
            public string DeactivateSound { get; set; }
        }
    }
}