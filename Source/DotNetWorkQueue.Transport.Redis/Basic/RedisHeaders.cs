using System;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Defines our custom headers for internal operations
    /// </summary>
    internal class RedisHeaders
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisHeaders" /> class.
        /// </summary>
        /// <param name="messageContextDataFactory">The message context data factory.</param>
        /// <param name="headers">The headers.</param>
        public RedisHeaders(IMessageContextDataFactory messageContextDataFactory,
            IHeaders headers)
        {
            Guard.NotNull(() => messageContextDataFactory, messageContextDataFactory);
            Guard.NotNull(() => headers, headers);
            Headers = headers;
            IncreaseQueueDelay = messageContextDataFactory.Create("IncreaseQueueDelay", new RedisQueueDelay(TimeSpan.Zero));
            CorrelationId = messageContextDataFactory.Create<RedisQueueCorrelationIdSerialized>("CorrelationId", null);
        }
        /// <summary>
        /// Gets the standard headers
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public IHeaders Headers { get; }
        /// <summary>
        /// Gets the increase queue delay.
        /// </summary>
        /// <value>
        /// The increase queue delay.
        /// </value>
        /// <remarks>How much a record should be delayed when a rollback occurs</remarks>
        public IMessageContextData<RedisQueueDelay> IncreaseQueueDelay
        {
            get; 
        }
        /// <summary>
        /// Gets the correlation identifier.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        public IMessageContextData<RedisQueueCorrelationIdSerialized> CorrelationId { get;  } 
    }
}
