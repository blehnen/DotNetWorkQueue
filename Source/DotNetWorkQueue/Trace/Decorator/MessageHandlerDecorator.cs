using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Tracer for message execution
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IMessageHandler" />
    public class MessageHandlerDecorator: IMessageHandler
    {
        private readonly IMessageHandler _handler;
        private readonly ITracer _tracer;
        private readonly IStandardHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public MessageHandlerDecorator(IMessageHandler handler, ITracer tracer, IStandardHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
        }

        /// <inheritdoc />
        public void Handle(IReceivedMessageInternal message, IWorkerNotification workerNotification)
        {
            var spanContext = message.Extract(_tracer, _headers);
            if (spanContext != null)
            {
                using (IScope scope = _tracer.BuildSpan("MessageHandler").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                {
                    _handler.Handle(message, workerNotification);
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan("MessageHandler").StartActive(finishSpanOnDispose: true))
                {
                    _handler.Handle(message, workerNotification);
                }
            }
        }
    }
}
