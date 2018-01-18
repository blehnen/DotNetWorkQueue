using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Redis.Basic.MessageID;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Factory
{
    /// <inheritdoc />
    internal class GetMessageIdFactory : IGetMessageIdFactory
    {
        private readonly IContainerFactory _container;
        private readonly RedisQueueTransportOptions _options;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetMessageIdFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="options">The options.</param>
        public GetMessageIdFactory(IContainerFactory container, RedisQueueTransportOptions options)
        {
            Guard.NotNull(() => container, container);
            Guard.NotNull(() => options, options);

            _container = container;
            _options = options;
        }
        /// <inheritdoc />
        public IGetMessageId Create()
        {
            switch (_options.MessageIdLocation)
            {
                case MessageIdLocations.Uuid:
                    return _container.Create().GetInstance<GetUuidMessageId>();
                case MessageIdLocations.RedisIncr:
                    return _container.Create().GetInstance<GetRedisIncrId>();
                case MessageIdLocations.Custom:
                    return _container.Create().GetInstance<IGetMessageId>();
                default:
                    throw new DotNetWorkQueueException($"unhandled type of {_options.MessageIdLocation}");
            }
        }
    }
}
