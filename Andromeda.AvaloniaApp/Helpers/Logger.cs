using System;

namespace Andromeda.AvaloniaApp.Helpers
{
    static class Logger
    {
        public enum LogLevel { Info = 0, Warning, Error }

#if DEBUG
        public static LogLevel logLevel = LogLevel.Info;
#else
        public static LogLevel logLevel = LogLevel.Warning;
#endif

        public static void Log(LogLevel level, string message)
        {
            if (Logger.logLevel <= level)
            {
                Console.WriteLine("[" + level.ToString() + "] " + message);
            }
        }

        public static void LogError(string message) => Log(LogLevel.Error, message);
        public static void LogWarning(string message) => Log(LogLevel.Warning, message);
        public static void LogInfo(string message) => Log(LogLevel.Info, message);
    }
}
