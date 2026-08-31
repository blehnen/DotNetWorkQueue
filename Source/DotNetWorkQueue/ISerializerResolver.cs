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

namespace DotNetWorkQueue
{
    /// <summary>
    /// Chooses which serializer reads a message body back, based on the serializer that wrote it.
    /// </summary>
    /// <remarks>
    /// The producer stamps <see cref="IStandardHeaders.SerializerId"/> on every message. The
    /// consumer reads the headers before the body - they already select the message interceptors -
    /// so by the time the body is deserialized the right serializer is known.
    /// <para>
    /// Messages written before the header existed do not carry it, and neither do messages written
    /// by a producer that has not been upgraded. Those fall back to <see cref="Fallback"/>.
    /// </para>
    /// </remarks>
    public interface ISerializerResolver
    {
        /// <summary>
        /// The serializer used for a message that carries no serializer header.
        /// </summary>
        /// <remarks>
        /// Defaults to the serializer registered for the queue, which reproduces the behaviour that
        /// applied before the header existed: whatever was registered read everything.
        /// <para>
        /// A caller who changes the registered serializer must set this to the serializer that
        /// wrote the existing backlog, or those messages become unreadable. Changing the serializer
        /// on a queue that already holds messages is the case this exists for.
        /// </para>
        /// </remarks>
        ISerializer Fallback { get; }

        /// <summary>
        /// Returns the serializer that wrote a body, given the value of its serializer header.
        /// </summary>
        /// <param name="serializerId">
        /// The header value, or null/empty for a message that predates the header.
        /// </param>
        /// <returns>The serializer to read the body with; never null.</returns>
        /// <exception cref="DotNetWorkQueue.Exceptions.DotNetWorkQueueException">
        /// The message names a serializer that is not registered. Deserializing it with a different
        /// serializer would produce silent corruption rather than an error, so this throws instead.
        /// </exception>
        ISerializer Resolve(string serializerId);

        /// <summary>The serializers this resolver can select from, keyed by their identifier.</summary>
        IReadOnlyDictionary<string, ISerializer> Registered { get; }
    }
}
