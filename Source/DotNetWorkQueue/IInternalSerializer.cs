// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Used internally by the queue to store the configuration and message headers
    /// <remarks>
    /// We don't use the message serialization here, because of the risk of the message interceptors having side affects on the
    /// queue configuration. Since message headers may contain important meta data about a message, they are stored and handled separately
    /// </remarks>
    /// </summary>
    public interface IInternalSerializer
    {
        /// <summary>
        /// Converts an input class to bytes.
        /// </summary>
        /// <typeparam name="T">Input type</typeparam>
        /// <param name="data">The data to serialize</param>
        /// <returns></returns>
        byte[] ConvertToBytes<T>(T data) where T : class;

        /// <summary>
        /// Converts an input class to a string
        /// </summary>
        /// <typeparam name="T">Input type</typeparam>
        /// <param name="data">The data to serialize</param>
        /// <returns></returns>
        string ConvertToString<T>(T data) where T : class;

        /// <summary>
        /// Converts the bytes back to the input class
        /// </summary>
        /// <typeparam name="T">output type</typeparam>
        /// <param name="bytes">The data to de-serialize.</param>
        /// <returns></returns>
        T ConvertBytesTo<T>(byte[] bytes) where T : class;
    }
}
