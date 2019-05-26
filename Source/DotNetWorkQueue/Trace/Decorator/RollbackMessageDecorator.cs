using System;
using System.Collections.Generic;
using System.Text;
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Sends trace information for rollbacks
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IRollbackMessage" />
    public class RollbackMessageDecorator: IRollbackMessage
    {
        private readonly ITracer _tracer;
        private readonly IRollbackMessage _handler;
        private readonly IStandardHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public RollbackMessageDecorator(IRollbackMessage handler, ITracer tracer, IStandardHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
        }

        /// <inheritdoc />
        public bool Rollback(IMessageContext context)
        {
            var spanContext = context.Extract(_tracer, _headers);
            if (spanContext != null)
            {
                using (IScope scope = _tracer.BuildSpan("RollBack").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                {
                    return _handler.Rollback(context);
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan("RollBack").StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.AddMessageIdTag(context);
                    return _handler.Rollback(context);
                }
            }
        }
    }
}
