using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Factory
{
    /// <inheritdoc />
    internal class RedisQueueWorkSubFactory : IRedisQueueWorkSubFactory
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly IQueueCancelWork _cancelWork;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueWorkSubFactory" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        /// <param name="cancelWork">The cancel work.</param>
        public RedisQueueWorkSubFactory(IRedisConnection connection,
            RedisNames redisNames,
            IQueueCancelWork cancelWork)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);
            Guard.NotNull(() => cancelWork, cancelWork);

            _connection = connection;
            _redisNames = redisNames;
            _cancelWork = cancelWork;
        }

        /// <inheritdoc />
        public IRedisQueueWorkSub Create()
        {
            return new RedisQueueWorkSub(_connection, _redisNames, _cancelWork);
        }
    }
}
