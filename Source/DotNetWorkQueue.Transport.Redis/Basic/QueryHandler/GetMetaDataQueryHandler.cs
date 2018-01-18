using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    /// <inheritdoc />
    internal class GetMetaDataQueryHandler : IQueryHandler<GetMetaDataQuery, RedisMetaData>
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly IInternalSerializer _internalSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryHandler" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        /// <param name="internalSerializer">The internal serializer.</param>
        public GetMetaDataQueryHandler(
            IRedisConnection connection,
            RedisNames redisNames, 
            IInternalSerializer internalSerializer)
        {
            Guard.NotNull(() => internalSerializer, internalSerializer);
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);

            _connection = connection;
            _redisNames = redisNames;
            _internalSerializer = internalSerializer;
        }

        /// <inheritdoc />
        public RedisMetaData Handle(GetMetaDataQuery query)
        {
            var db = _connection.Connection.GetDatabase();
            var result = (byte[])db.HashGet(_redisNames.MetaData, query.Id.Id.Value.ToString());
            if (result != null && result.Length > 0)
            {
                return _internalSerializer.ConvertBytesTo<RedisMetaData>(result);
            }
            return null;
        }
    }
}
