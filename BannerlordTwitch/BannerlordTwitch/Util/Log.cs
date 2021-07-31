
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using Debug = TaleWorlds.Library.Debug;

namespace BannerlordTwitch.Util
{
    public static class Log
    {
        //private static readonly string LogPath = $@"C:\ProgramData\Mount and Blade II Bannerlord\logs\log{DateTime.Now:yyyyMMddHHmmss}.txt";

        public enum Level
        {
            Trace,
            Debug,
            Information,
            Warning,
            Error,
            Critical,
            None,
        }

        private class LogTraceListener : TraceListener
        {
            private string pending;
            public override void Write(string message)
            {
                pending += message;
                //Log.Trace(message);
            }

            public override void WriteLine(string message)
            {
                Log.Trace(pending + message);
                pending = string.Empty;
            }
        }
        
        static Log()
        {
            System.Diagnostics.Trace.Listeners.Add(new LogTraceListener());
            System.Diagnostics.Debug.Listeners.Add(new LogTraceListener());
        }
        
        public static event Action<Level, string> OnLog;

        public static void LogMessage(Level level, string str)
        {
            //File.AppendAllLines(LogPath, new []{ $"{DateTime.Now:yyyyMMddHHmmss}|{str}"});
            RaiseLogEvent(level, str);
            MainThreadSync.Run(() => Debug.Print($"[BLT][{level}][{DateTime.Now:mmss}] {str}"));
        }

        public static void Trace(string str) => LogMessage(Level.Trace, str);

        public static void Info(string str) => LogMessage(Level.Information, str);

        public static void Fatal(string str)
        {
            LogMessage(Level.Critical, str);
            LogFeedFatal(str);
        }

        public static void Error(string str)
        {
            LogMessage(Level.Error, str);
            LogFeedError(str);
        }

        private static readonly ConcurrentBag<string> reportedExceptions = new(); 
            
        public static void Exception(string context, Exception ex, bool noRethrow = false)
        {
            string expId = context + (ex.GetBaseException().Message ?? "unknown");
            if (!reportedExceptions.Contains(expId))
            {
                Fatal($"{context}: {ex.GetBaseException()}");
                reportedExceptions.Add(expId);
            }
            else
            {
                Trace($"(repeat) {context}: {ex.GetBaseException()}");
            }
#if DEBUG
            if(!noRethrow) throw ex;
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
        
        private static void LogFeedError(string str) => LogFeed("!ERROR!: " + str, LogStyle.Fail);
        private static void LogFeedFatal(string str) => LogFeed("!!FATAL!!: " + str, LogStyle.Critical);

        public static void LogFeedSystem(string str) => LogFeed(str, LogStyle.System);
        public static void LogFeedBattle(string str) => LogFeed(str, LogStyle.Battle);
        public static void LogFeedEvent(string str) => LogFeed(str, LogStyle.Event);
        public static void LogFeedResponse(string userName, params string[] messages) => LogFeed($"@{userName}: {string.Join(", ", messages)}", LogStyle.Response);
        public static void LogFeedMessage(params string[] messages) => LogFeed(string.Join(", ", messages), LogStyle.General);

        private enum LogStyle
        {
            General,
            Fail,
            Critical,
            System,
            Battle,
            Event,
            Response,
        }
        
        private static void LogFeed(string str, LogStyle style)
        {
            BLTModule.AddToFeed(str, style.ToString().ToLower());
            var level = style switch
            {
                LogStyle.Battle => Level.Information,
                LogStyle.General => Level.Information,
                LogStyle.Fail => Level.Error,
                LogStyle.Critical => Level.Critical,
                LogStyle.System => Level.Information,
                LogStyle.Event => Level.Information,
                LogStyle.Response => Level.Information,
                _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
            };
            LogMessage(level, str);
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

        private static void RaiseLogEvent(Level level, string msg) => OnLog?.Invoke(level, msg);
    }
}