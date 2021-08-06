using System;
using System.ComponentModel;
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
        public string Id { get; set; }
        [PropertyOrder(3), UsedImplicitly]
        public string Args { get; set; }
        [PropertyOrder(4), UsedImplicitly]
        public float Weight { get; set; } = 1f;
        [PropertyOrder(5), ReadOnly(true), UsedImplicitly]
        public Guid ID { get; set; } = Guid.NewGuid();

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Args))
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