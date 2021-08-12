
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace BannerlordTwitch.UI
{
    public partial class CollectionPropertyEditor
    {
        #region ItemsSource Property
        public static readonly DependencyProperty PropertyNameProperty 
            = DependencyProperty.Register( "PropertyName", typeof(string), 
                typeof( CollectionPropertyEditor ), new UIPropertyMetadata( null ) );
        public string PropertyName
        {
            get => ( string )GetValue( PropertyNameProperty );
            set => SetValue( PropertyNameProperty, value );
        }
        #endregion
        
        #region ItemsSource Property
        public static readonly DependencyProperty ItemsSourceProperty 
            = DependencyProperty.Register( "ItemsSource", typeof(IEnumerable), 
                typeof( CollectionPropertyEditor ), new UIPropertyMetadata( null ) );
        public IEnumerable ItemsSource
        {
            get => ( IEnumerable )GetValue( ItemsSourceProperty );
            set => SetValue( ItemsSourceProperty, value );
        }
        #endregion

        #region ItemsSourceType Property
        public static readonly DependencyProperty ItemsSourceTypeProperty 
            = DependencyProperty.Register( "ItemsSourceType", typeof( Type ), 
                typeof( CollectionPropertyEditor ), new UIPropertyMetadata( null ) );
        public Type ItemsSourceType
        {
            get => ( Type )GetValue( ItemsSourceTypeProperty );
            set => SetValue( ItemsSourceTypeProperty, value );
        }
        #endregion

        #region NewItemTypes Property
        public static readonly DependencyProperty NewItemTypesProperty 
            = DependencyProperty.Register( "NewItemTypes", typeof( IList ), 
                typeof( CollectionPropertyEditor ), new UIPropertyMetadata( null ) );
        public IList<Type> NewItemTypes
        {
            get => ( IList<Type> )GetValue( NewItemTypesProperty );
            set => SetValue( NewItemTypesProperty, value );
        }
        #endregion
        
        public delegate void OpenCollectionEditorEventHandler(object sender, OpenCollectionEditorEventArgs e);

        public class OpenCollectionEditorEventArgs : RoutedEventArgs
        {
            public string PropertyName { get; }
            public IEnumerable ItemsSource { get; }

            public Type ItemsSourceType { get; }

            public IList<Type> NewItemTypes { get; }
            // public PropertyItemBase PropertyItem
            // {
            //     get;
            //     private set;
            // }
            //
            // public object Item
            // {
            //     get;
            //     private set;
            // }

            public OpenCollectionEditorEventArgs( RoutedEvent routedEvent, object source,
                string propertyName, IEnumerable itemsSource, Type itemsSourceType, IList<Type> newItemTypes)
                : base( routedEvent, source )
            {
                PropertyName = propertyName;
                ItemsSource = itemsSource;
                ItemsSourceType = itemsSourceType;
                NewItemTypes = newItemTypes;
                // this.PropertyItem = propertyItem;
                // this.Item = item;
            }
        }
        
        public static readonly RoutedEvent OpenCollectionEditorEvent 
            = EventManager.RegisterRoutedEvent( "OpenCollectionEditor", RoutingStrategy.Bubble, 
                typeof( OpenCollectionEditorEventHandler ), typeof( CollectionPropertyEditor ) );
        
        public event OpenCollectionEditorEventHandler OpenCollectionEditor
        {
            add
            {
                AddHandler( OpenCollectionEditorEvent, value );
            }
            remove
            {
                RemoveHandler( OpenCollectionEditorEvent, value );
            }
        }
        
        public CollectionPropertyEditor()
        {
            InitializeComponent();
        }

        private void EditButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.RaiseEvent( new OpenCollectionEditorEventArgs(
                OpenCollectionEditorEvent, this, 
                PropertyName, ItemsSource, ItemsSourceType, NewItemTypes));
        }
    }
}