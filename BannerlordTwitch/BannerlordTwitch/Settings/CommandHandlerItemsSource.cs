using BannerlordTwitch.Rewards;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch
{
    public class CommandHandlerItemsSource : IItemsSource
    {
        public ItemCollection GetValues()
        {
            var items = new ItemCollection();
            foreach (string cmd in ActionManager.CommandHandlerNames)
                items.Add(cmd);
            return items;
        }
    }
}