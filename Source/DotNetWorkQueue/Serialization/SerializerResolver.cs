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
using System;
using System.Collections.Generic;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Serialization
{
    /// <summary>
    /// The default <see cref="ISerializerResolver"/>.
    /// </summary>
    /// <remarks>
    /// Out of the box this knows about one serializer - the one registered for the queue - and uses
    /// it for everything, marked or not, which is exactly what happened before the serializer
    /// header existed. Registering a second serializer is what makes the header meaningful.
    /// <para>
    /// Register additional serializers during transport initialization, before the queue starts.
    /// This type is not safe against registration racing deserialization, for the same reason
    /// <see cref="AllowListSerializationBinder"/> is not: it is startup configuration.
    /// </para>
    /// </remarks>
    public class SerializerResolver : ISerializerResolver
    {
        private readonly Dictionary<string, ISerializer> _serializers =
            new Dictionary<string, ISerializer>(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerResolver"/> class.
        /// </summary>
        /// <param name="registered">The serializer registered for this queue. Becomes the fallback.</param>
        public SerializerResolver(ISerializer registered)
        {
            Guard.NotNull(registered);
            Add(registered);
            Fallback = registered;
        }

        /// <inheritdoc />
        public ISerializer Fallback { get; private set; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, ISerializer> Registered => _serializers;

        /// <summary>
        /// Makes a serializer available for reading messages that name it.
        /// </summary>
        /// <param name="serializer">The serializer to add.</param>
        /// <remarks>
        /// Adding a serializer only lets the consumer <em>read</em> messages it wrote. What writes
        /// new messages is whatever is registered as <see cref="ISerializer"/> in the container.
        /// </remarks>
        /// <exception cref="DotNetWorkQueueException">
        /// A different serializer is already registered under the same identifier. Two serializers
        /// sharing an id would make the header ambiguous.
        /// </exception>
        public void Add(ISerializer serializer)
        {
            Guard.NotNull(serializer);
            var id = serializer.SerializerId;
            Guard.NotNullOrEmpty(id);

            if (_serializers.TryGetValue(id, out var existing) && !ReferenceEquals(existing, serializer))
            {
                throw new DotNetWorkQueueException(
                    $"A different serializer is already registered as '{id}'. Serializer identifiers " +
                    "are written into message headers and must identify exactly one serializer.");
            }

            _serializers[id] = serializer;
        }

        /// <summary>
        /// Sets the serializer used for messages that carry no serializer header.
        /// </summary>
        /// <param name="serializer">The serializer that wrote the existing backlog.</param>
        /// <remarks>
        /// Set this when changing the serializer a queue writes with. Everything already in the
        /// queue was written by the old one and carries no header if it predates this feature.
        /// </remarks>
        public void SetFallback(ISerializer serializer)
        {
            Guard.NotNull(serializer);
            Add(serializer);
            Fallback = serializer;
        }

        /// <inheritdoc />
        public ISerializer Resolve(string serializerId)
        {
            //no header: the message predates the header, or came from a producer that has not been
            //upgraded. Either way the fallback is the caller's declaration of what wrote it.
            if (string.IsNullOrEmpty(serializerId)) return Fallback;

            if (_serializers.TryGetValue(serializerId, out var serializer)) return serializer;

            //Deliberately a throw rather than a fall back to the default. Reading a body with the
            //wrong serializer does not reliably fail - it can return a half-populated object - and
            //a poison message is a great deal easier to diagnose than silent data loss.
            throw new DotNetWorkQueueException(
                $"The message was written by serializer '{serializerId}', which is not registered for " +
                "this queue. Register it so the message can be read, or the message cannot be processed.");
        }
    }
}
