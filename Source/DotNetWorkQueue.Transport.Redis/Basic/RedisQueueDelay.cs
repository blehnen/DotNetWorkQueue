using System;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Holds information indicating if an existing message should have it's queue delay time increased
    /// </summary>
    internal class RedisQueueDelay
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueDelay"/> class.
        /// </summary>
        /// <param name="increaseDelay">The increase delay.</param>
        public RedisQueueDelay(TimeSpan increaseDelay)
        {
            IncreaseDelay = increaseDelay;
        }
        /// <summary>
        /// Gets the increase delay.
        /// </summary>
        /// <value>
        /// The increase delay.
        /// </value>
        public TimeSpan IncreaseDelay { get; }
    }
}
