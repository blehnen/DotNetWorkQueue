using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Command
{
    /// <summary>
    /// Moves a meta data record to the error table
    /// </summary>
    public class MoveRecordToErrorQueueCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MoveRecordToErrorQueueCommand" /> class.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        public MoveRecordToErrorQueueCommand(RedisQueueId queueId)
        {
            Guard.NotNull(() => queueId, queueId);
            QueueId = queueId;
        }
        /// <summary>
        /// Gets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public RedisQueueId QueueId { get; }
    }
}
