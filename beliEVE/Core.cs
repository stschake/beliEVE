using System;
using System.IO;

namespace beliEVE
{

    public enum LogSeverity
    {
        Minor,
        Warning,
        Error,
        Fatal
    }

    public class LogEvent : EventArgs
    {
        public LogSeverity Severity { get; private set; }
        public DateTime Time { get; private set; }
        public string Message { get; private set; }

        public LogEvent(LogSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
            Time = DateTime.Now;
        }
    }

    public static class Core
    {
        private static readonly object LogLock = new object();
        private static readonly StreamWriter LogWriter;

        public static event EventHandler<LogEvent> OnLog;
        public static PacketPapeline Pipeline = new PacketPapeline();
        public static PluginManager Plugins = new PluginManager();
        
        private static string GetTimeString(DateTime time)
        {
            return time.Hour.ToString("00") + ":" + time.Minute.ToString("00") + ":" + time.Second.ToString("00") + "." +
                   time.Millisecond.ToString("000");
        }

        static Core()
        {
            LogWriter = new StreamWriter(File.Create("beliEVE.log"));
            OnLog += (s, le) => Console.WriteLine("[" + le.Severity.ToString()[0] + "] " + le.Message);
            OnLog += (s, le) =>
                         {
                             LogWriter.WriteLine(GetTimeString(le.Time) + ": " + le.Message);
                             LogWriter.Flush();
                         };
        }

        public static void LogWarning(string text)
        {
            Log(LogSeverity.Warning, text);
        }

        public static void Log(LogSeverity severity, string msg)
        {
            lock (LogLock)
            {
                if (OnLog != null)
                    OnLog(null, new LogEvent(severity, msg));
            }
        }

    }

}