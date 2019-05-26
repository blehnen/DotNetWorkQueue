using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    internal class MessageHandlerAsyncDecorator : IMessageHandlerAsync
    {
        private readonly IMessageHandlerAsync _handler;
        private readonly ITracer _tracer;
        private readonly IHeaders _headers;

        public MessageHandlerAsyncDecorator(IMessageHandlerAsync handler, ITracer tracer, IHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
        }

        public async Task HandleAsync(IReceivedMessageInternal message, IWorkerNotification workerNotification)
        {
            var spanContext = message.Extract(_tracer, _headers.StandardHeaders);
            if (spanContext != null)
            {
                using (IScope scope = _tracer.BuildSpan("MessageHandlerAsync").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.AddMessageIdTag(message);
                    await _handler.HandleAsync(message, workerNotification);
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan("MessageHandlerAsync").StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.AddMessageIdTag(message);
                    await _handler.HandleAsync(message, workerNotification);
                }
            }
        }
    }
}
