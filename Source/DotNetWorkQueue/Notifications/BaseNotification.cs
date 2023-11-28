using System.Collections.Generic;

namespace DotNetWorkQueue.Notifications
{
    /// <summary>
    /// Base message notification
    /// </summary>
    public abstract class ABaseNotification
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">Message Id</param>
        /// <param name="correlationId">Correlation Id</param>
        /// <param name="headers">Message headers</param>
        protected ABaseNotification(IMessageId id, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers)
        {
            MessageId = id;
            CorrelationId = correlationId;
            Headers = headers;
        }

        /// <summary>
        /// Returns data from a header property
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        public THeader GetHeader<THeader>(IMessageContextData<THeader> property)
            where THeader : class
        {
            if (!Headers.ContainsKey(property.Name))
            {
                return property.Default;
            }
            return (THeader)Headers[property.Name];
        }

        /// <summary>
        /// Gets or sets the message identifier.
        /// </summary>
        /// <value>
        /// The message identifier.
        /// </value>
        /// <remarks>Can be null in some cases</remarks>
        public IMessageId MessageId { get; }

        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        /// <remarks>Can be null in some cases</remarks>
        public ICorrelationId CorrelationId { get; }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        /// <remarks>If possible use <seealso cref="GetHeader{THeader}"/> to get data in a type safe manner</remarks>
        public IReadOnlyDictionary<string, object> Headers { get; }
    }
}
