using System;
using System.Collections.Generic;
using System.Text;
using DotNetWorkQueue.Messages;
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Tracer for linq message handling
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IMessageMethodHandling" />
    public class MessageMethodHandlingDecorator: IMessageMethodHandling
    {
        private readonly IMessageMethodHandling _handler;
        private readonly ITracer _tracer;
        private readonly IStandardHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageMethodHandlingDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public MessageMethodHandlingDecorator(IMessageMethodHandling handler, ITracer tracer, IStandardHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _handler.Dispose();
        }

        /// <inheritdoc />
        public bool IsDisposed => _handler.IsDisposed;

        /// <inheritdoc />
        public void HandleExecution(IReceivedMessage<MessageExpression> receivedMessage, IWorkerNotification workerNotification)
        {
            var spanContext = receivedMessage.Headers.Extract(_tracer, _headers);
            if (spanContext != null)
            {
                using (IScope scope = _tracer.BuildSpan("LinqExecution").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.SetTag("ActionType", receivedMessage.Body.PayLoad.ToString());
                    _handler.HandleExecution(receivedMessage, workerNotification);
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan("LinqExecution").StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.SetTag("ActionType", receivedMessage.Body.PayLoad.ToString());
                    _handler.HandleExecution(receivedMessage, workerNotification);
                }
            }
        }
    }
}
