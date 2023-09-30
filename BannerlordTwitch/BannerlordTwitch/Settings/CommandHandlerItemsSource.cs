using BannerlordTwitch.Rewards;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch
{
    public class CommandHandlerItemsSource : IItemsSource
    {
        public ItemCollection GetValues()
        {
            var items = new ItemCollection();
            foreach ((string id, string displayName) in ActionManager.CommandHandlerIDsAndDisplayNames)
                items.Add(id, displayName);
            return items;
        }
    }
}