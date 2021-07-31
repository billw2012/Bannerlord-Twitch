using System;
using System.Diagnostics;
using BannerlordTwitch.Util;
using Microsoft.Owin.Logging;

namespace BLTOverlay
{
    public class LoggerFactory : ILoggerFactory
    {
        public Microsoft.Owin.Logging.ILogger Create(string name)
        {
            return new Logger(name);
        }

        private class Logger : Microsoft.Owin.Logging.ILogger
        {
            private readonly string name;

            internal Logger(string name)
            {
                this.name = name;
            }

            public bool WriteCore(TraceEventType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
            {
                // According to docs http://katanaproject.codeplex.com/SourceControl/latest#src/Microsoft.Owin/Logging/ILogger.cs
                // "To check IsEnabled call WriteCore with only TraceEventType and check the return value, no event will be written."
                if (state == null)
                {
                    return true;
                }
                var level = eventType switch
                {
                    TraceEventType.Critical => Log.Level.Critical,
                    TraceEventType.Error => Log.Level.Error,
                    TraceEventType.Warning => Log.Level.Warning,
                    TraceEventType.Information => Log.Level.Information,
                    _ => Log.Level.Trace
                };
                Log.LogMessage(level, $"[{name}]" + formatter(state, exception));
                return true;
            }
        }
    }
}