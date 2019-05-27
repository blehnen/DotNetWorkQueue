// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System.Threading.Tasks;
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    public class MessageHandlerAsyncDecorator : IMessageHandlerAsync
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
