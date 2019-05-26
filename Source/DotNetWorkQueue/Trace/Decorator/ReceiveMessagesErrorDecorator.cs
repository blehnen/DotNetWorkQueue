using System;
using System.Collections.Generic;
using System.Text;
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Tracer for receiving a message
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IReceiveMessagesError" />
    public class ReceiveMessagesErrorDecorator: IReceiveMessagesError
    {
        private readonly ITracer _tracer;
        private readonly IReceiveMessagesError _handler;
        private readonly IStandardHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessagesErrorDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public ReceiveMessagesErrorDecorator(IReceiveMessagesError handler, ITracer tracer, IStandardHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
        }

        /// <inheritdoc />
        public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context, Exception exception)
        {
            var spanContext = message.Extract(_tracer, _headers);
            if (spanContext != null)
            {
                using (IScope scope = _tracer.BuildSpan("Error").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.Log(exception.ToString());
                    return _handler.MessageFailedProcessing(message, context, exception);
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan("Error").StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.AddMessageIdTag(message);
                    scope.Span.Log(exception.ToString());
                    return _handler.MessageFailedProcessing(message, context, exception);
                }
            }
        }
    }
}
