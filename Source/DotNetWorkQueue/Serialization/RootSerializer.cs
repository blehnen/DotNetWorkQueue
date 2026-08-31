// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Serialization
{
    /// <summary>
    /// Wraps messages serialization for interception
    /// </summary>
    public class RootSerializer : ASerializer
    {
        private readonly ISerializer _serializer;
        private readonly ISerializerResolver _resolver;
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="RootSerializer"/> class.
        /// </summary>
        /// <param name="messageInterceptors">The message interceptors.</param>
        /// <param name="serializer">The serializer used to write message bodies.</param>
        /// <param name="resolver">Selects the serializer used to read a body back.</param>
        public RootSerializer(IMessageInterceptorRegistrar messageInterceptors, ISerializer serializer,
            ISerializerResolver resolver)
            : base(messageInterceptors)
        {
            Guard.NotNull(serializer);
            Guard.NotNull(resolver);
            _serializer = serializer;
            _resolver = resolver;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Converts a message to a byte array
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="headers">the message headers</param>
        /// <returns></returns>
        protected override byte[] ConvertMessageToBytes<T>(T message, IReadOnlyDictionary<string, object> headers)
        {
            Guard.NotNull(message);
            return _serializer.ConvertMessageToBytes(message, headers);
        }

        /// <summary>
        /// Converts a byte array to a message
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bytes">The bytes.</param>
        /// <param name="headers">the message headers</param>
        /// <returns></returns>
        protected override T ConvertBytesToMessage<T>(byte[] bytes, IReadOnlyDictionary<string, object> headers)
        {
            Guard.NotNull(bytes);
            return ReaderFor(headers).ConvertBytesToMessage<T>(bytes, headers);
        }

        /// <summary>
        /// Picks the serializer that wrote this body.
        /// </summary>
        /// <remarks>
        /// Every transport reaches deserialization through here, so resolving in this one place is
        /// what keeps the transports out of it entirely. The headers are always deserialized before
        /// the body - they carry the interceptor graph, which is needed first - so the marker is
        /// available by the time it is wanted.
        /// </remarks>
        private ISerializer ReaderFor(IReadOnlyDictionary<string, object> headers)
        {
            if (headers == null) return _resolver.Fallback;
            if (!headers.TryGetValue(SerializerIdHeaderName, out var stamped)) return _resolver.Fallback;

            //A marker that is absent, or present but null, means the message predates the header.
            if (stamped == null) return _resolver.Fallback;
            if (stamped is string serializerId) return _resolver.Resolve(serializerId);

            //Anything else is a corrupt or forged header rather than a legacy message. Casting it
            //away would quietly select the fallback and read the body with the wrong serializer,
            //which is the silent-corruption case the resolver exists to prevent.
            throw new DotNetWorkQueueException(
                $"The '{SerializerIdHeaderName}' header holds a " +
                $"{stamped.GetType().FullName} instead of a string, so the serializer that wrote " +
                "this message cannot be determined.");
        }

        /// <summary>
        /// The header the producer stamps. Read by name rather than through IStandardHeaders
        /// because only the deserialized header dictionary is available at this point, not the
        /// message context.
        /// </summary>
        internal const string SerializerIdHeaderName = "Queue-SerializerId";
        #endregion
    }
}
