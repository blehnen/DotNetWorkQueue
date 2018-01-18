using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Redis.Basic.Time;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Factory
{
    /// <inheritdoc />
    internal class UnixTimeFactory : IUnixTimeFactory
    {
        private readonly IContainerFactory _container;
        private readonly RedisQueueTransportOptions _options;
        /// <summary>
        /// Initializes a new instance of the <see cref="UnixTimeFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="options">The options.</param>
        public UnixTimeFactory(IContainerFactory container, RedisQueueTransportOptions options)
        {
            Guard.NotNull(() => container, container);
            Guard.NotNull(() => options, options);

            _container = container;
            _options = options;
        }

        /// <inheritdoc />
        public IUnixTime Create()
        {
            switch (_options.TimeServer)
            {
                case TimeLocations.LocalMachine:
                    return _container.Create().GetInstance<LocalMachineUnixTime>();
                case TimeLocations.RedisServer:
                    return _container.Create().GetInstance<RedisServerUnixTime>();
                case TimeLocations.SntpServer:
                    return _container.Create().GetInstance<SntpUnixTime>();
                case TimeLocations.Custom:
                    return _container.Create().GetInstance<IUnixTime>();
                default:
                    throw new DotNetWorkQueueException($"unhandled type of {_options.TimeServer}");
            }
        }
    }
}
