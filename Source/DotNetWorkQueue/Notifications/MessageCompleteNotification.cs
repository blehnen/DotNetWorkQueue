using System.Collections.Generic;

namespace DotNetWorkQueue.Notifications
{
    /// <summary>
    /// A message has completed processing 
    /// </summary>
    public class MessageCompleteNotification : ABaseNotification
    {
        /// <summary>
        /// A message has completed processing.
        /// </summary>
        /// <param name="id">The message id.</param>
        /// <param name="correlationId">The correlation id.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="body">The message body.</param>
        public MessageCompleteNotification(IMessageId id, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers, dynamic body) : base(id, correlationId, headers)
        {
            Body = body;
        }

        /// <summary>
        /// Gets the body of the message.
        /// </summary>
        /// <remarks>Only the message processor knows the type; If you need to access this data, you will need to cast it to your object type</remarks>
        public dynamic Body { get; }
    }
}
