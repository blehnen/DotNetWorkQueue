using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    /// <inheritdoc />
    public class GetJobIdQueryHandler : IQueryHandler<GetJobIdQuery, string>
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetErrorCountQueryHandler" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public GetJobIdQueryHandler(
            IRedisConnection connection,
            RedisNames redisNames)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);

            _connection = connection;
            _redisNames = redisNames;
        }

        /// <inheritdoc />
        public string Handle(GetJobIdQuery query)
        {
            var db = _connection.Connection.GetDatabase();
            return db.HashGet(_redisNames.JobNames, query.JobName);
        }
    }
}
