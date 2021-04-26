
using System;
using System.Diagnostics;
using TaleWorlds.Library;
using TaleWorlds.Core;
using Debug = TaleWorlds.Library.Debug;

namespace BannerlordTwitch.Util
{
    public static class Log
    {
        private static void LogFilePrint(string str)
        {
            Debug.Print($"[BLT] {str}");
        }

        public static void Trace(string str)
        {
            MainThreadSync.Run(() => LogFilePrint(str));
        }
        
        public static void Info(string str)
        {
#if DEBUG
            Screen(str);
#else
            MainThreadSync.Run(() => LogFilePrint(str));
#endif
        }

        public static void Error(string str)
        {
#if DEBUG
            ScreenCritical(str);
#else
            MainThreadSync.Run(() => LogFilePrint("ERROR: " + str));
#endif
        }

        public static void Screen(string str, Color color = default)
        {
            MainThreadSync.Run(() =>
            {
                LogFilePrint(str);
                InformationManager.DisplayMessage(new InformationMessage("BLT: " + str,
                    color == default
                        ? new Color(31 / 255f, 195 / 255f, 255 / 255f)
                        : color
                    ,
                    "event:/ui/notification/quest_finished"));
            });
        }
        
        public static void ScreenFail(string str)
        {
            MainThreadSync.Run(() =>
            {
                LogFilePrint(str);
                InformationManager.DisplayMessage(new InformationMessage("BLT: " + str, new Color(252 / 255f, 95 / 255f, 43 / 255f),
                    "event:/ui/notification/quest_fail"));
            });
        }

        public static void ScreenCritical(string str)
        {
            MainThreadSync.Run(() =>
            {
                LogFilePrint(str);
                InformationManager.DisplayMessage(new InformationMessage("!! BLT-CRITICAL !!: " + str, new Color(1, 0, 0),
                    "event:/ui/notification/quest_fail"));
            });
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