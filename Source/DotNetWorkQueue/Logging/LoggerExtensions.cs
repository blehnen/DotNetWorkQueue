// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
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
