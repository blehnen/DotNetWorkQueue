using System.Threading;
using DotNetWorkQueue.Transport.Redis.Basic.Command;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <inheritdoc />
    internal class RedisQueueClearExpiredMessages: IClearExpiredMessages
    {
        private readonly ICommandHandlerWithOutput<ClearExpiredMessagesCommand, long> _commandReset;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueClearExpiredMessages"/> class.
        /// </summary>
        /// <param name="commandReset">The command reset.</param>
        public RedisQueueClearExpiredMessages(ICommandHandlerWithOutput<ClearExpiredMessagesCommand, long> commandReset)
        {
            _commandReset = commandReset;
        }

        /// <inheritdoc />
        public long ClearMessages(CancellationToken cancelToken)
        {
            var counter = _commandReset.Handle(new ClearExpiredMessagesCommand());
            var total = counter;
            while (counter > 0)
            {
                if (cancelToken.IsCancellationRequested)
                    return total;
                counter = _commandReset.Handle(new ClearExpiredMessagesCommand());
                total += counter;
            }
            return total;
        }
    }
}
