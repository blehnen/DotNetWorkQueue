// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Diagnostics;
using OpenTelemetry.Trace;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Traces the serialization logic
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ISerializer" />
    public class SerializerDecorator : ISerializer
    {
        private readonly ActivitySource _tracer;
        private readonly ISerializer _handler;
        private readonly IStandardHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitMessageDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public SerializerDecorator(ISerializer handler, ActivitySource tracer, IStandardHeaders headers)
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
            var activityContext = headers.Extract(_tracer, _headers);
            using (var scope = _tracer.StartActivity($"MessageSerializerMessageToBytes{_handler.DisplayName}", ActivityKind.Internal, activityContext))
            {
                var output = _handler.ConvertMessageToBytes(message, headers);
                scope?.SetTag("Length", output.Length.ToString());
                return output;
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
            var ActivityContext = headers.Extract(_tracer, _headers);
            using (var scope = _tracer.StartActivity($"MessageSerializerBytesToMessage{_handler.DisplayName}", ActivityKind.Internal, ActivityContext))
            {
                return _handler.ConvertBytesToMessage<T>(bytes, headers);
            }
        }

        /// <inheritdoc />
        public string DisplayName { get; }
    }
}
