using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Command
{
    /// <summary>
    /// Sends a heart beat to a queue record.
    /// </summary>
    public class SendHeartBeatCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatCommand"/> class.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        public SendHeartBeatCommand(RedisQueueId queueId)
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
        public RedisQueueId QueueId { get;}
    }
}
