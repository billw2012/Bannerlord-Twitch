using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace BLTConfigure.UI
{
    public partial class ConfigurationRootPage : Page
    {
        public ConfigurationRootViewModel Model { get; }

        public IEnumerable<NewActionViewModel> RewardHandlersViewModel => ActionManager.RewardHandlers.Select(a => new NewActionViewModel(_ => this.NewReward(a), a.GetType()));

        public IEnumerable<NewActionViewModel> CommandHandlersViewModel => ActionManager.CommandHandlers.Select(h => new NewActionViewModel(_ => this.NewCommand(h), h.GetType()));
        
        public ConfigurationRootPage(ConfigurationRootViewModel model)
        {
            Model = model;
            DataContext = this;
            InitializeComponent();
        }
        
        private void Actions_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                PropertyGrid.SelectedObject = e.AddedItems[0];
                //CommandsListBox.SelectedItem = null;
            }
        }

        private DateTime saveIn;
        private async void PropertyGrid_OnPropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            bool newSave = saveIn == default;
            saveIn = DateTime.Now + TimeSpan.FromSeconds(5);
            if (!newSave)
            {
                return;
            }
            
            while (DateTime.Now < saveIn)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            Model.RefreshActionList();
            Model.SaveSettings();

            saveIn = default;
        }
        
        private void NewReward(IRewardHandler handler)
        {
            NewRewardDropDown.IsOpen = false;

            // var action = (sender as Button)?.DataContext as ActionViewModel;
            var newReward = new Reward
            {
                Handler = handler.GetType().Name,
                RewardSpec = new RewardSpec { Title = "New Reward" },
            };
            var settingsType = handler.RewardConfigType;
            if (settingsType != null)
            {
                newReward.HandlerConfig = Activator.CreateInstance(settingsType);
            }
            
            Model.EditedSettings.Rewards.Add(newReward);
            Model.RefreshActionList();
            ActionsListBox.SelectedItem = newReward;
        }

        private void NewCommand(ICommandHandler handler)
        {
            NewCommandDropDown.IsOpen = false;
            var newCommand = new Command
            {
                Handler = handler.GetType().Name,
                Name = "New Command",
            };
            var settingsType = handler.HandlerConfigType;
            if (settingsType != null)
            {
                newCommand.HandlerConfig = Activator.CreateInstance(settingsType);
            }
            Model.EditedSettings.Commands.Add(newCommand);
            Model.RefreshActionList();
            ActionsListBox.SelectedItem = newCommand;
        }

        private void DeleteAction_OnClick(object sender, RoutedEventArgs e)
        {
            if (ActionsListBox.SelectedItem is Reward reward)
            {
                Model.EditedSettings.Rewards.Remove(reward);
            }
            else
            {
                Model.EditedSettings.Commands.Remove(ActionsListBox.SelectedItem as Command);
            }
            Model.RefreshActionList();
            Model.SaveSettings();
            PropertyGrid.SelectedObject = null;
        }

        private void CollectionPropertyEditor_OpenCollectionEditor(object sender, CollectionPropertyEditor.OpenCollectionEditorEventArgs e)
        {
            this.NavigationService?.Navigate(new CollectionEditorPage(
                e.PropertyName, e.ItemsSource, e.ItemsSourceType, e.NewItemTypes
            ));
        }
    }
}