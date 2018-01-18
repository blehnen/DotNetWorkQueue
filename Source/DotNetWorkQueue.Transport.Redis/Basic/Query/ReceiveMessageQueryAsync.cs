using System.Threading.Tasks;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Query
{
    /// <inheritdoc />
    /// <summary>
    /// Dequeues a message from the queue.
    /// </summary>
    public class ReceiveMessageQueryAsync : IQuery<Task<RedisMessage>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQuery"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="id">The identifier.</param>
        public ReceiveMessageQueryAsync(IMessageContext context, IMessageId id)
        {
            Guard.NotNull(() => context, context);

            MessageContext = context;
            MessageId = id;
        }
        /// <summary>
        /// Gets the message context.
        /// </summary>
        /// <value>
        /// The message context.
        /// </value>
        public IMessageContext MessageContext { get;  }
        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        /// <value>
        /// The message identifier.
        /// </value>
        public IMessageId MessageId { get; }
    }
}
