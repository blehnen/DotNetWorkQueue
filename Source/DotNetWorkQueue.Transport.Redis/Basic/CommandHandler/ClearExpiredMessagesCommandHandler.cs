using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class ClearExpiredMessagesCommandHandler : ICommandHandlerWithOutput<ClearExpiredMessagesCommand, long>
    {
        private readonly ClearExpiredMessagesLua _clearExpiredMessagesLua;
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly RedisQueueTransportOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearExpiredMessagesCommandHandler" /> class.
        /// </summary>
        /// <param name="clearExpiredMessagesLua">The clear expired messages.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="options">The options.</param>
        public ClearExpiredMessagesCommandHandler(ClearExpiredMessagesLua clearExpiredMessagesLua, 
            IUnixTimeFactory unixTimeFactory, 
            RedisQueueTransportOptions options)
        {
            Guard.NotNull(() => clearExpiredMessagesLua, clearExpiredMessagesLua);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            Guard.NotNull(() => options, options);

            _clearExpiredMessagesLua = clearExpiredMessagesLua;
            _unixTimeFactory = unixTimeFactory;
            _options = options;
        }

        /// <inheritdoc />
        public long Handle(ClearExpiredMessagesCommand command)
        {
            return _clearExpiredMessagesLua.Execute(_unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds(), _options.ClearExpiredMessagesBatchLimit);
        }
    }
}
