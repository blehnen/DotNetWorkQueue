using System;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Command
{
    /// <summary>
    /// Returns a message back to a waiting for processing state
    /// </summary>
    public class RollbackMessageCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommand" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="increaseQueueDelay">The increase queue delay.</param>
        public RollbackMessageCommand(RedisQueueId id, TimeSpan? increaseQueueDelay)
        {
            Guard.NotNull(() => id, id);
            Id = id;
            IncreaseQueueDelay = increaseQueueDelay;
        }
        /// <summary>
        /// Gets or sets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public RedisQueueId Id { get;  }
        /// <summary>
        /// Gets the increase queue delay.
        /// </summary>
        /// <value>
        /// The increase queue delay.
        /// </value>
        public TimeSpan? IncreaseQueueDelay { get; }
    }
}
