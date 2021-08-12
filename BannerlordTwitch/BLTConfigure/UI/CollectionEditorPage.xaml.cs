using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BannerlordTwitch.UI;

namespace BLTConfigure.UI
{
    public partial class CollectionEditorPage : Page, INotifyPropertyChanged
    {
        public string PropertyName { get; set; }
        public IEnumerable ItemsSource { get; set; }
        public Type ItemsSourceType { get; set; }
        public IList<Type> NewItemTypes { get; set; }

        public CollectionEditorPage(string propertyName, IEnumerable itemsSource, Type itemsSourceType, IList<Type> newItemTypes = null)
        {
            PropertyName = propertyName;
            ItemsSource = itemsSource;
            ItemsSourceType = itemsSourceType;
            NewItemTypes = newItemTypes;
            
            DataContext = this;
            InitializeComponent();
        }

        private void BackButton_Click( object sender, RoutedEventArgs e )
        {
            CollectionControl.PersistChanges();
            this.NavigationService?.GoBack();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void CollectionPropertyEditor_OpenCollectionEditor(object sender, 
            CollectionPropertyEditor.OpenCollectionEditorEventArgs e)
        {
            this.NavigationService?.Navigate(new CollectionEditorPage(
                e.PropertyName, e.ItemsSource, e.ItemsSourceType, e.NewItemTypes
            ));
        }
    }
}