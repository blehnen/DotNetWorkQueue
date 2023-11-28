using System;
using System.Collections.Generic;

namespace DotNetWorkQueue.Notifications
{
    /// <summary>
    /// A message error
    /// </summary>
    public class ErrorNotification : ABaseNotification
    {
        /// <summary>
        /// Message error
        /// </summary>
        /// <param name="id">Message Id</param>
        /// <param name="correlationId">Correlation Id</param>
        /// <param name="headers">Message headers</param>
        /// <param name="error">The error</param>
        public ErrorNotification(IMessageId id, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers, Exception error) : base(id, correlationId, headers)
        {
            Error = error;
        }

        /// <summary>
        /// The exception that occurred.
        /// </summary>
        public Exception Error { get; }
    }
}
