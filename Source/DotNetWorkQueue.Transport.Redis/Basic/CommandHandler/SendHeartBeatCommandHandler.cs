using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class SendHeartBeatCommandHandler: ICommandHandlerWithOutput<SendHeartBeatCommand, long>
    {
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;

        /// <summary>Initializes a new instance of the <see cref="DeleteMessageCommandHandler"/> class.</summary>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="connection">Redis connection</param>
        /// <param name="redisNames">Redis key names</param>
        public SendHeartBeatCommandHandler(IUnixTimeFactory unixTimeFactory,
            IRedisConnection connection, 
            RedisNames redisNames)
        {
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);

            _unixTimeFactory = unixTimeFactory;
            _connection = connection;
            _redisNames = redisNames;
        }

        /// <inheritdoc />
        public long Handle(SendHeartBeatCommand command)
        {
            if (_connection.IsDisposed)
                return 0;

            if (!command.QueueId.HasValue)
                return 0;

            var db = _connection.Connection.GetDatabase();
            var date = _unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds();
            db.SortedSetAdd(_redisNames.Working, command.QueueId.Id.Value.ToString(), date, When.Exists);

            return date;
        }
    }
}
