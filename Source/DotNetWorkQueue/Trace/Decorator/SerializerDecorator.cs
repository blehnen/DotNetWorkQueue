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
using System.Collections.Generic;
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Traces the serialization logic
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ISerializer" />
    public class SerializerDecorator: ISerializer
    {
        private readonly ITracer _tracer;
        private readonly ISerializer _handler;
        private readonly IStandardHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitMessageDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public SerializerDecorator(ISerializer handler, ITracer tracer, IStandardHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
            DisplayName = _handler.DisplayName;
        }

        /// <summary>
        /// Converts the message to an array of bytes
        /// </summary>
        /// <typeparam name="T">the message type</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="headers">The message headers</param>
        /// <returns>
        /// byte array
        /// </returns>
        public byte[] ConvertMessageToBytes<T>(T message, IReadOnlyDictionary<string, object> headers) where T : class
        {
            var spanContext = headers.Extract(_tracer, _headers);
            if (spanContext != null && _tracer.ActiveSpan == null)
            {
                using (IScope scope = _tracer.BuildSpan($"MessageSerializerMessageToBytes{_handler.DisplayName}").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                {
                    var output = _handler.ConvertMessageToBytes(message, headers);
                    scope.Span.SetTag("Length", output.Length.ToString());
                    return output;
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan($"MessageSerializerMessageToBytes{_handler.DisplayName}").StartActive(finishSpanOnDispose: true))
                {
                    var output = _handler.ConvertMessageToBytes(message, headers);
                    scope.Span.SetTag("Length", output.Length.ToString());
                    return output;
                }
            }
        }

        /// <summary>
        /// Converts the byte array to a message.
        /// </summary>
        /// <typeparam name="T">the message type</typeparam>
        /// <param name="bytes">The bytes.</param>
        /// <param name="headers">The message headers</param>
        /// <returns>
        /// an instance of T
        /// </returns>
        public T ConvertBytesToMessage<T>(byte[] bytes, IReadOnlyDictionary<string, object> headers) where T : class
        {
            var spanContext = headers.Extract(_tracer, _headers);
            if (spanContext != null)
            {
                using (IScope scope = _tracer.BuildSpan($"MessageSerializerBytesToMessage{_handler.DisplayName}").AddReference(References.FollowsFrom, spanContext).StartActive(finishSpanOnDispose: true))
                {
                    return _handler.ConvertBytesToMessage<T>(bytes, headers);
                }
            }
            else
            {
                using (IScope scope = _tracer.BuildSpan($"MessageSerializerBytesToMessage{_handler.DisplayName}").StartActive(finishSpanOnDispose: true))
                {
                    return _handler.ConvertBytesToMessage<T>(bytes, headers);
                }
            }
        }

        /// <inheritdoc />
        public string DisplayName { get; }
    }
}
