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
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    public class MessageInterceptorDecorator: IMessageInterceptor
    {
        private readonly ITracer _tracer;
        private readonly IMessageInterceptor _handler;
        private readonly IStandardHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitMessageDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public MessageInterceptorDecorator(IMessageInterceptor handler, ITracer tracer, IStandardHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
            DisplayName = _handler.DisplayName;
        }

        /// <inheritdoc />
        public MessageInterceptorResult MessageToBytes(byte[] input, IReadOnlyDictionary<string, object> headers)
        {
            var spanContext = headers.Extract(_tracer, _headers);
            if (spanContext != null && _tracer.ActiveSpan == null)
            {
                using (IScope scope = _tracer.BuildSpan($"MessageInterceptorMessageToBytes{_handler.DisplayName}").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.SetTag("InputLength", input.Length.ToString());
                    var result = _handler.MessageToBytes(input, headers);
                    scope.Span.SetTag("AddedToGraph", result.AddToGraph);
                    if (result.AddToGraph)
                    {
                        scope.Span.SetTag("OutputLength", result.Output.Length.ToString());
                    }
                    return result;
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan($"MessageInterceptorMessageToBytes{_handler.DisplayName}").StartActive(finishSpanOnDispose: true))
                {
                    scope.Span.SetTag("InputLength", input.Length.ToString());
                    var result = _handler.MessageToBytes(input, headers);
                    scope.Span.SetTag("AddedToGraph", result.AddToGraph);
                    if (result.AddToGraph)
                    {
                        scope.Span.SetTag("OutputLength", result.Output.Length.ToString());
                    }
                    return result;
                }
            }
        }

        /// <inheritdoc />
        public byte[] BytesToMessage(byte[] input, IReadOnlyDictionary<string, object> headers)
        {
            var spanContext = headers.Extract(_tracer, _headers);
            if (spanContext != null)
            {
                using (IScope scope = _tracer.BuildSpan($"MessageInterceptorBytesToMessage{_handler.DisplayName}").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                {
                    return _handler.BytesToMessage(input, headers);
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan($"MessageInterceptorBytesToMessage{_handler.DisplayName}").StartActive(finishSpanOnDispose: true))
                {
                    return _handler.BytesToMessage(input, headers);
                }
            }
        }

        /// <inheritdoc />
        public string DisplayName { get; }

        /// <inheritdoc />
        public Type BaseType => _handler.BaseType;
    }
}
