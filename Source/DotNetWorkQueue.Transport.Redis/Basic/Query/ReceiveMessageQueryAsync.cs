using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
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
        public ReceiveMessageQueryAsync(IMessageContext context)
        {
            Guard.NotNull(() => context, context);
            MessageContext = context;
        }
        /// <summary>
        /// Gets the message context.
        /// </summary>
        /// <value>
        /// The message context.
        /// </value>
        public IMessageContext MessageContext { get;  }
    }
}
