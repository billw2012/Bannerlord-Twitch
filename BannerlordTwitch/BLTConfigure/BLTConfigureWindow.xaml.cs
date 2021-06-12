﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace BLTConfigure
{
    public class NewActionViewModel
    {
        public string Module => NewType.Assembly.GetName().Name;
        public string Name => NewType.Name;
        public string Description => NewType.GetCustomAttribute<DescriptionAttribute>()?.Description;

        public ICommand Command { get; }
        public Type NewType { get; }

        public NewActionViewModel(Action<object> cmd, Type newType)
        {
            NewType = newType;
            Command = new RelayCommand(cmd);
        }
    }

    public partial class BLTConfigureWindow
    {
        public IEnumerable<NewActionViewModel> RewardHandlersViewModel => ActionManager.RewardHandlers.Select(a => new NewActionViewModel(_ => this.NewReward(a), a.GetType()));
        public IEnumerable<NewActionViewModel> CommandHandlersViewModel => ActionManager.CommandHandlers.Select(h => new NewActionViewModel(_ => this.NewCommand(h), h.GetType()));

        public Settings EditedSettings  { get; set; }
        public AuthSettings EditedAuthSettings  { get; set; }
        
        public bool AffiliateSpoofing
        {
            get => EditedAuthSettings.DebugSpoofAffiliate;
            set
            {
                EditedAuthSettings.DebugSpoofAffiliate = value;
                SaveAuth();
            }
        }
        
        public BLTConfigureWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            Reload();
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            SaveSettings();
            SaveAuth();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            SaveSettings();
            SaveAuth();
        }
        
        public class TypeGroupDescription : GroupDescription
        {
            public override object GroupNameFromItem(object item, int level, CultureInfo culture)
            {
                return item switch
                {
                    null => "",
                    GlobalConfig => "Global Configs",
                    Reward => "Channel Rewards",
                    Command => "Chat Commands",
                    SimTestingConfig => "Sim Testing Config",
                    _ => item.GetType().Name
                };
            }
        }
        
        private void Reload()
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
            
            ActionManager.ConvertSettings(EditedSettings.Commands);
            ActionManager.ConvertSettings(EditedSettings.Rewards);
            ActionManager.EnsureGlobalSettings(EditedSettings.GlobalConfigs);
            
            RefreshActionList();
            PropertyGrid.SelectedObject = null;

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
                ClientID = Twitch.ClientID,
                BotMessagePrefix = "░BLT░ ",
            };

            UpdateToken(EditedAuthSettings.AccessToken);
            UpdateBotToken(EditedAuthSettings.BotAccessToken);
        }

        private void RefreshActionList()
        {
            if (EditedSettings != null)
            {
                // CommandsListBox.ItemsSource = EditedSettings.Commands;
                var actionFilterView =
                    CollectionViewSource.GetDefaultView(
                        EditedSettings.GlobalConfigs.Cast<object>()
                            .Concat(EditedSettings.Rewards)
                            .Concat(EditedSettings.Commands)
                            .Concat(EditedSettings.SimTesting.Yield())
                        );
                actionFilterView.GroupDescriptions.Add(new TypeGroupDescription());
                // actionFilterView.SortDescriptions.Add(new SortDescription("Branch", ListSortDirection.Descending));
                ActionsListBox.ItemsSource = actionFilterView;
            }
        }

        private bool suspendSync = false;
        private void Actions_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!suspendSync && ActionsListBox.SelectedItem != null)
            {
                PropertyGrid.SelectedObject = ActionsListBox.SelectedItem;
                //CommandsListBox.SelectedItem = null;
            }
        }

        private void NewReward(IRewardHandler action)
        {
            NewRewardDropDown.IsOpen = false;
            // var action = (sender as Button)?.DataContext as ActionViewModel;
            var newReward = new Reward
            {
                Handler = action.GetType().Name,
                RewardSpec = new RewardSpec { Title = "New Reward" },
            };
            var settingsType = action.RewardConfigType;
            if (settingsType != null)
            {
                newReward.HandlerConfig = Activator.CreateInstance(settingsType);
            }
            
            EditedSettings.Rewards.Add(newReward);
            RefreshActionList();
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
            EditedSettings.Commands.Add(newCommand);
            RefreshActionList();
            ActionsListBox.SelectedItem = newCommand;
        }

        private void DeleteAction_OnClick(object sender, RoutedEventArgs e)
        {
            if (ActionsListBox.SelectedItem is Reward reward)
            {
                EditedSettings.Rewards.Remove(reward);
            }
            else
            {
                EditedSettings.Commands.Remove(ActionsListBox.SelectedItem as Command);
            }
            RefreshActionList();
            SaveSettings();
            PropertyGrid.SelectedObject = null;
        }

        private async void PropertyGrid_OnPropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            RefreshActionList();
            SaveSettings();
        }
        
        private void SaveSettings()
        {
            if (EditedSettings == null)
                return;
            try
            {
                Settings.Save(EditedSettings);
            }
            catch (Exception ex)
            {
                Log.Exception($"BLTConfigureWindow.SaveSettings", ex);
            }
        }

        private void SaveAuth()
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

        private static readonly string[] MainScopes =
        {
            "user:read:email",
            "chat:edit",
            "chat:read",
            "whispers:edit",
            "channel:read:subscriptions",
            "channel:read:redemptions",
            "channel:manage:redemptions",
        };

        private void ReloadButton_OnClick(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private async void GenerateToken_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            try
            {
                button.Visibility = Visibility.Collapsed;
                GenerateTokenCancel.Visibility = Visibility.Visible;
                string token = await Twitch.Authorize(MainScopes);

                if (token == null)
                {
                    throw new AuthenticationException($"Didn't get token");
                }
                UpdateToken(token);
                SaveAuth();
            }
            catch (Exception ex)
            {
                AuthTokenTextBox.Text = $"FAILED TO AUTH: {ex.Message}";
            }
            finally
            {
                button.Visibility = Visibility.Visible;
                GenerateTokenCancel.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateToken(string token)
        {
            if (EditedAuthSettings.BotAccessToken == EditedAuthSettings.AccessToken)
            {
                UpdateBotToken(token);
            }
            AuthTokenTextBox.Text = EditedAuthSettings.AccessToken = token;
            TestToken();
            
            UseMainAccountForBotButton.Visibility =
                EditedAuthSettings.BotAccessToken == EditedAuthSettings.AccessToken 
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        private static readonly string[] BotScopes =
        {
            "user:read:email",
            "chat:edit",
            "chat:read",
            "whispers:edit",
        };

        private async void GenerateBotToken_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            try
            {
                button.Visibility = Visibility.Collapsed;
                GenerateBotTokenCancel.Visibility = Visibility.Visible;
                string token = await Twitch.Authorize(BotScopes);
                if (token == null)
                {
                    throw new AuthenticationException($"Didn't get token");
                }
                UpdateBotToken(token);
                SaveAuth();
            }
            catch (Exception ex)
            {
                BotAccessTokenTextBox.Text = $"FAILED TO AUTH: {ex.Message}";
                TestToken();
            }
            finally
            {
                button.Visibility = Visibility.Visible;
                GenerateBotTokenCancel.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateBotToken(string token)
        {
            BotAccessTokenTextBox.Text = EditedAuthSettings.BotAccessToken = token;
            UseMainAccountForBotButton.Visibility =
                EditedAuthSettings.BotAccessToken == EditedAuthSettings.AccessToken 
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            // TestBotTokenButton.Visibility = !string.IsNullOrEmpty(token) 
            //     ? Visibility.Visible
            //     : Visibility.Collapsed;
        }

        private void TestToken()
        {
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                AuthTokenTestSuccess.Visibility = AuthTokenTestFailure.Visibility = Visibility.Collapsed;
                AuthTokenTesting.Visibility = Visibility.Visible;
                if (await Twitch.TestAPIToken(EditedAuthSettings.AccessToken))
                {
                    AuthTokenTestSuccess.Visibility = Visibility.Visible;
                }
                else
                {
                    AuthTokenTestFailure.Visibility = Visibility.Visible;
                    TabControl.SelectedIndex = 1;
                    this.Activate();
                }

                AuthTokenTesting.Visibility = Visibility.Collapsed;
            }));
        }

        private void UseMainAccountForBot_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateBotToken(EditedAuthSettings.AccessToken);
        }

        private void CancelAuth_OnClick(object sender, RoutedEventArgs e)
        {
            Twitch.CancelAuth();
        }

        private void TestTokenButton_OnClick(object sender, RoutedEventArgs e)
        {
            TestToken();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private static string SplitCamelCase(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
        }

        private void PropertyGrid_OnSelectedObjectChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //var grid = sender as PropertyGrid;
            //ExpandAndFixNames(grid.Properties);
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void PropertyGrid_OnPreparePropertyItem(object sender, PropertyItemEventArgs e)
        {
            if (e.PropertyItem.IsExpandable)
            {
                e.PropertyItem.IsExpanded = true;
                // e.PropertyItem.IsExpandable = false;
                // ExpandAndFixNames(e.PropertyItem.Properties);
            }

            e.PropertyItem.DisplayName = SplitCamelCase(e.PropertyItem.DisplayName);
            //throw new NotImplementedException();
        }
    }
    
    public static class Twitch
    {
        private const string HttpRedirect = 
            @"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <title>Twitch Token Auth Redirection</title>
            </head>
            <body>
                <h1 style=""color: #1cb425"">You can close this and go back to the Bannerlord Twitch window now!</h1>
                <noscript>
                    <h1>You must have javascript enabled for OAuth redirection to work!</h1>
                </noscript>
                <script lang=""javascript"">
                    let req = new XMLHttpRequest();
                    req.open('POST', '/', false);
                    req.setRequestHeader('Content-Type', 'text');
                    req.send(document.location.hash);
                    window.close();
                </script>
            </body>
            </html>
            ";

        private static HttpListener listener;

        private const int Port = 18211;
        public const string ClientID = "spo54cze6gxb3zs5qrq4njistimg87";
        
        private static readonly HttpClient client = new HttpClient();
        
        public static async Task<string> Authorize(params string[] scopes)
        {
            // Make sure we close before we try again
            listener?.Close();
            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{Port}/");
            listener.Start();

            string scopeStr = string.Join(" ", scopes);
            Process.Start(
                $"https://id.twitch.tv/oauth2/authorize?client_id={ClientID}" +
                $"&redirect_uri=http%3A%2F%2Flocalhost%3A{Port}" +
                $"&response_type=token" +
                $"&scope={scopeStr}");

            while (listener.IsListening)
            {
                var context = await listener.GetContextAsync();
                var request = context.Request;
                if (request.HttpMethod == "POST")
                {
                    string text;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        text = await reader.ReadToEndAsync();
                    }
                    listener.Close();
                    
                    var keyValuePairs = HttpUtility.UrlDecode(text)
                        .Split('&')
                        .Select(kv => kv.Split('='))
                        .ToDictionary(kv => kv[0].Replace("#", ""), kv => kv[1]);
                    if (keyValuePairs.TryGetValue("access_token", out var accessToken))
                    {
                        return accessToken;
                    }
                }
                else
                {
                    var response = context.Response;
                    byte[] buffer = Encoding.UTF8.GetBytes(HttpRedirect);
                    response.ContentLength64 = buffer.Length;
                    var output = response.OutputStream;
                    await output.WriteAsync(buffer, 0, buffer.Length);
                    output.Close();
                }
            }

            return null;
        }

        public static void CancelAuth() => listener?.Close();

        public static async Task<bool> TestAPIToken(string token)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                client.DefaultRequestHeaders.Add("Client-Id", ClientID);
                var response = await client.GetAsync("https://api.twitch.tv/helix/users");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}