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
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Sends a message to an RPC queue
    /// </summary>
    /// <typeparam name="TSendMessage">The type of the send message.</typeparam>
    public class MessageProcessingRpcSend<TSendMessage>: IMessageProcessingRpcSend<TSendMessage>
        where TSendMessage : class
    {
        private readonly IHeaders _headers;
        private readonly IProducerQueue<TSendMessage> _sendQueue;
        private readonly IRpcTimeoutFactory _rpcTimeoutFactory;
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageProcessingRpcSend{TSendMessage}" /> class.
        /// </summary>
        /// <param name="sendQueue">The send queue.</param>
        /// <param name="rpcTimeoutFactory">The RPC timeout factory.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public MessageProcessingRpcSend(
            IProducerQueue<TSendMessage> sendQueue,
            IRpcTimeoutFactory rpcTimeoutFactory,
            IHeaders headers,
            IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => sendQueue, sendQueue);
            Guard.NotNull(() => rpcTimeoutFactory, rpcTimeoutFactory);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => connectionInformation, connectionInformation);

            _sendQueue = sendQueue;
            _rpcTimeoutFactory = rpcTimeoutFactory;
            _headers = headers;
            _connectionInformation = connectionInformation;
        }

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeOut">The time out.</param>
        /// <returns></returns>
        public ISentMessage Handle(TSendMessage message, TimeSpan timeOut)
        {
            return Handle(message, new AdditionalMessageData(), timeOut);
        }

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeOut">The time out.</param>
        /// <returns></returns>
        public async Task<ISentMessage> HandleAsync(TSendMessage message, TimeSpan timeOut)
        {
            return await HandleAsync(message, new AdditionalMessageData(), timeOut).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The additional message data.</param>
        /// <param name="timeOut">The time out.</param>
        /// <returns></returns>
        public ISentMessage Handle(TSendMessage message, IAdditionalMessageData data, TimeSpan timeOut)
        {
            //store the destination queue as a header
            data.SetHeader(_headers.StandardHeaders.RpcConnectionInfo, _connectionInformation.Clone());

            //store the timeout as a header
            data.SetHeader(_headers.StandardHeaders.RpcTimeout, _rpcTimeoutFactory.Create(timeOut));

            //send the request
            return _sendQueue.Send(message, data).SentMessage;
        }

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The additional message data.</param>
        /// <param name="timeOut">The time out.</param>
        /// <returns></returns>
        public async Task<ISentMessage> HandleAsync(TSendMessage message, IAdditionalMessageData data, TimeSpan timeOut)
        {
            //store the destination queue as a header
            data.SetHeader(_headers.StandardHeaders.RpcConnectionInfo, _connectionInformation.Clone());

            //store the timeout as a header
            data.SetHeader(_headers.StandardHeaders.RpcTimeout, _rpcTimeoutFactory.Create(timeOut));

            //send the request
            var result = await _sendQueue.SendAsync(message, data).ConfigureAwait(false);
            return result.SentMessage;
        }
    }
}
