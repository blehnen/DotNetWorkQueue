using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class MoveDelayedRecordsCommandHandler : ICommandHandlerWithOutput<MoveDelayedRecordsCommand, long>
    {
        private readonly MoveDelayedToPendingLua _moveDelayedToPendingLua;
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly RedisQueueTransportOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveDelayedRecordsCommandHandler" /> class.
        /// </summary>
        /// <param name="moveDelayedToPendingLua">The move delayed to pending lua.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="options">The options.</param>
        public MoveDelayedRecordsCommandHandler( 
            MoveDelayedToPendingLua moveDelayedToPendingLua, 
            IUnixTimeFactory unixTimeFactory, 
            RedisQueueTransportOptions options)
        {
            Guard.NotNull(() => moveDelayedToPendingLua, moveDelayedToPendingLua);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            Guard.NotNull(() => options, options);

            _moveDelayedToPendingLua = moveDelayedToPendingLua;
            _unixTimeFactory = unixTimeFactory;
            _options = options;
        }

        /// <inheritdoc />
        public long Handle(MoveDelayedRecordsCommand command)
        {
            return command.Token.IsCancellationRequested
                ? 0
                : _moveDelayedToPendingLua.Execute(_unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds(),
                    _options.MoveDelayedMessagesBatchLimit);
        }
    }
}
