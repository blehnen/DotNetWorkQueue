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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Sends messages to a transport
    /// </summary>
    /// <typeparam name="T">The type of the message</typeparam>
    public class ProducerQueue<T> : IProducerQueue<T>
        where T: class
    {
        private readonly QueueProducerConfiguration _configuration;
        private readonly ISendMessages _sendMessages;
        private readonly IMessageFactory _messageFactory;
        private readonly GenerateMessageHeaders _generateMessageHeaders;
        private readonly AddStandardMessageHeaders _addStandardMessageHeaders;
        private readonly ILogger _log;
        private int _disposeCount;
        private long _asyncTaskCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProducerQueue{T}" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sendMessages">The send messages.</param>
        /// <param name="messageFactory">The message factory.</param>
        /// <param name="log">The log.</param>
        /// <param name="generateMessageHeaders">The generate message headers.</param>
        /// <param name="addStandardMessageHeaders">The add standard message headers.</param>
        public ProducerQueue(
            QueueProducerConfiguration configuration,
            ISendMessages sendMessages,
            IMessageFactory messageFactory,
            ILogger log, 
            GenerateMessageHeaders generateMessageHeaders,
            AddStandardMessageHeaders addStandardMessageHeaders)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => sendMessages, sendMessages);
            Guard.NotNull(() => messageFactory, messageFactory);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => generateMessageHeaders, generateMessageHeaders);
            Guard.NotNull(() => addStandardMessageHeaders, addStandardMessageHeaders);

            _configuration = configuration;
            _sendMessages = sendMessages;
            _addStandardMessageHeaders = addStandardMessageHeaders;

            _messageFactory = messageFactory;
            _log = log;
            _generateMessageHeaders = generateMessageHeaders;
        }

        /// <summary>
        /// The configuration settings for the queue.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public QueueProducerConfiguration Configuration { get { ThrowIfDisposed(); return _configuration; } }

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The additional message data.</param>
        /// <returns></returns>
        public IQueueOutputMessage Send(T message, IAdditionalMessageData data = null)
        {
            ThrowIfDisposed();
            if (data != null)
            {
                return InternalSend(message, data);
            }
            return InternalSend(message, new AdditionalMessageData());
        }

        /// <summary>
        /// Sends the specified messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        public IQueueOutputMessages Send(List<T> messages)
        {
            ThrowIfDisposed();
            var data = new List<QueueMessage<T, IAdditionalMessageData>>(messages.Count);
            data.AddRange(messages.Select(t => new QueueMessage<T, IAdditionalMessageData>(t, new AdditionalMessageData())));
            return InternalSend(data);
        }

        /// <summary>
        /// Sends the specified messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        public IQueueOutputMessages Send(List<QueueMessage<T, IAdditionalMessageData>> messages)
        {
            ThrowIfDisposed();
            return InternalSend(messages);
        }

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The additional message data.</param>
        /// <returns></returns>
        public async Task<IQueueOutputMessage> SendAsync(T message, IAdditionalMessageData data = null)
        {
            ThrowIfDisposed();
            Interlocked.Increment(ref _asyncTaskCount);
            try
            {
                if (data != null)
                {
                    return await InternalSendAsync(message, data).ConfigureAwait(false);
                }
                return await InternalSendAsync(message, new AdditionalMessageData()).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _asyncTaskCount);
            }
        }

        /// <summary>
        /// Sends the specified messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        public async Task<IQueueOutputMessages> SendAsync(List<T> messages)
        {
            ThrowIfDisposed();
            var data = new List<QueueMessage<T, IAdditionalMessageData>>(messages.Count);
            data.AddRange(messages.Select(t => new QueueMessage<T, IAdditionalMessageData>(t, new AdditionalMessageData())));
            Interlocked.Increment(ref _asyncTaskCount);
            try
            {
                return await InternalSendAsync(data).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _asyncTaskCount);
            }
        }

        /// <summary>
        /// Sends the specified messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        public async Task<IQueueOutputMessages> SendAsync(List<QueueMessage<T, IAdditionalMessageData>> messages)
        {
            ThrowIfDisposed();
            Interlocked.Increment(ref _asyncTaskCount);
            try
            {
                return await InternalSendAsync(messages).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _asyncTaskCount);
            }
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
            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            if (_asyncTaskCount > 0)
            {
                WaitOnAsyncTask.Wait(() => _asyncTaskCount > 0,
                    () => _log.LogWarning(
                        $"Unable to terminate because async requests have not finished. Current task count is {_asyncTaskCount}"));
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        #endregion

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        private IQueueOutputMessage InternalSend(T message, IAdditionalMessageData data)
        {
            Guard.NotNull(() => message, message);
            Guard.NotNull(() => data, data);

            if(!Configuration.IsReadOnly)
                Configuration.SetReadOnly();

            var additionalHeaders = _generateMessageHeaders.HeaderSetup(data);
            var messageToSend = _messageFactory.Create(message, additionalHeaders);
            _addStandardMessageHeaders.AddHeaders(messageToSend, data);

            //send the message to the transport
            return _sendMessages.Send(messageToSend, data);
        }

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        private async Task<IQueueOutputMessage> InternalSendAsync(T message, IAdditionalMessageData data)
        {
            Guard.NotNull(() => message, message);
            Guard.NotNull(() => data, data);

            if (!Configuration.IsReadOnly)
                Configuration.SetReadOnly();

            var additionalHeaders = _generateMessageHeaders.HeaderSetup(data);
            var messageToSend = _messageFactory.Create(message, additionalHeaders);
            _addStandardMessageHeaders.AddHeaders(messageToSend, data);

            //send the message to the transport
            return await _sendMessages.SendAsync(messageToSend, data).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        private IQueueOutputMessages InternalSend(List<QueueMessage<T, IAdditionalMessageData>> messages)
        {
            Guard.NotNull(() => messages, messages);

            var newMessages = InternalSendPrepare(messages);

            //send the message to the transport
            return _sendMessages.Send(newMessages.ToList());
        }

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        private async Task<IQueueOutputMessages> InternalSendAsync(List<QueueMessage<T, IAdditionalMessageData>> messages)
        {
            Guard.NotNull(() => messages, messages);

            var newMessages = InternalSendPrepare(messages);
          
            //send the message to the transport
            return await _sendMessages.SendAsync(newMessages.ToList()).ConfigureAwait(false);
        }

        /// <summary>
        /// Turns the user messages into the internal message format for the queue
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        private ConcurrentBag<QueueMessage<IMessage, IAdditionalMessageData>> InternalSendPrepare(List<QueueMessage<T, IAdditionalMessageData>> messages)
        {
            if (!Configuration.IsReadOnly)
                Configuration.SetReadOnly();

            var newMessages = new ConcurrentBag<QueueMessage<IMessage, IAdditionalMessageData>>();
            Parallel.ForEach(messages, t =>
            {
                var data = t.MessageData ?? new AdditionalMessageData();
                var additionalHeaders = _generateMessageHeaders.HeaderSetup(data);
                var newMessage = _messageFactory.Create(t.Message, additionalHeaders);
                _addStandardMessageHeaders.AddHeaders(newMessage, data);
                newMessages.Add(new QueueMessage<IMessage, IAdditionalMessageData>(newMessage, data));
            });
            return newMessages;
        }
    }
}
