using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    /// <inheritdoc />
    internal class GetWorkingCountQueryHandler : IQueryHandler<GetWorkingCountQuery, long>
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetWorkingCountQueryHandler" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public GetWorkingCountQueryHandler(
            IRedisConnection connection,
            RedisNames redisNames)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);

            _connection = connection;
            _redisNames = redisNames;
        }

        /// <inheritdoc />
        public long Handle(GetWorkingCountQuery query)
        {
            var db = _connection.Connection.GetDatabase();
            return db.SortedSetLength(_redisNames.Working);
        }
    }
}
