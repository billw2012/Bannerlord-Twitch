using BannerlordTwitch.Rewards;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch
{
    public class RewardHandlerItemsSource : IItemsSource
    {
        public ItemCollection GetValues()
        {
            var items = new ItemCollection();
            foreach (string action in ActionManager.RewardHandlerNames)
                items.Add(action);
            return items;
        }
    }
}