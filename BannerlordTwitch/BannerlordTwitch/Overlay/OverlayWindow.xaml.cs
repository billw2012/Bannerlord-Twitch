using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using BannerlordTwitch.Util;
using TaleWorlds.Library;
using YamlDotNet.Serialization;
using Color = System.Windows.Media.Color;

namespace BannerlordTwitch.Overlay
{
    // From https://stackoverflow.com/a/21461482/6402065 and  https://stackoverflow.com/a/6792677/6402065
    internal partial class OverlayWindow
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

        public double FeedFontSize { get => FeedControl.FontSize; set => FeedControl.FontSize = value; }

        private class OverlaySettings
        {
            public double FeedFontSize { get; set; }

            public double WindowLeft { get; set; }
            public double WindowTop { get; set; }
            public double WindowWidth { get; set; }
            public double WindowHeight { get; set; }

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
                
                this.FeedFontSize = settings.FeedFontSize;
            }
        }

        private void OverlayWindow_OnClosing(object sender, CancelEventArgs e)
        {
            var settings = new OverlaySettings
            {
                WindowLeft = this.Left,
                WindowTop = this.Top,
                WindowWidth = this.Width,
                WindowHeight = this.Height,
                
                FeedFontSize = this.FeedFontSize,
            };
            OverlaySettings.Save(settings);
        }
    }
}