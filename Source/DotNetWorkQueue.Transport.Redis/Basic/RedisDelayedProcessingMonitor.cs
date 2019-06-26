using System.Threading;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <inheritdoc cref="IDelayedProcessingMonitor" />
    internal class RedisDelayedProcessingMonitor : BaseMonitor, IDelayedProcessingMonitor
    {
        #region Constructor
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisDelayedProcessingMonitor"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="log">The log.</param>
        /// <param name="options">The options.</param>
        public RedisDelayedProcessingMonitor(IDelayedProcessingAction action, ILogFactory log, RedisQueueTransportOptions options)
            : base(Guard.NotNull(() => action, action).Run, options.DelayedProcessingConfiguration, log)
        {

        }
        #endregion
    }

    /// <inheritdoc />
    internal class DelayedProcessingAction: IDelayedProcessingAction
    {
        private readonly ICommandHandlerWithOutput<MoveDelayedRecordsCommand, long> _moveRecords;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedProcessingAction"/> class.
        /// </summary>
        /// <param name="moveRecords">The move records.</param>
        public DelayedProcessingAction(ICommandHandlerWithOutput<MoveDelayedRecordsCommand, long> moveRecords)
        {
            Guard.NotNull(() => moveRecords, moveRecords);
            _moveRecords = moveRecords;
        }

        /// <inheritdoc />
        public long Run(CancellationToken token)
        {
            var records = _moveRecords.Handle(new MoveDelayedRecordsCommand(token));
            var total = records;
            while (records > 0)
            {
                if (token.IsCancellationRequested)
                    return total;
                records = _moveRecords.Handle(new MoveDelayedRecordsCommand(token));
                total += records;
            }
            return total;
        }
    }
}
