using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace BLTConfigure.UI
{
    public class ConfigurationRootViewModel : INotifyPropertyChanged
    {
        public Settings EditedSettings  { get; set; }
        
        public AuthSettings EditedAuthSettings  { get; set; }

        // public delegate void NewRewardDelegate(IRewardHandler handler);
        //
        // public event NewRewardDelegate OnNewReward;
        //
        // public delegate void NewCommandDelegate(ICommandHandler handler);
        //
        // public event NewCommandDelegate OnNewCommand;cv

        public ConfigurationRootViewModel()
        {
            Load();
            UpdateLastSavedLoop();
        }

        public bool AffiliateSpoofing
        {
            get => EditedAuthSettings.DebugSpoofAffiliate;
            set
            {
                EditedAuthSettings.DebugSpoofAffiliate = value;
                SaveAuth();
            }
        }

        public bool DisableAutomaticFulfillment
        {
            get => EditedSettings.DisableAutomaticFulfillment;
            set
            {
                EditedSettings.DisableAutomaticFulfillment = value;
                SaveSettings();
            }
        }
        
                
        public string DocsTitle
        {
            get => EditedAuthSettings.DocsTitle;
            set
            {
                EditedAuthSettings.DocsTitle = value;
                SaveAuth();
            }
        }          
        
        public string DocsIntroduction
        {
            get => EditedAuthSettings.DocsIntroduction;
            set
            {
                EditedAuthSettings.DocsIntroduction = value;
                SaveAuth();
            }
        }
        
        public void RefreshActionList()
        {
            if (EditedSettings != null)
            {
                // CommandsListBox.ItemsSource = EditedSettings.Commands;
                ActionFilterView = CollectionViewSource.GetDefaultView(
                    EditedSettings.GlobalConfigs.Cast<object>()
                        .Concat(EditedSettings.Rewards)
                        .Concat(EditedSettings.Commands)
                        .Concat(EditedSettings.SimTesting.Yield())
                );
                ActionFilterView.GroupDescriptions.Add(new BLTConfigureWindow.TypeGroupDescription());
                // actionFilterView.SortDescriptions.Add(new SortDescription("Branch", ListSortDirection.Descending));
                //ActionsListBox.ItemsSource = actionFilterView;
            }
        }

        private void Load()
        {
            try
            {
                EditedSettings = Settings.Load();
            }
            catch (Exception ex)
            {
                Log.Exception($"BLTConfigureWindow.Reload", ex);
                // MessageBox.Show($"Either the settings file didn't exist, or it was corrupted.\nLoaded default settings.\nIf you want to keep your broken settings file then go and copy it somewhere now, as it will be overwritten on exit.\nError: {e}", "Failed to Load Settings!");
            }

            EditedSettings ??= new Settings();
            ConfigureContext.CurrentlyEditedSettings = EditedSettings; 
            
            RefreshActionList();

            try
            {
                EditedAuthSettings = AuthSettings.Load();
            }
            catch (Exception ex)
            {
                Log.Exception($"BLTConfigureWindow.Reload", ex);
                // MessageBox.Show($"Either the auth settings file didn't exist, or it was corrupted.\nYou need to reauthorize.", "Failed to Load Auth Settings!");
            }

            EditedAuthSettings ??= new AuthSettings
            {
                ClientID = TwitchAuthHelper.ClientID,
                BotMessagePrefix = "░BLT░ ",
            };
        }
        
        private DateTime lastSaved = DateTime.MinValue;
        public string LastSavedMessage => lastSaved == DateTime.MinValue || DateTime.Now - lastSaved > TimeSpan.FromSeconds(5)
            ? string.Empty
            : $"Saved {(DateTime.Now - lastSaved).TotalSeconds:0} seconds ago. " +
              $"Reload save to apply changes.";
        
        private async void UpdateLastSavedLoop()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                PropertyChanged?.Invoke(this, new (nameof(LastSavedMessage)));
            }
        }
        
        public void SaveSettings()
        {
            if (EditedSettings == null)
                return;
            try
            {
                Settings.Save(EditedSettings);
                lastSaved = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.Exception($"BLTConfigureWindow.SaveSettings", ex);
            }
        }
        
        public void SaveAuth()
        {
            if (EditedAuthSettings == null)
                return;
            try
            {
                AuthSettings.Save(EditedAuthSettings);
            }
            catch (Exception ex)
            {
                Log.Exception($"BLTConfigureWindow.SaveAuth", ex);
            }
        }
        
        public ICollectionView ActionFilterView { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}