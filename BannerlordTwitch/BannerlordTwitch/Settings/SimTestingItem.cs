using System;
using System.ComponentModel;
using BannerlordTwitch.Localization;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BannerlordTwitch
{
    public enum SimActionType
    {
        Reward,
        Command
    }

    [UsedImplicitly]
    public class SimTestingItem : INotifyPropertyChanged
    {
        [PropertyOrder(0), UsedImplicitly]
        public bool Enabled { get; set; } = true;
        [PropertyOrder(1), UsedImplicitly]
        public SimActionType Type { get; set; }
        [PropertyOrder(2), UsedImplicitly]
        public LocString Id { get; set; } = string.Empty;
        [PropertyOrder(3), UsedImplicitly]
        public LocString Args { get; set; } = string.Empty;
        [PropertyOrder(4), UsedImplicitly]
        public float Weight { get; set; } = 1f;
        [PropertyOrder(5), ReadOnly(true), UsedImplicitly]
        public Guid ID { get; set; } = Guid.NewGuid();

        public override string ToString()
        {
            if (!LocString.IsNullOrEmpty(Args))
            {
                return $"{Id} ({Type}), {nameof(Args)}: {Args}, {nameof(Weight)}: {Weight}";
            }
            else
            {
                return $"{Id} ({Type}), {nameof(Weight)}: {Weight}";
            }
        }
        //public override string ToString() => $"{Type} {Id} {Args} {Weight}";

        public event PropertyChangedEventHandler PropertyChanged;
    }
}