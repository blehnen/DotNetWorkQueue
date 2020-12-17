using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Logging
{
    /// <summary>
    /// Logging interface
    /// </summary>
    /// <remarks>Must be implemented by consumers of the queue, as in release mode there are no targets</remarks>
    public interface ILogger
    {
        /// <summary>
        /// Logs the specified entry.
        /// </summary>
        /// <param name="entry">The entry.</param>
        void Log(LogEntry entry);
        /// <summary>
        /// Logs the specified entry.
        /// </summary>
        /// <param name="entry">The entry.</param>
        void Log(Func<LogEntry> entry);
    }
}
