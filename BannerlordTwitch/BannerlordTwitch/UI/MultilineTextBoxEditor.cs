using System.Windows;
using System.Windows.Data;
using JetBrains.Annotations;

namespace BannerlordTwitch.UI
{
    // From https://stackoverflow.com/a/48838490/6402065
    [UsedImplicitly]
    public class MultilineTextNonEditable : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            var textBlock = new System.Windows.Controls.TextBlock {TextWrapping = TextWrapping.Wrap};
            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value")
            {
                Source = propertyItem, Mode = BindingMode.OneWay
            }; 
            //bind to the Value property of the PropertyItem
            BindingOperations.SetBinding(textBlock, System.Windows.Controls.TextBlock.TextProperty, _binding);
            return textBlock;
        }
    }
}