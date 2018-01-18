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
using System.Text;
using DotNetWorkQueue.Validation;
using Newtonsoft.Json;

namespace DotNetWorkQueue.Serialization
{
    /// <summary>
    /// Uses Newtonsoft.Json to serialize messages
    /// </summary>
    public class JsonSerializer : ISerializer
    {
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        #region Protected Methods
        /// <summary>
        /// Converts a message to a byte array
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public byte[] ConvertMessageToBytes<T>(T message)
             where T : class
        {
            Guard.NotNull(() => message, message);
            var wrapper = new SerializationWrapper<T> {Message = message};
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(wrapper, _serializerSettings));
        }

        /// <summary>
        /// Converts a byte array to a message
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bytes">The bytes.</param>
        /// <returns></returns>
        public T ConvertBytesToMessage<T>(byte[] bytes)
             where T : class
        {
            Guard.NotNull(() => bytes, bytes);
            var wrapper = JsonConvert.DeserializeObject<SerializationWrapper<T>>(Encoding.UTF8.GetString(bytes), _serializerSettings);
            return wrapper.Message;
        }
        #endregion
        /// <summary>
        /// A wrapper class to avoid issues with being passed a dictionary as the top level object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class SerializationWrapper<T>
        {
            /// <summary>
            /// Gets or sets the message.
            /// </summary>
            /// <value>
            /// The message.
            /// </value>
            public T Message { get; set; }
        }
    }
}
