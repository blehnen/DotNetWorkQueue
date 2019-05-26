using System;
using System.Collections.Generic;
using System.Text;
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Tracer for removing a message
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IRemoveMessage" />
    public class RemoveMessageDecorator: IRemoveMessage
    {
        private readonly ITracer _tracer;
        private readonly IRemoveMessage _handler;
        private readonly IStandardHeaders _headers;
        private readonly IGetHeader _getHeader;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveMessageDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="getHeader">The get header.</param>
        public RemoveMessageDecorator(IRemoveMessage handler, ITracer tracer, IStandardHeaders headers, IGetHeader getHeader)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
            _getHeader = getHeader;
        }

        /// <inheritdoc />
        public RemoveMessageStatus Remove(IMessageId id, RemoveMessageReason reason)
        {
            var header = _getHeader.GetHeaders(id);
            if (header != null)
            {
                var spanContext = header.Extract(_tracer, _headers);
                if (spanContext != null)
                {
                    using (IScope scope = _tracer.BuildSpan("Remove").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                    {
                        scope.Span.SetTag("RemovedBecause", reason.ToString());
                        return _handler.Remove(id, reason);
                    }
                }
                else
                {
                    using (IScope scope = _tracer.BuildSpan("Remove").StartActive(finishSpanOnDispose: true))
                    {
                        if (id.HasValue)
                            scope.Span.SetTag("MessageID", id.Id.Value.ToString());
                        scope.Span.SetTag("RemovedBecause", reason.ToString());
                        return _handler.Remove(id, reason);
                    }
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan("Remove").StartActive(finishSpanOnDispose: true))
                {
                    if (id.HasValue)
                        scope.Span.SetTag("MessageID", id.Id.Value.ToString());
                    scope.Span.SetTag("RemovedBecause", reason.ToString());
                    return _handler.Remove(id, reason);
                }
            }
        }

        /// <inheritdoc />
        public RemoveMessageStatus Remove(IMessageContext context, RemoveMessageReason reason)
        {
            var spanContext = context.Extract(_tracer, _headers);
            if (spanContext != null)
            {
                using (IScope scope = _tracer.BuildSpan("Remove").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.SetTag("RemovedBecause", reason.ToString());
                    return _handler.Remove(context, reason);
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan("Remove").StartActive(finishSpanOnDispose: true))
                {
                    if (context.MessageId.HasValue)
                        scope.Span.SetTag("MessageID", context.MessageId.Id.Value.ToString());
                    scope.Span.SetTag("RemovedBecause", reason.ToString());
                    return _handler.Remove(context, reason);
                }
            }
        }
    }
}
