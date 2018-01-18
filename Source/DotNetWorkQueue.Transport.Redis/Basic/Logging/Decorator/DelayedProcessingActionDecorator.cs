using System.Threading;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Logging.Decorator
{
    /// <inheritdoc />
    internal class DelayedProcessingActionDecorator: IDelayedProcessingAction
    {
        private readonly IDelayedProcessingAction _handler;
        private readonly ILog _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryDecorator" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="handler">The handler.</param>
        public DelayedProcessingActionDecorator(ILogFactory log,
            IDelayedProcessingAction handler)
        {
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => handler, handler);

            _log = log.Create();
            _handler = handler;
        }

        /// <inheritdoc />
        public long Run(CancellationToken token)
        {
            var records = _handler.Run(token);
            if (records > 0)
            {
                _log.InfoFormat("Moved {0} records from the delayed queue to the pending queue", records);
            }
            return records;
        }
    }
}
