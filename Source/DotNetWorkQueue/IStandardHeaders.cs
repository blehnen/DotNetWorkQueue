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

using System;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Contains system standard header assessors
    /// </summary>
    public interface IStandardHeaders
    {
        /// <summary>
        /// The connection information for the receiving queue in an RPC queue.
        /// <remarks>The consuming code of the original message can use this information to send a reply</remarks>
        /// <example>
        /// IConnectionInformationSend connection = (IConnectionInformationSend)message.Headers[Headers.RPCConnectionInfoKey];
        /// </example>
        /// </summary>
        IMessageContextData<IConnectionInformation> RpcConnectionInfo { get; }
        /// <summary>
        /// The consuming code in an RPC queue can use this header to return an exception to the caller.
        /// <example>
        /// Send
        /// IAdditionalMessageData data = oConfig.GetAdditionalData();
        /// data.Headers.Set(Headers.RPCConsumerException, new DotNetWorkQueue.Exceptions.MessageException("Exception information here!", message.ID, message.CorrelationID));
        /// Receive
        /// //do we have an exception?
        /// if(message.Headers[Headers.RPCConsumerException] != null)
        /// {
        /// Exception error = (Exception)message.Headers[Headers.RPCConsumerException];
        /// //etc
        /// }
        /// </example>
        /// </summary>
        /// <value>
        /// The RPC consumer exception.
        /// </value>
        IMessageContextData<Exception> RpcConsumerException { get; }
        /// <summary>
        /// The timeout period will be attached to the headers when an RPC queue sends a message
        /// <remarks>The consuming code can re-use the timeout, or use it's own value. The timeout period when sending a response simply lets the clean up thread know when its safe to delete the record if it was not processed.</remarks><example>
        /// TimeSpan timeOut = TimeSpan.Parse((string)message.Headers[Headers.RPCTimeout], System.Globalization.CultureInfo.InvariantCulture);
        /// </example>
        /// </summary>
        /// <value>
        /// The RPC timeout.
        /// </value>
        IMessageContextData<IRpcTimeout> RpcTimeout { get; }
        /// <summary>
        /// Contains the ID of the RPC response
        /// </summary>
        IMessageContextData<string> RpcResponseId { get; }
        /// <summary>
        /// Gets the RPC context.
        /// </summary>
        /// <value>
        /// The RPC context.
        /// </value>
        IMessageContextData<IRpcContext> RpcContext { get; }

        /// <summary>
        /// Gets the first possible delivery date.
        /// </summary>
        /// <value>
        /// The first possible delivery date.
        /// </value>
        /// <remarks>Used to record the first possible date/time a message could be de-queued</remarks>
        IMessageContextData<ValueTypeWrapper<DateTime>> FirstPossibleDeliveryDate { get; }

        /// <summary>
        /// Gets the message interceptor graph.
        /// </summary>
        /// <value>
        /// The message interceptor graph.
        /// </value>
        IMessageContextData<MessageInterceptorsGraph> MessageInterceptorGraph { get; }
    }

    /// <summary>
    /// A wrapper for allowing a value type to be treated as a reference type
    /// </summary>
    /// <typeparam name="T">The value type (normally a strut) to wrap</typeparam>
    public class ValueTypeWrapper<T>
        where T : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTypeWrapper{T}"/> class.
        /// </summary>
        public ValueTypeWrapper()
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTypeWrapper{T}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public ValueTypeWrapper(T value)
        {
            Value = value;
        }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public T Value { get; set; }
    }
}
