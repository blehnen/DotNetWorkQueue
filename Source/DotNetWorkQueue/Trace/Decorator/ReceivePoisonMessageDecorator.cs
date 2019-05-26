using DotNetWorkQueue.Exceptions;
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Tracer for poison messages
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IReceivePoisonMessage" />
    public class ReceivePoisonMessageDecorator: IReceivePoisonMessage
    {
        private readonly ITracer _tracer;
        private readonly IReceivePoisonMessage _handler;
        private readonly IStandardHeaders _headers;
        private readonly IGetHeader _getHeader;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceivePoisonMessageDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="getHeader">The get header.</param>
        public ReceivePoisonMessageDecorator(IReceivePoisonMessage handler, ITracer tracer, IStandardHeaders headers, IGetHeader getHeader)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
            _getHeader = getHeader;
        }

        /// <inheritdoc />
        public void Handle(IMessageContext context, PoisonMessageException exception)
        {
            var header = _getHeader.GetHeaders(context.MessageId);
            if (header != null)
            {
                var spanContext = header.Extract(_tracer, _headers);
                if (spanContext != null)
                {
                    using (IScope scope = _tracer.BuildSpan("PoisonMessage").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                    {
                        scope.Span.Log(exception.ToString());
                        _handler.Handle(context, exception);
                    }
                }
                else
                {
                    using (IScope scope = _tracer.BuildSpan("PoisonMessage").StartActive(finishSpanOnDispose: true))
                    {
                        if (context.MessageId.HasValue)
                            scope.Span.SetTag("MessageID", context.MessageId.Id.Value.ToString());
                        scope.Span.Log(exception.ToString());
                        _handler.Handle(context, exception);
                    }
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan("PoisonMessage").StartActive(finishSpanOnDispose: true))
                {
                    if (context.MessageId.HasValue)
                        scope.Span.SetTag("MessageID", context.MessageId.Id.Value.ToString());
                    scope.Span.Log(exception.ToString());
                    _handler.Handle(context, exception);
                }
            }
        }
    }
}
