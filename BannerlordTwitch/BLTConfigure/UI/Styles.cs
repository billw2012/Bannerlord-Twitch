using System.Windows;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace BLTConfigure.UI
{
    [UsedImplicitly]
    public partial class Styles
    {
        private void PropertyGrid_OnPreparePropertyItem(object sender, PropertyItemEventArgs e)
        {
            if (e.PropertyItem.IsExpandable 
                && e.PropertyItem is PropertyItem p && p.PropertyDescriptor.Attributes.Contains(new ExpandAttribute()))
            {
                e.PropertyItem.IsExpanded = true;
            }

            e.PropertyItem.DisplayName = e.PropertyItem.DisplayName.SplitCamelCase();
        }
    }
}