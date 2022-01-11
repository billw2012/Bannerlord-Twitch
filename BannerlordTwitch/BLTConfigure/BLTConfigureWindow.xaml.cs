using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using BannerlordTwitch;
using BannerlordTwitch.Annotations;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using BLTConfigure.UI;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Xceed.Wpf.Toolkit.PropertyGrid;
using BinaryReader = System.IO.BinaryReader;
using BinaryWriter = System.IO.BinaryWriter;

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

    public class LogMessage
    {
        public Log.Level Level { get; set; }
        public Brush LevelColor => Level switch
        {
            Log.Level.Critical or Log.Level.Error => Brushes.Red,
            Log.Level.Warning => Brushes.Yellow,
            Log.Level.Trace => Brushes.Gray,
            _ => Brushes.Black,
        };
        public string Message { get; set; }
    }
    
    public partial class BLTConfigureWindow : INotifyPropertyChanged
    {
        public ConfigurationRootViewModel ConfigurationRoot { get; set; }
        
        public string OverlayUrl => BLTOverlay.BLTOverlay.UrlRoot;

        public ObservableCollection<LogMessage> LogEntries { get; } = new();
        
        public BLTConfigureWindow()
        {
            Log.OnLog += (level, msg) =>
            {
                this.Dispatcher.InvokeAsync(() =>
                {
                    LogEntries.Add(new () { Level = level, Message = msg });
                    if (LogEntries.Count > 500)
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            LogEntries.RemoveAt(0);
                        }
                    }
                });
            };
            
            //Loaded += (_, _) => UpdateLastSavedLoop();

            InitializeComponent();
            this.DataContext = this;
            
            Load();

            ConfigurationFrame.Navigate(new ConfigurationRootPage(ConfigurationRoot));

            // PropertyGrid.EditorDefinitions.Add(
            //     new EditorTemplateDefinition
            //     {
            //         TargetProperties = {typeof(RangeInt)}
            //     });
        }
        
        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            ConfigurationRoot.SaveSettings();
            StoreNeocitiesLogin();
            ConfigurationRoot.SaveAuth();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Don't let user accidentally close this
            e.Cancel = true;
        }
        
        public class TypeGroupDescription : GroupDescription
        {
            public override object GroupNameFromItem(object item, int level, CultureInfo culture)
            {
                return item switch
                {
                    null => "",
                    GlobalConfig => "{=S7fvEW5l}Global Configs".Translate(),
                    Reward => "{=xljCKxH7}Channel Rewards".Translate(),
                    Command => "{=mHkSZwdI}Chat Commands".Translate(),
                    SimTestingConfig => "{=c1lGU26j}Sim Testing Config".Translate(),
                    _ => item.GetType().Name
                };
            }
        }
        
        private void Load()
        {
            ConfigurationRoot = new ConfigurationRootViewModel();
            
            UpdateToken(ConfigurationRoot.EditedAuthSettings.AccessToken);
            UpdateBotToken(ConfigurationRoot.EditedAuthSettings.BotAccessToken);
            
            NeocitiesUsername.Text = ConfigurationRoot.EditedAuthSettings.NeocitiesUsername ?? string.Empty;
            NeocitiesPassword.Password = !string.IsNullOrEmpty(ConfigurationRoot.EditedAuthSettings.NeocitiesPassword) 
                ? UnprotectString(ConfigurationRoot.EditedAuthSettings.NeocitiesPassword) 
                : string.Empty;
        }
        
        private static int RoundUp(int numToRound, int multiple)
        {
            if (multiple == 0)
                return numToRound;

            int remainder = numToRound % multiple;
            if (remainder == 0)
                return numToRound;

            return numToRound + multiple - remainder;
        }

        private static string ProtectString(string unprotectedString)
        {
            try
            {
                byte[] pwBytes = Encoding.ASCII.GetBytes(unprotectedString);
                var ms = new MemoryStream();
                var writer = new BinaryWriter(ms);
                writer.Write(pwBytes.Length);
                writer.Write(pwBytes);
                while (ms.Length % 16 != 0)
                {
                    writer.Write((byte) 0);
                }

                byte[] packedBytes = ms.GetBuffer();
                ProtectedMemory.Protect(packedBytes, MemoryProtectionScope.SameLogon);
                return Convert.ToBase64String(packedBytes);
            }
            catch (Exception ex)
            {
                Log.Error($"Couldn't encrypt string: {ex.Message}");
                return string.Empty;
            }
        }
        
        private static string UnprotectString(string protectedString)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(protectedString);
                ProtectedMemory.Unprotect(bytes, MemoryProtectionScope.SameLogon);
                var ms = new MemoryStream(bytes);
                var reader = new BinaryReader(ms);
                int len = reader.ReadInt32();
                byte[] pwBytes = reader.ReadBytes(len);
                return Encoding.ASCII.GetString(pwBytes);
            }
            catch (Exception ex)
            {
                Log.Error($"Couldn't un-encrypt string: {ex.Message}");
                return string.Empty;
            }
        }
        
        private void StoreNeocitiesLogin()
        {
            ConfigurationRoot.EditedAuthSettings.NeocitiesUsername = NeocitiesUsername.Text;
            if (!string.IsNullOrEmpty(NeocitiesPassword.Password))
            {
                ConfigurationRoot.EditedAuthSettings.NeocitiesPassword = ProtectString(NeocitiesPassword.Password);
            }
            else
            {
                ConfigurationRoot.EditedAuthSettings.NeocitiesPassword = string.Empty;
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

        private async void GenerateToken_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            try
            {
                button.Visibility = Visibility.Collapsed;
                GenerateTokenCancel.Visibility = Visibility.Visible;
                string token = await TwitchAuthHelper.Authorize(MainScopes);

                if (token == null)
                {
                    throw new AuthenticationException($"Didn't get token");
                }
                UpdateToken(token);
                ConfigurationRoot.SaveAuth();
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
            if (ConfigurationRoot.EditedAuthSettings.BotAccessToken == ConfigurationRoot.EditedAuthSettings.AccessToken)
            {
                UpdateBotToken(token);
            }
            AuthTokenTextBox.Text = ConfigurationRoot.EditedAuthSettings.AccessToken = token;
            TestToken();
            
            UseMainAccountForBotButton.Visibility =
                ConfigurationRoot.EditedAuthSettings.BotAccessToken == ConfigurationRoot.EditedAuthSettings.AccessToken 
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

        private static readonly SolidColorBrush ErrorStatusForeground = Brushes.Crimson;

        private async void GenerateBotToken_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            try
            {
                button.Visibility = Visibility.Collapsed;
                GenerateBotTokenCancel.Visibility = Visibility.Visible;
                string token = await TwitchAuthHelper.Authorize(BotScopes);
                if (token == null)
                {
                    throw new AuthenticationException($"Didn't get token");
                }
                UpdateBotToken(token);
                ConfigurationRoot.SaveAuth();
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
            BotAccessTokenTextBox.Text = ConfigurationRoot.EditedAuthSettings.BotAccessToken = token;
            UseMainAccountForBotButton.Visibility =
                ConfigurationRoot.EditedAuthSettings.BotAccessToken == ConfigurationRoot.EditedAuthSettings.AccessToken 
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
                if (await TwitchAuthHelper.TestAPIToken(ConfigurationRoot.EditedAuthSettings.AccessToken))
                {
                    AuthTokenTestSuccess.Visibility = Visibility.Visible;
                }
                else
                {
                    AuthTokenTestFailure.Visibility = Visibility.Visible;
                    TabControl.SelectedIndex = 3;
                    this.Activate();
                }

                AuthTokenTesting.Visibility = Visibility.Collapsed;
            }));
        }

        private void UseMainAccountForBot_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateBotToken(ConfigurationRoot.EditedAuthSettings.AccessToken);
        }

        private void CancelAuth_OnClick(object sender, RoutedEventArgs e)
        {
            TwitchAuthHelper.CancelAuth();
        }

        private void TestTokenButton_OnClick(object sender, RoutedEventArgs e)
        {
            TestToken();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Task.Run(() => Process.Start(e.Uri.ToString()));
        }

        private async void GenerateDocumentationButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Campaign.Current?.GameStarted != true)
            {
                GenerateDocumentationResult.Foreground = ErrorStatusForeground;
                GenerateDocumentationResult.Text =
                    $"You need to start the campaign, or load a save before generating documentation!";
                return;
            }
            
            try
            {
                GenerateDocumentationButton.IsEnabled = false;
                GenerateDocumentationResult.Text = "Generating Documentation...";
                var docs = new DocumentationGenerator();
                await docs.Document(ConfigurationRoot.EditedSettings);
                await docs.SaveAsync(ConfigurationRoot.DocsTitle, ConfigurationRoot.DocsIntroduction);
                GenerateDocumentationResult.Text = "Documentation Generation Complete";
                GenerateDocumentationResult.Foreground = Brushes.Black;
            }
            catch (Exception ex)
            {
                GenerateDocumentationResult.Foreground = ErrorStatusForeground;
                GenerateDocumentationResult.Text =
                    $"Couldn't generate the documentation: {ex.Message}";
            }

            MainThreadSync.Run(() =>
                InformationManager.ShowInquiry(
                    new InquiryData(
                        "You must reload your save now!",
                        "After generating documentation you must reload your save before continuing, as the state has been changed by the generation process.",
                        true, false, "{=hpFXglKx}Okay".Translate(), null,
                        () => { }, () => { }), true)
            );

            GenerateDocumentationButton.IsEnabled = true;
        }

        private void OpenGeneratedDocumentationButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(File.Exists(DocumentationGenerator.DocumentationPath))
            {
                try
                {
                    Process.Start(DocumentationGenerator.DocumentationPath);
                    GenerateDocumentationResult.Text = string.Empty;
                }
                catch (Exception ex)
                {
                    GenerateDocumentationResult.Foreground = ErrorStatusForeground;
                    GenerateDocumentationResult.Text =
                        $"Couldn't open the documentation: {ex.Message}";
                }
            }
            else
            {
                GenerateDocumentationResult.Foreground = ErrorStatusForeground;
                GenerateDocumentationResult.Text =
                    "Documentation file doesn't exist, did you generate the documentation yet?";
            }
        }

        private void OpenGeneratedDocumentationFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(Directory.Exists(DocumentationGenerator.DocumentationRootDir))
            {
                try
                {
                    Process.Start(DocumentationGenerator.DocumentationRootDir);
                    GenerateDocumentationResult.Text = string.Empty;
                }
                catch (Exception ex)
                {
                    GenerateDocumentationResult.Foreground = ErrorStatusForeground;
                    GenerateDocumentationResult.Text =
                        $"Couldn't open the documentation {ex.Message}";
                }
            }
            else
            {
                GenerateDocumentationResult.Foreground = ErrorStatusForeground;
                GenerateDocumentationResult.Text =
                    "Documentation folder doesn't exist, did you generate the documentation yet?";
            }
        }

        private class ResponseFile
        {
            [UsedImplicitly]
            public string path;
        }
        
        private class ResponseFiles
        {
            [UsedImplicitly]
            public string result;
            [UsedImplicitly]
            public ResponseFile[] files;
        }
        
        private async void UploadDocumentation_OnClick(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(DocumentationGenerator.DocumentationRootDir))
            {
                UploadStatus.Text = "Documentation directory does not exist, did you Generate Documentation yet?";
                UploadStatus.Foreground = ErrorStatusForeground;
                return;
            }
            string[] files = Directory.GetFiles(DocumentationGenerator.DocumentationRootDir);
            if (!files.Any())
            {
                UploadStatus.Text = "No documentation files found, did you Generate Documentation yet?";
                UploadStatus.Foreground = ErrorStatusForeground;
                return;
            }
            if (files.All(f => Path.GetFileName(f) != "index.html"))
            {
                UploadStatus.Text = "Documentation doesn't contain index.html file, did you Generate Documentation yet?";
                UploadStatus.Foreground = ErrorStatusForeground;
                return;
            }

            UploadDocumentationButton.IsEnabled = false;

            try
            {
                using var httpClient = new HttpClient(new HttpClientHandler{UseProxy = false});
                httpClient.DefaultRequestHeaders.Authorization = new("Basic", 
                    Convert.ToBase64String(
                        Encoding.ASCII.GetBytes($"{NeocitiesUsername.Text}:{NeocitiesPassword.Password}")));

                UploadStatus.Text = "Checking for existing files on the site...";

                var filesResponse = await httpClient.GetAsync($"https://neocities.org/api/list");
                filesResponse.EnsureSuccessStatusCode();

                var result = JsonConvert.DeserializeObject<ResponseFiles>(await filesResponse.Content.ReadAsStringAsync());
                var deleteList = result.files
                    .Select(f => f.path)
                    .Where(f => f.ToLower() != "index.html")
                    .Select(f => new KeyValuePair<string, string>("filenames[]", f))
                    .ToList();
                if (deleteList.Any())
                {
                    UploadStatus.Text = "Deleting existing files from the site...";
                    var deleteResponse = await httpClient.PostAsync($"https://neocities.org/api/delete",
                        new FormUrlEncodedContent(deleteList));
                    deleteResponse.EnsureSuccessStatusCode();
                }

                UploadStatus.Text = "Upload in progress (might take a few seconds or longer)...";

                const int chunkSize = 20;
                int filesDone = 0;
                while (filesDone < files.Length)
                {
                    UploadStatus.Text = $"Uploading {filesDone} / {files.Length}...";
                    var chunk = files.Skip(filesDone).Take(chunkSize);
                    filesDone += chunkSize;

                    var form = new MultipartFormDataContent();
                    foreach (string f in chunk)
                    {
                        string fileName = Path.GetFileName(f);
                        var streamContent = new StreamContent(File.Open(f, FileMode.Open));
                        streamContent.Headers.Add("Content-Type", MimeMapping.GetMimeMapping(fileName));
                        streamContent.Headers.Add("Content-Disposition",
                            $"form-data; name=\"{fileName}\"; filename=\"{fileName}\"");
                        form.Add(streamContent, "file", fileName);
                    }

                    var response = await httpClient.PostAsync($"https://neocities.org/api/upload", form);
                    response.EnsureSuccessStatusCode();
                }

                UploadStatus.Text = "Upload complete!";
            }
            catch (Exception ex)
            {
                UploadStatus.Text = $"Error uploading: {ex.Message}";
                UploadStatus.Foreground = ErrorStatusForeground;
            }

            UploadDocumentationButton.IsEnabled = true;
        }
        
        private void CopyOverlayUrlButton_OnClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(OverlayUrl);
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        private void ConfigurationFrame_OnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            var ta = new ThicknessAnimation
            {
                Duration = TimeSpan.FromSeconds(0.3),
                DecelerationRatio = 0.7,
                To = new Thickness(0 , 0 , 0 , 0),
            };
            if (e.NavigationMode == NavigationMode.New)
                ta.From = new Thickness(500, 0, 0, 0);
            else if (e.NavigationMode == NavigationMode.Back)
                ta.From = new Thickness(0, 0, 500, 0);
            (e.Content as UIElement)?.BeginAnimation(MarginProperty , ta);
        }
    }
}