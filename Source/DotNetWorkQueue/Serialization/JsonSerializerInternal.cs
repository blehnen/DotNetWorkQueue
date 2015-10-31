// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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

using System.Text;
using Newtonsoft.Json;
namespace DotNetWorkQueue.Serialization
{
    /// <summary>
    /// Used internally by the queue to store the configuration
    /// <remarks>
    /// We don't use the message serialization here, because of the risk of the message interceptors having side affects on the
    /// queue configuration
    /// </remarks>
    /// </summary>
    internal class JsonSerializerInternal : IInternalSerializer
    {
        /// <summary>
        /// Converts an input class to bytes.
        /// </summary>
        /// <typeparam name="T">the input type</typeparam>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public byte[] ConvertToBytes<T>(T message) where T : class
        {
            Guard.NotNull(() => message, message);
            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };
            return Encoding.UTF8.GetBytes((JsonConvert.SerializeObject(message, serializerSettings)));
        }

        /// <summary>
        /// Converts the bytes back to the input class
        /// </summary>
        /// <typeparam name="T">the output type</typeparam>
        /// <param name="bytes">The bytes.</param>
        /// <returns></returns>
        public T ConvertBytesTo<T>(byte[] bytes) where T : class
        {
            Guard.NotNull(() => bytes, bytes);
            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes), serializerSettings);
        }

        /// <summary>
        /// Converts an input class to a string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data to serialize</param>
        /// <returns></returns>
        public string ConvertToString<T>(T data) where T : class
        {
            Guard.NotNull(() => data, data);
            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None
            };
            return JsonConvert.SerializeObject(data, serializerSettings);
        }
    }
}
