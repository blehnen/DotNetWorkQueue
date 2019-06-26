using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Logging.Decorator
{
    /// <inheritdoc />
    internal class ReceiveMessageQueryDecorator : IQueryHandler<ReceiveMessageQuery, RedisMessage>
    {
        private readonly IQueryHandler<ReceiveMessageQuery, RedisMessage> _handler;
        private readonly ILog _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryDecorator" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="handler">The handler.</param>
        public ReceiveMessageQueryDecorator(ILogFactory log,
            IQueryHandler<ReceiveMessageQuery, RedisMessage> handler)
        {
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => handler, handler);

            _log = log.Create();
            _handler = handler;
        }

        /// <inheritdoc />
        public RedisMessage Handle(ReceiveMessageQuery query)
        {
            var result = _handler.Handle(query);
            if (result != null && result.Expired)
            {
                _log.DebugFormat("Message {0} expired before it could be processed", result.MessageId);
            }
            return result;
        }
    }
}
