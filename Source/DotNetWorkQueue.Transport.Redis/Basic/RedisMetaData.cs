namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Contains meta data about a message
    /// </summary>
    /// <remarks>This data is stored in a hash, separate from the message itself</remarks>
    public class RedisMetaData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisMetaData" /> class.
        /// </summary>
        /// <param name="queueDateTime">The queue date time.</param>
        public RedisMetaData(long queueDateTime)
        {
            ErrorTracking = new RedisErrorTracking();
            QueueDateTime = queueDateTime;
        }
        /// <summary>
        /// Gets the queue date time.
        /// </summary>
        /// <value>
        /// The queue date time.
        /// </value>
        /// <remarks>Unix timestamps (MS) of when this record was enqueued</remarks>
        public long QueueDateTime { get;}
        /// <summary>
        /// Gets the error tracking.
        /// </summary>
        /// <value>
        /// The error tracking.
        /// </value>
        public RedisErrorTracking ErrorTracking { get; set; }
    }
}
