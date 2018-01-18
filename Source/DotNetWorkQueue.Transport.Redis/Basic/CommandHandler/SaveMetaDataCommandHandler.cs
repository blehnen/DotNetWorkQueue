using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class SaveMetaDataCommandHandler : ICommandHandler<SaveMetaDataCommand>
    {
        private readonly IInternalSerializer _internalSerializer;
        private readonly RedisNames _redisNames;
        private readonly IRedisConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveMetaDataCommandHandler" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        /// <param name="internalSerializer">The internal serializer.</param>
        public SaveMetaDataCommandHandler(IRedisConnection connection, 
            RedisNames redisNames, 
            IInternalSerializer internalSerializer)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);
            Guard.NotNull(() => internalSerializer, internalSerializer);

            _redisNames = redisNames;
            _internalSerializer = internalSerializer;
            _connection = connection;
        }

        /// <inheritdoc />
        public void Handle(SaveMetaDataCommand command)
        {
            var db = _connection.Connection.GetDatabase();
            db.HashSet(_redisNames.MetaData, command.Id.Id.Value.ToString(),
                _internalSerializer.ConvertToBytes(command.MetaData));
        }
    }
}
