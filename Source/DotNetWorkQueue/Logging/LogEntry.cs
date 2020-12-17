using System;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Logging
{
    /// <summary>
    /// Log entry
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        /// <param name="severity">The severity.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <exception cref="ArgumentNullException">message</exception>
        /// <exception cref="ArgumentException">empty - message</exception>
        public LogEntry(LoggingEventType severity, string message, Exception exception = null)
        {
            Guard.NotNullOrEmpty(() => message, message);
            Severity = severity;
            Message = message;
            Exception = exception;
        }

        /// <summary>
        /// Gets the severity.
        /// </summary>
        public LoggingEventType Severity { get; }
        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message { get; }
        /// <summary>
        /// Gets the exception.
        /// </summary>
        public Exception Exception { get; }
    }
}
