using System;
using System.Collections.Generic;

namespace DotNetWorkQueue.Notifications
{
    /// <summary>
    /// A message rollback notification
    /// </summary>
    public class RollBackNotification : ABaseNotification
    {
        /// <summary>
        /// A message rollback notification.
        /// </summary>
        /// <param name="id">The message id.</param>
        /// <param name="correlationId">The correlation id.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="error">The message error.</param>
        public RollBackNotification(IMessageId id, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers, Exception error) : base(id, correlationId, headers)
        {
            Error = error;
        }

        /// <summary>
        /// The error that triggered the rollback.
        /// </summary>
        public Exception Error { get; }
    }
}
