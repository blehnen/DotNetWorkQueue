using System.Threading;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Metrics.Decorator
{
    /// <inheritdoc />
    internal class DelayedProcessingActionDecorator: IDelayedProcessingAction
    {
        private readonly IDelayedProcessingAction _handler;
        private readonly ITimer _timer;
        private readonly ICounter _counter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedProcessingActionDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public DelayedProcessingActionDecorator(IMetrics metrics,
            IDelayedProcessingAction handler,
            IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => metrics, metrics);
            Guard.NotNull(() => handler, handler);

            var name = handler.GetType().Name;
            _timer = metrics.Timer($"{connectionInformation.QueueName}.{name}.RunTimer", Units.Calls);
            _counter = metrics.Counter($"{connectionInformation.QueueName}.{name}.RunCounter", Units.Items);
            _handler = handler;
        }
        /// <inheritdoc />
        public long Run(CancellationToken token)
        {
            using (_timer.NewContext())
            {
                var count = _handler.Run(token);
                _counter.Increment(count);
                return count;
            }
        }
    }
}
