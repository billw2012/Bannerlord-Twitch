using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace BLTConfigure.UI
{
    public class LocStringEditor : TypeEditor<LocStringControl>
    {
        protected override LocStringControl CreateEditor()
        {
            return new LocStringControl();
        }

        protected override void SetValueDependencyProperty()
        {
            //ValueProperty = LocStringControl.TextProperty;
        }
    }

    // public class PropertyGridEditorLocString : LocStringControl
    // {
    //     static PropertyGridEditorLocString()
    //     {
    //         DefaultStyleKeyProperty.OverrideMetadata( typeof( PropertyGridEditorLocString ), new FrameworkPropertyMetadata( typeof( PropertyGridEditorLocString ) ) );
    //     }
    // }
}