using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Command
{
    /// <summary>
    /// Deletes a specific message
    /// </summary>
    public class DeleteMessageCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommand" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public DeleteMessageCommand(RedisQueueId id)
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
        public RedisQueueId Id { get;  }
    }
}
