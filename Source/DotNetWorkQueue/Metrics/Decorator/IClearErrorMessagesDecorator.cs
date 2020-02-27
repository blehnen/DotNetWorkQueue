using System.Threading;

namespace DotNetWorkQueue.Metrics.Decorator
{
    internal class ClearErrorMessagesDecorator : IClearErrorMessages
    {
        private readonly IClearErrorMessages _handler;
        private readonly ITimer _timer;
        private readonly ICounter _counter;

        /// <summary>
        /// Initializes a new instance of the <see cref="IClearExpiredMessages" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public ClearErrorMessagesDecorator(IMetrics metrics,
            IClearErrorMessages handler,
            IConnectionInformation connectionInformation)
        {
            var name = "ClearErrorMessages";
            _timer = metrics.Timer($"{connectionInformation.QueueName}.{name}.ClearMessages.ResetTimer", Units.Calls);
            _counter = metrics.Counter($"{connectionInformation.QueueName}.{name}.ClearMessages.ResetCounter", Units.Items);
            _handler = handler;
        }

        /// <inheritdoc />
        public long ClearMessages(CancellationToken cancelToken)
        {
            using (_timer.NewContext())
            {
                var count = _handler.ClearMessages(cancelToken);
                _counter.Increment(count);
                return count;
            }
        }
    }
}
