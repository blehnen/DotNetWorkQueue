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
    /// Contract for a message serializer
    /// </summary>
    public interface ISerializer
    {
        /// <summary>Converts the message to an array of bytes</summary>
        /// <typeparam name="T">the message type</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="headers">The message headers</param>
        /// <returns>byte array</returns>
        byte[] ConvertMessageToBytes<T>(T message, IReadOnlyDictionary<string, object> headers) where T : class;
        /// <summary>Converts the byte array to a message.</summary>
        /// <typeparam name="T">the message type</typeparam>
        /// <param name="bytes">The bytes.</param>
        /// <param name="headers">The message headers</param>
        /// <returns>an instance of T</returns>
        T ConvertBytesToMessage<T>(byte[] bytes, IReadOnlyDictionary<string, object> headers) where T : class;

        /// <summary>
        /// Gets the display name for logging or display purposes
        /// </summary>
        /// <value>
        /// The display name.
        /// </value>
        string DisplayName { get; }

        /// <summary>
        /// A stable identifier for this serializer, written into the message headers so the
        /// consumer can pick the right serializer to read a body back.
        /// </summary>
        /// <remarks>
        /// This value ends up on the wire, so it must not change once messages have been written
        /// with it - treat it like a database column name rather than a display string. Prefer a
        /// short constant over anything derived from the type, since renaming or moving the class
        /// would then strand every message already in a queue.
        /// <para>
        /// The default is the implementing type's full name, so existing implementations keep
        /// working without change and still get a usable identity.
        /// </para>
        /// </remarks>
        string SerializerId => GetType().FullName;
    }
}
