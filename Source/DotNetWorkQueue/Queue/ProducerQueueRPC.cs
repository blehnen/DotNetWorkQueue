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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Sends responses for an RPC request
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    public class ProducerQueueRpc<T> : IProducerQueueRpc<T>
        where T : class
    {
        private readonly QueueProducerConfiguration _configuration;
        private readonly ISendMessages _sendMessages;
        private readonly IResponseIdFactory _responseIdFactory;
        private readonly IMessageFactory _messageFactory;
        private readonly IRpcTimeoutFactory _rpcTimeoutFactory;
        private readonly GenerateMessageHeaders _generateMessageHeaders;
        private readonly AddStandardMessageHeaders _addStandardMessageHeaders;

        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProducerQueueRpc{T}" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sendMessages">The send messages.</param>
        /// <param name="responseIdFactory">The response identifier factory.</param>
        /// <param name="messageFactory">The message factory.</param>
        /// <param name="rpcTimeoutFactory">The RPC timeout factory.</param>
        /// <param name="generateMessageHeaders">The generate message headers.</param>
        /// <param name="addStandardMessageHeaders">The add standard message headers.</param>
        public ProducerQueueRpc(
            QueueProducerConfiguration configuration,
            ISendMessages sendMessages,
            IResponseIdFactory responseIdFactory,
            IMessageFactory messageFactory,
            IRpcTimeoutFactory rpcTimeoutFactory, 
            GenerateMessageHeaders generateMessageHeaders, 
            AddStandardMessageHeaders addStandardMessageHeaders)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => sendMessages, sendMessages);
            Guard.NotNull(() => responseIdFactory, responseIdFactory);
            Guard.NotNull(() => messageFactory, messageFactory);
            Guard.NotNull(() => rpcTimeoutFactory, rpcTimeoutFactory);
            Guard.NotNull(() => generateMessageHeaders, generateMessageHeaders);
            Guard.NotNull(() => addStandardMessageHeaders, addStandardMessageHeaders);


            _configuration = configuration;
            _sendMessages = sendMessages;
            _responseIdFactory = responseIdFactory;
            _messageFactory = messageFactory;
            _rpcTimeoutFactory = rpcTimeoutFactory;
            _generateMessageHeaders = generateMessageHeaders;
            _addStandardMessageHeaders = addStandardMessageHeaders;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public QueueProducerConfiguration Configuration
        {
            get
            {
                ThrowIfDisposed();
                return _configuration;
            }
        }

        /// <summary>
        /// Creates a response instance for sending a message reply
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="timeOut">The message time out.</param>
        /// <returns></returns>
        public IResponseId CreateResponse(IMessageId messageId, TimeSpan timeOut)
        {
            ThrowIfDisposed();
            return _responseIdFactory.Create(messageId, timeOut);
        }

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="responseId">The response identifier.</param>
        /// <param name="inputData"></param>
        /// <returns></returns>
        public IQueueOutputMessage Send(T message, IResponseId responseId, IAdditionalMessageData inputData = null)
        {
            ThrowIfDisposed();

            Guard.NotNull(() => message, message);
            Guard.NotNull(() => responseId, responseId);

            var data = SetupForSend(message, responseId, inputData);

            //send the message to the transport
            return _sendMessages.Send(data.Item1, data.Item2);
        }

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="responseId">The response identifier.</param>
        /// <returns></returns>
        public async Task<IQueueOutputMessage> SendAsync(T message, IResponseId responseId)
        {
            ThrowIfDisposed();
            var data = new AdditionalMessageData();
            return await SendAsync(message, responseId, data).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="responseId">The response identifier.</param>
        /// <param name="inputData">The additional message data.</param>
        /// <returns></returns>
        public async Task<IQueueOutputMessage> SendAsync(T message, IResponseId responseId, IAdditionalMessageData inputData)
        {
            ThrowIfDisposed();

            Guard.NotNull(() => message, message);
            Guard.NotNull(() => responseId, responseId);

            var data = SetupForSend(message, responseId, inputData);

            //send the message to the transport
            return await _sendMessages.SendAsync(data.Item1, data.Item2).ConfigureAwait(false);
        }

        private Tuple<IMessage, IAdditionalMessageData> SetupForSend(T message, IResponseId responseId, IAdditionalMessageData inputData)
        {
            var data = inputData ?? new AdditionalMessageData();

            if (!Configuration.IsReadOnly)
                Configuration.SetReadOnly();

            var additionalHeaders = _generateMessageHeaders.HeaderSetup(data);

            //construct our wrapper around the data to send
            var messageToSend = _messageFactory.Create(message, additionalHeaders);
            _addStandardMessageHeaders.AddHeaders(messageToSend, data);

            SetIpcInternalHeaders(messageToSend, responseId);

            return new Tuple<IMessage, IAdditionalMessageData>(messageToSend, data);
        }
        /// <summary>
        /// Sets the ipc internal headers.
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="responseId">The response identifier.</param>
        private void SetIpcInternalHeaders(IMessage messageToSend, IResponseId responseId)
        {
            messageToSend.SetInternalHeader(Configuration.HeaderNames.StandardHeaders.RpcTimeout,
             _rpcTimeoutFactory.Create(responseId.TimeOut));
            messageToSend.SetInternalHeader(Configuration.HeaderNames.StandardHeaders.RpcResponseId,
                responseId.MessageId.Id.Value.ToString());
        }

        #region IDispose, IIsDisposed
        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected void ThrowIfDisposed([CallerMemberName] string name = "")
        {
            if (Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0)
            {
                throw new ObjectDisposedException(name);
            }
        }
        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (Interlocked.Increment(ref _disposeCount) == 1)
            {
 
            }
        }

        #endregion
    }
}
