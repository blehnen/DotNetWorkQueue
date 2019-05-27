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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Contract for message serialization interception
    /// </summary>
    public abstract class ASerializer
    {
        /// <summary>
        /// The interceptors
        /// </summary>
        protected readonly IMessageInterceptorRegistrar MessageInterceptors;
        /// <summary>
        /// Initializes a new instance of the <see cref="ASerializer" /> class.
        /// </summary>
        /// <param name="messageInterceptors">The interceptors.</param>
        protected ASerializer(IMessageInterceptorRegistrar messageInterceptors)
        {
            MessageInterceptors = messageInterceptors;
        }
        /// <summary>Converts a message into a byte array</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message.</param>
        /// <param name="headers">message headers</param>
        /// <returns></returns>
        public virtual MessageInterceptorsResult MessageToBytes<T>(T message, IDictionary<string, object> headers) where T : class
        {
            Guard.NotNull(() => message, message);
            byte[] btBytes;
            try
            {
                btBytes = ConvertMessageToBytes(message, new ReadOnlyDictionary<string, object>(headers));
            }
            catch (Exception error)
            {
                throw new SerializationException("An error has occurred when converting a message into byte array", error);
            }
            if (MessageInterceptors == null) return new MessageInterceptorsResult {Output = btBytes};
            try
            {
                return MessageInterceptors.MessageToBytes(btBytes, new ReadOnlyDictionary<string, object>(headers));
            }
            catch (Exception error)
            {
                throw new InterceptorException("An error has occurred while intercepting message serialization", error);
            }
        }
        /// <summary>
        /// Converts a byte array to a message
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bytes">The bytes.</param>
        /// <param name="graph">The message interception graph.</param>
        /// <param name="headers">The message headers</param>
        /// <returns></returns>
        /// <exception cref="SerializationException">An error has occurred when de-serializing a message</exception>
        public virtual T BytesToMessage<T>(byte[] bytes, MessageInterceptorsGraph graph, IDictionary<string, object> headers) where T : class
        {
            Guard.NotNull(() => bytes, bytes);

            if (MessageInterceptors != null)
            {
                return BytesToMessageWithInterceptors<T>(bytes, graph, headers);
            }

            try
            {
                return ConvertBytesToMessage<T>(bytes, new ReadOnlyDictionary<string, object>(headers));
            }
            catch (Exception error)
            {
                throw new SerializationException("An error has occurred when de-serializing a message", error);
            }
        }

        /// <summary>
        /// Converts a byte array to a message and applies any interceptors that are 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes">The bytes.</param>
        /// <param name="graph">The graph.</param>
        /// <returns></returns>
        /// <exception cref="InterceptorException">An error has occurred while intercepting message de-serialization</exception>
        /// <exception cref="SerializationException">An error has occurred when de-serializing a message</exception>
        private T BytesToMessageWithInterceptors<T>(byte[] bytes, MessageInterceptorsGraph graph, IDictionary<string, object> headers) where T : class
        {
            byte[] btBytes;

            try
            {
                btBytes = MessageInterceptors.BytesToMessage(bytes, graph, new ReadOnlyDictionary<string, object>(headers));
            }
            catch (Exception error)
            {
                throw new InterceptorException("An error has occurred while intercepting message de-serialization", error);
            }

            try
            {
                return ConvertBytesToMessage<T>(btBytes, new ReadOnlyDictionary<string, object>(headers));
            }
            catch (Exception error)
            {
                throw new SerializationException("An error has occurred when de-serializing a message", error);
            }
        }

        /// <summary>
        /// Converts the message to a byte array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        protected abstract byte[] ConvertMessageToBytes<T>(T message, IReadOnlyDictionary<string, object> headers) where T : class;
        /// <summary>
        /// Converts a byte array back into a message
        /// </summary>
        /// <typeparam name="T">the type of the message</typeparam>
        /// <param name="bytes">The bytes.</param>
        /// <returns></returns>
        protected abstract T ConvertBytesToMessage<T>(byte[] bytes, IReadOnlyDictionary<string, object> headers) where T : class;
    }
}
