using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Metrics.Decorator
{
    /// <inheritdoc />
    internal class ReceiveMessageQueryDecorator: IQueryHandler<ReceiveMessageQuery, RedisMessage>
    {
        private readonly IQueryHandler<ReceiveMessageQuery, RedisMessage> _handler;
        private readonly ICounter _counter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public ReceiveMessageQueryDecorator(IMetrics metrics,
            IQueryHandler<ReceiveMessageQuery, RedisMessage> handler,
            IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => metrics, metrics);
            Guard.NotNull(() => handler, handler);

            var name = handler.GetType().Name;
            _counter = metrics.Counter($"{connectionInformation.QueueName}.{name}.HandleAsync.Expired", Units.Items);
            _handler = handler;
        }

        /// <inheritdoc />
        public RedisMessage Handle(ReceiveMessageQuery query)
        {
            var result = _handler.Handle(query);
            if (result != null && result.Expired)
            {
                _counter.Increment(1);
            }
            return result;
        }
    }
}
