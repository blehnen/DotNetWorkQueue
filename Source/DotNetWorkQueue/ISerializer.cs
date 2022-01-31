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
    }
}
