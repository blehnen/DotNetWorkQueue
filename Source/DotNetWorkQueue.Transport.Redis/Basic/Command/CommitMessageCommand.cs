using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Command
{
    /// <summary>
    /// Commits the message by removing it from redis
    /// </summary>
    public class CommitMessageCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitMessageCommand" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public CommitMessageCommand(RedisQueueId id)
        {
            Guard.NotNull(() => id, id);
            Id = id;
        }
        /// <summary>
        /// Gets or sets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public RedisQueueId Id { get; }
    }
}
