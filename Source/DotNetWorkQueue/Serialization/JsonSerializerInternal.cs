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
using System.Text;
using DotNetWorkQueue.Validation;
using JsonNet.PrivateSettersContractResolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
        private readonly JsonSerializerSettings _serializeSettings;
        private readonly JsonSerializerSettings _deserializeSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializerInternal"/> class.
        /// </summary>
        /// <param name="serializationBinder">The serialization binder used to control type resolution during deserialization.</param>
        public JsonSerializerInternal(ISerializationBinder serializationBinder)
        {
            Guard.NotNull(() => serializationBinder, serializationBinder);
            _serializeSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                SerializationBinder = serializationBinder
            };
            _deserializeSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ContractResolver = new PrivateSetterContractResolver(),
                SerializationBinder = serializationBinder
            };
        }

        /// <summary>
        /// Converts an input class to bytes.
        /// </summary>
        /// <typeparam name="T">the input type</typeparam>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public byte[] ConvertToBytes<T>(T data) where T : class
        {
            Guard.NotNull(() => data, data);
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data, _serializeSettings));
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
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes), _deserializeSettings);
        }
    }
}
