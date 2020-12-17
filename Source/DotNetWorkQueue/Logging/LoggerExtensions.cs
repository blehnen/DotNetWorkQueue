using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Logging
{
    /// <summary>
    /// Logging extension methods for logging for a particular event level
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Logs the information.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void LogInformation(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LoggingEventType.Information, message));
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public static void LogError(this ILogger logger, string message, Exception exception)
        {
            logger.Log(new LogEntry(LoggingEventType.Error, message, exception));
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void LogError(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LoggingEventType.Error, message));
        }

        /// <summary>
        /// Logs the fatal exception.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public static void LogFatal(this ILogger logger, string message, Exception exception)
        {
            logger.Log(new LogEntry(LoggingEventType.Error, message, exception));
        }

        /// <summary>
        /// Logs the fatal exception.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void LogFatal(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LoggingEventType.Error, message));
        }

        /// <summary>
        /// Logs the debug.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void LogDebug(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LoggingEventType.Debug, message));
        }

        /// <summary>
        /// Logs the trace.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void LogTrace(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LoggingEventType.Trace, message));
        }

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void LogWarning(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LoggingEventType.Warning, message));
        }

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public static void LogWarning(this ILogger logger, string message, Exception exception)
        {
            logger.Log(new LogEntry(LoggingEventType.Warning, message, exception));
        }


        /// <summary>
        /// Logs the information.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void LogInformation(this ILogger logger, Func<string> message)
        {
            logger.Log(() => new LogEntry(LoggingEventType.Information, message.Invoke()));
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public static void LogError(this ILogger logger, Func<string> message, Exception exception)
        {
            logger.Log(() => new LogEntry(LoggingEventType.Error, message.Invoke(), exception));
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void LogError(this ILogger logger, Func<string> message)
        {
            logger.Log(() => new LogEntry(LoggingEventType.Error, message.Invoke()));
        }


        /// <summary>
        /// Logs the fatal exception.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public static void LogFatal(this ILogger logger, Func<string> message, Exception exception)
        {
            logger.Log(() => new LogEntry(LoggingEventType.Error, message.Invoke(), exception));
        }

        /// <summary>
        /// Logs the fatal exception.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void LogFatal(this ILogger logger, Func<string> message)
        {
            logger.Log(() => new LogEntry(LoggingEventType.Error, message.Invoke()));
        }

        /// <summary>
        /// Logs the debug.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void LogDebug(this ILogger logger, Func<string> message)
        {
            logger.Log(() => new LogEntry(LoggingEventType.Debug, message.Invoke()));
        }

        /// <summary>
        /// Logs the trace.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void LogTrace(this ILogger logger, Func<string> message)
        {
            logger.Log(() => new LogEntry(LoggingEventType.Trace, message.Invoke()));
        }

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void LogWarning(this ILogger logger, Func<string> message)
        {
            logger.Log(() => new LogEntry(LoggingEventType.Warning, message.Invoke()));
        }

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public static void LogWarning(this ILogger logger, Func<string> message, Exception exception)
        {
            logger.Log(() => new LogEntry(LoggingEventType.Warning, message.Invoke(), exception));
        }

    }
}
