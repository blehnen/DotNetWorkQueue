using System.Threading;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Resets records that are outside of the heartbeat window
    /// </summary>
    internal class RedisQueueResetHeartBeat: IResetHeartBeat
    {
        private readonly ICommandHandlerWithOutput<ResetHeartBeatCommand, long> _commandReset;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueResetHeartBeat"/> class.
        /// </summary>
        /// <param name="commandReset">The command reset.</param>
        public RedisQueueResetHeartBeat(ICommandHandlerWithOutput<ResetHeartBeatCommand, long> commandReset)
        {
            Guard.NotNull(() => commandReset, commandReset);
            _commandReset = commandReset;
        }

        /// <summary>
        /// Used to find and reset work items that are out of the heart beat window
        /// </summary>
        /// <param name="cancelToken">The cancel token.</param>
        public long Reset(CancellationToken cancelToken)
        {
            var counter = _commandReset.Handle(new ResetHeartBeatCommand());
            var total = counter;
            while (counter > 0)
            {
                if (cancelToken.IsCancellationRequested)
                    return total;

                counter = _commandReset.Handle(new ResetHeartBeatCommand());
                total = total + counter;
            }
            return total;
        }
    }
}
