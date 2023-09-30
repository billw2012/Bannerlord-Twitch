using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BannerlordTwitch.UI;

namespace BLTConfigure.UI
{
    public partial class CollectionEditorPage : Page, INotifyPropertyChanged
    {
        public List<string> PropertyPath { get; set; }
        public string PropertyName => string.Join(" > ", PropertyPath);
        public IEnumerable ItemsSource { get; set; }
        public Type ItemsSourceType { get; set; }
        public IList<Type> NewItemTypes { get; set; }

        public CollectionEditorPage(IEnumerable<string> propertyPath, IEnumerable itemsSource, Type itemsSourceType, IList<Type> newItemTypes = null)
        {
            PropertyPath = propertyPath.ToList();
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
                PropertyPath.Concat(e.PropertyName.Yield()), e.ItemsSource, e.ItemsSourceType, e.NewItemTypes
            ));
        }
    }
}