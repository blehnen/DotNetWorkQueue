using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Gets the current error record count
    /// </summary>
    internal class GetErrorCountQueryHandler : IQueryHandler<GetErrorCountQuery, long>
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetErrorCountQueryHandler" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public GetErrorCountQueryHandler(
            IRedisConnection connection,
            RedisNames redisNames)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);

            _connection = connection;
            _redisNames = redisNames;
        }

        /// <inheritdoc />
        public long Handle(GetErrorCountQuery query)
        {
            var db = _connection.Connection.GetDatabase();
            return db.ListLength(_redisNames.Error);
        }
    }
}
