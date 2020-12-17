using System;
namespace DotNetWorkQueue.Logging
{
    /// <summary>
    /// No-Op logger
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Logging.ILogger" />
    public class NullLogger : ILogger
    {
        /// <inheritdoc />
        public void Log(Func<LogEntry> entry)
        {
        }

        /// <inheritdoc />
        public void Log(LogEntry entry)
        {
        }
    }
}
