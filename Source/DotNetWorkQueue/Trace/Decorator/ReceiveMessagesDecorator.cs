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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Decorator for receiving a message
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IReceiveMessages" />
    public class ReceiveMessagesDecorator : IReceiveMessages
    {
        private readonly IReceiveMessages _handler;
        private readonly ITracer _tracer;
        private readonly IHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessagesDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public ReceiveMessagesDecorator(IReceiveMessages handler, ITracer tracer, IHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
        }

        /// <inheritdoc />
        public IReceivedMessageInternal ReceiveMessage(IMessageContext context)
        {
            //we can't attach the span since we don't have the parent until after we get a message
            //so save off the start and end times, and replace those in the child span below
            var start = DateTime.UtcNow;
            var message = _handler.ReceiveMessage(context);
            var end = DateTime.UtcNow;
            if (message == null) return null;
            var spanContext = message.Extract(_tracer, _headers.StandardHeaders);

            //blocking operations can last forever for queues that can signal for new messages
            //so, we will treat this is a 0 ms operation, rather than have it possibly last for N
            if (IsBlockingOperation)
                start = end;

            if (spanContext != null)
            {
                using (IScope scope = _tracer.BuildSpan("ReceiveMessage").AddReference(References.FollowsFrom, spanContext).WithStartTimestamp(start)
                    .StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.AddMessageIdTag(message);
                    scope.Span.SetTag("IsBlockingOperation", IsBlockingOperation);
                    scope.Span.Finish(end);
                    return message;
                }
            }

            using (IScope scope = _tracer.BuildSpan("ReceiveMessage").WithStartTimestamp(start).StartActive(finishSpanOnDispose: true))
            {
                scope.Span.AddMessageIdTag(message);
                scope.Span.SetTag("IsBlockingOperation", IsBlockingOperation);
                scope.Span.Finish(end);
                return message;
            }

        }

        /// <inheritdoc />
        public async Task<IReceivedMessageInternal> ReceiveMessageAsync(IMessageContext context)
        {
            //we can't attach the span since we don't have the parent until after we get a message
            //so save off the start and end times, and replace those in the child span below
            var start = DateTime.UtcNow;
            var message = await _handler.ReceiveMessageAsync(context);
            var end = DateTime.UtcNow;
            if (message == null) return null;
            var spanContext = message.Extract(_tracer, _headers.StandardHeaders);
            if (spanContext != null)
            {
                using (IScope scope = _tracer.BuildSpan("ReceiveMessage").AddReference(References.FollowsFrom, spanContext).WithStartTimestamp(start)
                    .StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.AddMessageIdTag(message);
                    scope.Span.Finish(end);
                    return message;
                }
            }
            using (IScope scope = _tracer.BuildSpan("ReceiveMessage").WithStartTimestamp(start).StartActive(finishSpanOnDispose: true))
            {
                scope.Span.AddMessageIdTag(message);
                scope.Span.Finish(end);
                return message;
            }
        }

        /// <inheritdoc />
        public bool IsBlockingOperation => _handler.IsBlockingOperation;
    }
}
