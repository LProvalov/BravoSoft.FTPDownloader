using NLog;
using System;

namespace DBDownloader.MainLogger
{
    public class Log
    {
        private static NLog.Logger _logger = LogManager.GetCurrentClassLogger();

        public enum LogType
        {
            Info,
            Trace,
            Error
        }

        public static void WriteInfo(string message)
        {
            _logger.Info(message);
        }

        public static void WriteInfo(string formatString, params object[] args)
        {
            string message = string.Format(formatString, args);
            _logger.Info(message);
        }

        public static void WriteTrace(string message)
        {
            _logger.Trace(message);
        }

        public static void WriteTraceF(string tag, string formatMessageString, params object[] args)
        {
            string message = string.Format(formatMessageString, args);
            WriteTraceF(tag, message);
        }

        public static void WriteTraceF(string tag, string message)
        {
            _logger.Trace(
                string.Format("{0} | {1} - {2}", 
                new DateTime().ToShortTimeString(), 
                tag, message));
        }

        public static void WriteTrace(string formatString, params object[] args)
        {
            string trace = string.Format(formatString, args);
            _logger.Trace(trace);
        }

        public static void WriteError(string message)
        {
            _logger.Error(message);
        }

        public static void WriteError(string formatString, params object[] args)
        {
            string error = string.Format(formatString, args);
            _logger.Error(error);
        }
    }
}
