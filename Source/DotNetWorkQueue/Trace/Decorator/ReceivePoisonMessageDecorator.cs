// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using DotNetWorkQueue.Exceptions;
using OpenTracing;
using OpenTracing.Tag;

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
                        Tags.Error.Set(scope.Span, true);
                        _handler.Handle(context, exception);
                    }
                }
                else
                {
                    using (IScope scope = _tracer.BuildSpan("PoisonMessage").StartActive(finishSpanOnDispose: true))
                    {
                        scope.Span.AddMessageIdTag(context);
                        scope.Span.Log(exception.ToString());
                        Tags.Error.Set(scope.Span, true);
                        _handler.Handle(context, exception);
                    }
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan("PoisonMessage").StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.AddMessageIdTag(context);
                    scope.Span.Log(exception.ToString());
                    _handler.Handle(context, exception);
                }
            }
        }
    }
}
