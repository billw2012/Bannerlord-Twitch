using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using BannerlordTwitch.Annotations;
using BannerlordTwitch.Util;
using TaleWorlds.Library;
using YamlDotNet.Serialization;
using Color = System.Windows.Media.Color;

namespace BannerlordTwitch.Overlay
{
    // From https://stackoverflow.com/a/21461482/6402065 and https://stackoverflow.com/a/6792677/6402065
    internal partial class OverlayWindow : INotifyPropertyChanged
    {
        public class FeedItem
        {
            public FeedItem(string text, Color color)
            {
                Text = text;
                this.Color = new SolidColorBrush(color);
            }

            public string Text { get; }
            public Brush Color { get; set; }
        }

        public void AddToFeed(string text, Color color)
        {
            void AddToFeedInternal()
            {
                var item = new FeedItem(text, color);
                FeedItems.Add(item);
                while (FeedItems.Count > 50)
                {
                    FeedItems.RemoveAt(0);
                }
            }

            if(Dispatcher.Thread == Thread.CurrentThread)
            {
                AddToFeedInternal();
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(AddToFeedInternal));
            }
        }

        public void AddInfoPanel(Func<UIElement> construct)
        {
            if(Dispatcher.Thread == Thread.CurrentThread)
            {
                InfoPanel.Children.Add(construct());
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => InfoPanel.Children.Add(construct())));
            }
        }

        public void RemoveInfoPanel(UIElement element)
        {
            if(Dispatcher.Thread == Thread.CurrentThread)
            {
                InfoPanel.Children.Remove(element);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => InfoPanel.Children.Remove(element)));
            }
        }

        public void RunInfoPanelUpdate(Action action)
        {
            if(Dispatcher.Thread == Thread.CurrentThread)
            {
                action();
            }
            else
            {
                Dispatcher.BeginInvoke(action);
            }
        }

        public ObservableCollection<FeedItem> FeedItems { get; set; } = new();

        public int OverlayScalePercent
        {
            get
            {
                if (RootControl.LayoutTransform is ScaleTransform scale)
                {
                    return (int) (scale.ScaleX * 100);
                }

                return 100;
            }
            set
            {
                float clampedValue = MathF.Clamp(value, 25f, 400f);
                RootControl.LayoutTransform = new ScaleTransform(clampedValue / 100f, clampedValue / 100f);
                OnPropertyChanged();
            }
        }

        private class OverlaySettings
        {
            public int OverlayScalePercent { get; set; } = 100;

            public int WindowLeft { get; set; } = 20;
            public int WindowTop { get; set; } = 20;
            public int WindowWidth { get; set; } = 250;
            public int WindowHeight { get; set; } = 600;

            private static PlatformFilePath OverlayFilePath => FileSystem.GetConfigPath("Bannerlord-Twitch-Overlay.yaml");
            
            public static OverlaySettings Load()
            {
                try
                {
                    return !FileSystem.FileExists(OverlayFilePath) 
                        ? null 
                        : new DeserializerBuilder()
                            .IgnoreUnmatchedProperties()
                            .Build()
                            .Deserialize<OverlaySettings>(FileSystem.GetFileContentString(OverlayFilePath));
                }
                catch (Exception e)
                {
                    Log.Error($"Couldn't load overlay settings: {e.Message}");
                    return null;
                }
            }

            public static void Save(OverlaySettings settings)
            {
                try
                {
                    FileSystem.SaveFileString(OverlayFilePath, new SerializerBuilder()
                        .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
                        .Build()
                        .Serialize(settings));
                }
                catch (Exception e)
                {
                    Log.Error($"Couldn't save overlay settings: {e.Message}");
                }
            }
        }

        public OverlayWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            // for (int i = 0; i < 15; i++)
            // {
            //     AddToFeed($"{i} A long piece of text to test wrapping and what not. Hey how is it going? I'm a long string.");
            // }
        }

        private void OverlayWindow_OnSourceInitialized(object sender, EventArgs e)
        {
            var settings = OverlaySettings.Load();
            if (settings != null)
            {
                this.Left = settings.WindowLeft;
                this.Top = settings.WindowTop;
                this.Width = settings.WindowWidth;
                this.Height = settings.WindowHeight;
                
                this.OverlayScalePercent = settings.OverlayScalePercent;
            }
        }

        private void OverlayWindow_OnClosing(object sender, CancelEventArgs e)
        {
            var settings = new OverlaySettings
            {
                WindowLeft = (int) this.Left,
                WindowTop = (int) this.Top,
                WindowWidth = (int) this.Width,
                WindowHeight = (int) this.Height,
                
                OverlayScalePercent = this.OverlayScalePercent,
            };
            OverlaySettings.Save(settings);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}