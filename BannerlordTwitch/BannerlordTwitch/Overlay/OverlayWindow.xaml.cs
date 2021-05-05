using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BannerlordTwitch.Util;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using YamlDotNet.Serialization;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace BannerlordTwitch.Overlay
{
    // From https://stackoverflow.com/a/21461482/6402065 and  https://stackoverflow.com/a/6792677/6402065
    internal partial class OverlayWindow : Window
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
        
        public ObservableCollection<FeedItem> FeedItems { get; set; } = new();
        
        public Color BackgroundColor { get => (FeedControl.Background as SolidColorBrush)?.Color ?? Color.FromRgb(0, 0xFF, 0); set => FeedControl.Background = new SolidColorBrush(value); }

        public Color ForegroundColor { get => (FeedControl.Foreground as SolidColorBrush)?.Color ?? Colors.White; set => FeedControl.Foreground = new SolidColorBrush(value); }
        
        public double FeedFontSize { get => FeedControl.FontSize; set => FeedControl.FontSize = value; }

        private class OverlaySettings
        {
            [Browsable(false)]
            public string BackgroundColorText { get; set; }
            [YamlIgnore]
            public Color BackgroundColor
            {
                get => (Color) (ColorConverter.ConvertFromString(BackgroundColorText ?? $"#FF000000") ?? Colors.Black);
                set => BackgroundColorText = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
            }

            public string ForegroundColorText { get; set; }
            [YamlIgnore]
            public Color ForegroundColor
            {
                get => (Color) (ColorConverter.ConvertFromString(ForegroundColorText ?? $"#FF000000") ?? Colors.Black);
                set => ForegroundColorText = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
            }
            
            public double FeedFontSize { get; set; }

            public double WindowLeft { get; set; }
            public double WindowTop { get; set; }
            public double WindowWidth { get; set; }
            public double WindowHeight { get; set; }

            private static PlatformFilePath OverlayFilePath => new (EngineFilePaths.ConfigsPath, "Bannerlord-Twitch-Overlay.yaml");
            
            public static OverlaySettings Load()
            {
                try
                {
                    return !Common.PlatformFileHelper.FileExists(OverlayFilePath) 
                        ? null 
                        : new DeserializerBuilder().Build().Deserialize<OverlaySettings>(Common.PlatformFileHelper.GetFileContentString(OverlayFilePath));
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
                    Common.PlatformFileHelper.SaveFileString(OverlayFilePath, new SerializerBuilder()
                        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
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

                this.BackgroundColor = settings.BackgroundColor;
                this.ForegroundColor = settings.ForegroundColor;
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

                BackgroundColor = this.BackgroundColor,
                ForegroundColor = this.ForegroundColor,
                FeedFontSize = this.FeedFontSize,
            };
            OverlaySettings.Save(settings);
        }
    }
}