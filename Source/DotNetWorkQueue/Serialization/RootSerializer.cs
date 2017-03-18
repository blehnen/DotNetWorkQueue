// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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

using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Serialization
{
    /// <summary>
    /// Wraps messages serialization for interception
    /// </summary>
    public class RootSerializer: ASerializer
    {
        private readonly ISerializer _serializer;
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="RootSerializer"/> class.
        /// </summary>
        /// <param name="messageInterceptors">The message interceptors.</param>
        /// <param name="serializer">The serializer.</param>
        public RootSerializer(IMessageInterceptorRegistrar messageInterceptors, ISerializer serializer)
            : base(messageInterceptors)
        {
            Guard.NotNull(() => serializer, serializer);
            _serializer = serializer;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Converts a message to a byte array
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        protected override byte[] ConvertMessageToBytes<T>(T message)
        {
            Guard.NotNull(() => message, message);
            return _serializer.ConvertMessageToBytes(message);
        }

        /// <summary>
        /// Converts a byte array to a message
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bytes">The bytes.</param>
        /// <returns></returns>
        protected override T ConvertBytesToMessage<T>(byte[] bytes)
        {
            Guard.NotNull(() => bytes, bytes);
            return _serializer.ConvertBytesToMessage<T>(bytes);

        }
        #endregion
    }
}
