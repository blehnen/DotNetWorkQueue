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
using DotNetWorkQueue.Trace;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Contains system standard headers 
    /// </summary>
    public class StandardHeaders : IStandardHeaders
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StandardHeaders"/> class.
        /// </summary>
        /// <param name="messageContextDataFactory">The message context data factory.</param>
        /// <param name="timeoutFactory">The timeout factory.</param>
        public StandardHeaders(IMessageContextDataFactory messageContextDataFactory, IRpcTimeoutFactory timeoutFactory)
        {
            Guard.NotNull(() => messageContextDataFactory, messageContextDataFactory);
            Guard.NotNull(() => timeoutFactory, timeoutFactory);

            RpcTimeout = messageContextDataFactory.Create("Queue-RPCTimeout", timeoutFactory.Create(TimeSpan.Zero));
            RpcResponseId = messageContextDataFactory.Create<string>("Queue-RPCResponseID", null);
            RpcConsumerException = messageContextDataFactory.Create<Exception>("Queue-RPCConsumerException", null);
            RpcConnectionInfo = messageContextDataFactory.Create<IConnectionInformation>("Queue-RPCConnectionInfo", null);
            RpcContext = messageContextDataFactory.Create<IRpcContext>("Queue-RPCContext", null);
            FirstPossibleDeliveryDate = messageContextDataFactory.Create<ValueTypeWrapper<DateTime>>("Queue-FirstPossibleDeliveryDate", null);
            MessageInterceptorGraph =
                messageContextDataFactory.Create("Queue-MessageInterceptorGraph",
                    new MessageInterceptorsGraph());
            TraceSpan = messageContextDataFactory.Create<DataMappingTextMap>("Queue-TraceSpan", null);
        }

        /// <summary>
        /// The connection information for the receiving queue in an RPC queue.
        /// <remarks>The consuming code of the original message can use this information to send a reply</remarks>
        /// <example>
        /// IConnectionInformationSend connection = (IConnectionInformationSend)message.Headers[Headers.RPCConnectionInfoKey];
        /// </example>
        /// </summary>
        public IMessageContextData<IConnectionInformation> RpcConnectionInfo { get; }
        /// <summary>
        /// The consuming code in an RPC queue can use this header to return an exception to the caller.
        /// <example>
        ///  Send
        ///  IAdditionalMessageData data = oConfig.GetAdditionalData();
        ///  data.Headers.Set(Headers.RPCConsumerException, new DotNetWorkQueue.Exceptions.MessageException("Exception information here!", message.ID, message.CorrelationID));
        ///  
        ///  Receive
        ///  //do we have an exception?
        ///  if(message.Headers[Headers.RPCConsumerException] != null)
        ///  {
        ///     Exception error = (Exception)message.Headers[Headers.RPCConsumerException];
        ///     //etc
        ///  }
        /// </example>
        /// </summary>
        public IMessageContextData<Exception> RpcConsumerException { get; }
        /// <summary>
        /// The timeout period will be attached to the headers when an RPC queue sends a message
        /// <remarks>The consuming code can re-use the timeout, or use it's own value. The timeout period when sending a response simply lets the clean up thread know when its safe to delete the record if it was not processed.</remarks>
        /// <example>
        /// TimeSpan timeOut = TimeSpan.Parse((string)message.Headers[Headers.RPCTimeout], System.Globalization.CultureInfo.InvariantCulture);
        /// </example>
        /// </summary>
        public IMessageContextData<IRpcTimeout> RpcTimeout  { get; }
        /// <summary>
        /// Contains the ID of the RPC response
        /// </summary>
        public IMessageContextData<string> RpcResponseId { get; }
        /// <summary>
        /// Gets the RPC context.
        /// </summary>
        /// <value>
        /// The RPC context.
        /// </value>
        public IMessageContextData<IRpcContext> RpcContext { get; }

        /// <summary>
        /// Gets the first possible delivery date.
        /// </summary>
        /// <value>
        /// The first possible delivery date.
        /// </value>
        /// <remarks>
        /// Used to record the first possible date/time a message could be de-queued
        /// </remarks>
        public IMessageContextData<ValueTypeWrapper<DateTime>> FirstPossibleDeliveryDate { get; }

        /// <summary>
        /// Gets the message interceptor graph.
        /// </summary>
        /// <value>
        /// The message interceptor graph.
        /// </value>
        public IMessageContextData<MessageInterceptorsGraph> MessageInterceptorGraph { get; }

        /// <inheritdoc/>
        public IMessageContextData<DataMappingTextMap> TraceSpan { get; }
    }
}
