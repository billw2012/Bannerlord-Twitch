using System.ComponentModel;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch
{
    [Description("Bot command definition")]
    public class Command : ActionBase
    {
        [Category("General"), Description("The command itself, not including the !"), PropertyOrder(1), UsedImplicitly]
        public string Name { get; set; }
        [Category("General"), Description("Hides the command from the !help action"), PropertyOrder(2), UsedImplicitly]
        public bool HideHelp { get; set; }
        [Category("General"), Description("What to show in the !help command"), PropertyOrder(3), UsedImplicitly]
        public string Help { get; set; }
        
        [ItemsSource(typeof(CommandHandlerItemsSource))]
        public override string Handler { get; set; }
        
        public override string ToString() => $"{Name} ({Handler})";
    }
}