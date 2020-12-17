using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Logging
{
    /// <summary>
    /// Logs messages to the console
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Logging.ILogger" />
    public class ConsoleLogger: ILogger
    {
        private LoggingEventType? _level;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
        /// </summary>
        /// <param name="level">The level.</param>
        public ConsoleLogger(LoggingEventType? level)
        {
            _level = level;
        }

        /// <inheritdoc />
        public void Log(Func<LogEntry> entry)
        {
            if (!_level.HasValue)
                return;
            Log(entry.Invoke());
        }

        /// <inheritdoc />
        public void Log(LogEntry entry)
        {
            if (!_level.HasValue)
                return;

            if(entry.Severity == LoggingEventType.Trace && _level.Value <= LoggingEventType.Trace)
                Console.WriteLine(entry.Format());
            else if (entry.Severity == LoggingEventType.Debug && _level.Value <= LoggingEventType.Debug)
                Console.WriteLine(entry.Format());
            else if (entry.Severity == LoggingEventType.Information && _level.Value <= LoggingEventType.Information)
                Console.WriteLine(entry.Format());
            else if (entry.Severity == LoggingEventType.Warning && _level.Value <= LoggingEventType.Warning)
                Console.WriteLine(entry.Format());
            else if (entry.Severity == LoggingEventType.Error && _level.Value <= LoggingEventType.Error)
                Console.WriteLine(entry.Format());
            else
                Console.WriteLine(entry.Format());
        }
    }

    /// <summary>
    /// Formats log messages
    /// </summary>
    public static class ConsoleLoggerExt
    {
        /// <summary>
        /// Formats the specified entry.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <returns></returns>
        public static string Format(this LogEntry entry)
        {
            return entry.Exception != null ? $"{entry.Severity}    {entry.Message}{System.Environment.NewLine}{entry.Exception}" : $"{entry.Severity}    {entry.Message}";
        }
    }
}
