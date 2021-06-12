﻿
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using Debug = TaleWorlds.Library.Debug;

namespace BannerlordTwitch.Util
{
    public static class Log
    {
        //private static readonly string LogPath = $@"C:\ProgramData\Mount and Blade II Bannerlord\logs\log{DateTime.Now:yyyyMMddHHmmss}.txt";

        private static void LogFilePrint(string str)
        {
            //File.AppendAllLines(LogPath, new []{ $"{DateTime.Now:yyyyMMddHHmmss}|{str}"});
            Debug.Print($"[BLT] {str}");
        }

        public static void Trace(string str)
        {
            MainThreadSync.Run(() => LogFilePrint(str));
        }
        
        public static void Info(string str)
        {
// #if DEBUG
//             LogFeedSystem(str);
// #else
            MainThreadSync.Run(() => LogFilePrint(str));
//#endif
        }

        public static void Error(string str)
        {
#if DEBUG
            LogFeedCritical(str);
#else
            MainThreadSync.Run(() => LogFilePrint("ERROR: " + str));
#endif
        }

        private static readonly ConcurrentBag<string> reportedExceptions = new(); 
            
        public static void Exception(string context, Exception ex)
        {
            string str = $"{context}: {ex.GetBaseException()}";
            MainThreadSync.Run(() => LogFilePrint("ERROR: " + str));
            Error($"{context}: {ex.GetBaseException()}");
            string expId = context + (ex.GetBaseException().Message ?? "unk");
            if (!reportedExceptions.Contains(expId))
            {
                reportedExceptions.Add(expId);
                LogFeedCritical(str);
            }
#if DEBUG
            throw ex;
#endif
        }

        // public static void Screen(string str, Color color = default)
        // {
        //     MainThreadSync.Run(() =>
        //     {
        //         LogFilePrint(str);
        //         InformationManager.DisplayMessage(new InformationMessage("BLT: " + str,
        //             color == default
        //                 ? new Color(31 / 255f, 195 / 255f, 255 / 255f)
        //                 : color
        //             ,
        //             "event:/ui/notification/quest_finished"));
        //     });
        // }
        
        public static void LogFeedFail(string str) => LogFeed("!FAIL!: " + str, new Color(1f, 0.5f, 0f));
        public static void LogFeedCritical(string str) => LogFeed("!!CRITICAL!!: " + str, Colors.Red);
        public static void LogFeedSystem(string str) => LogFeed(str, Colors.Magenta);
        public static void LogFeedBattle(string str) => LogFeed(str, Colors.White);
        public static void LogFeedEvent(string str) => LogFeed(str, Colors.Cyan);
        public static void LogFeedResponse(string userName, params string[] messages) => LogFeed($"@{userName}: {string.Join(", ", messages)}", Colors.Yellow);
        
        public static void LogFeed(string str, Color color)
        {
            BLTModule.AddToFeed(str, color);
            Info(str);
        }
                
        public static void AddInfoPanel(Func<UIElement> construct)
        {
            BLTModule.AddInfoPanel(construct);
        }
                
        public static void RemoveInfoPanel(UIElement element)
        {
            BLTModule.RemoveInfoPanel(element);
        }

        public static void RunInfoPanelUpdate(Action action)
        {
            BLTModule.RunInfoPanelUpdate(action);
        }

        public enum Sound
        {
            None,
            Horns,
            Horns2,
            Horns3,
            Notification1,
        }

        public static void ShowInformation(string message, BasicCharacterObject characterObject = null, Sound sound = Sound.None)
        {
            string soundStr = sound switch
            {
                Sound.None => null,
                Sound.Horns => "event:/ui/mission/horns/attack",
                Sound.Horns2 => "event:/ui/mission/horns/move",
                Sound.Horns3 => "event:/ui/mission/horns/retreat",
                Sound.Notification1 => "event:/ui/notification/levelup",
                _ => throw new ArgumentOutOfRangeException(nameof(sound), sound, null)
            }; 
            InformationManager.AddQuickInformation(new TextObject(message), 1000, characterObject, soundStr);
        }

        public static long TimeFunction(Action action)
        {
            var sw = new Stopwatch();
            sw.Start();
            action();
            return sw.ElapsedMilliseconds;
        }
    }
}