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
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// A queue that can be used to send a message and receive a response
    /// </summary>
    /// <typeparam name="TReceivedMessage">The type of the response message</typeparam>
    /// <typeparam name="TSendMessage">The type of the sent message</typeparam>
    public class RpcQueue<TReceivedMessage, TSendMessage> : BaseQueue, IRpcQueue<TReceivedMessage, TSendMessage>
        where TReceivedMessage : class
        where TSendMessage: class
    {
        private readonly QueueConsumerConfiguration _configurationReceive;
        private readonly QueueRpcConfiguration _configurationRpc;
        private readonly IMessageProcessingRpcReceive<TReceivedMessage> _messageProcessingRpcReceive;
        private readonly IMessageProcessingRpcSend<TSendMessage> _messageProcessingRpcSend;
        private readonly IQueueWaitFactory _queueWaitFactory;

        private readonly IClearExpiredMessagesRpcMonitor _clearQueue;
        private IQueueWait _queueWait;

        private int _disposeCount;

        private long _waitingOnAsyncTasks;

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcQueue{TReceivedMessage, TSendMessage}" /> class.
        /// </summary>
        /// <param name="configurationRpc">The configuration RPC.</param>
        /// <param name="configurationReceive">The configuration receive.</param>
        /// <param name="clearMessages">The clear messages factory.</param>
        /// <param name="log">The log.</param>
        /// <param name="messageProcessingRpcReceive">The message processing RPC receive.</param>
        /// <param name="messageProcessingRpcSend">The message processing RPC send.</param>
        /// <param name="queueWaitFactory">The queue wait factory.</param>
        public RpcQueue(
            QueueRpcConfiguration configurationRpc,
            QueueConsumerConfiguration configurationReceive,
            IClearExpiredMessagesRpcMonitor clearMessages,
            ILogFactory log,
            IMessageProcessingRpcReceive<TReceivedMessage> messageProcessingRpcReceive,
            IMessageProcessingRpcSend<TSendMessage> messageProcessingRpcSend,
            IQueueWaitFactory queueWaitFactory)
            : base(log)
        {
            Guard.NotNull(() => configurationRpc, configurationRpc);
            Guard.NotNull(() => configurationReceive, configurationReceive);
            Guard.NotNull(() => clearMessages, clearMessages);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => messageProcessingRpcReceive, messageProcessingRpcReceive);
            Guard.NotNull(() => messageProcessingRpcSend, messageProcessingRpcSend);
            Guard.NotNull(() => queueWaitFactory, queueWaitFactory);

            _configurationReceive = configurationReceive;
            _configurationRpc = configurationRpc;
            _messageProcessingRpcReceive = messageProcessingRpcReceive;
            _messageProcessingRpcSend = messageProcessingRpcSend;
            _queueWaitFactory = queueWaitFactory;

            _clearQueue = clearMessages;
        }

        /// <summary>
        /// The queue configuration
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public QueueRpcConfiguration Configuration {
            get { ThrowIfDisposed(); return _configurationRpc; }
        }

        /// <summary>
        /// Starts the queue.
        /// </summary>
        /// <remarks>
        /// This must be called after setting any configuration options, and before sending any messages.
        /// </remarks>
        public void Start()
        {
            ThrowIfDisposed();
            if (Started)
            {
                throw new DotNetWorkQueueException("Start must only be called 1 time");
            }

            _clearQueue.Start();
            _configurationRpc.SetReadOnly();
            _configurationReceive.SetReadOnly();
            _queueWait = _queueWaitFactory.CreateQueueDelay();
            Started = true;
        }
        /// <summary>
        /// Sends a message, and awaits a response
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Start() must be called before sending messages</exception>
        /// <exception cref="System.TimeoutException"></exception>
        public IReceivedMessage<TReceivedMessage> Send(TSendMessage message, TimeSpan timeout, IAdditionalMessageData data = null)
        {
            ThrowIfDisposed();
            CheckStarted();

            Guard.NotNull(() => message, message);

            //send the request
            var sm = data == null ? _messageProcessingRpcSend.Handle(message, timeout) : _messageProcessingRpcSend.Handle(message, data, timeout);
            if (sm.MessageId != null && sm.MessageId.HasValue)
            {
                //look for the response
                return _messageProcessingRpcReceive.Handle(sm.MessageId, timeout, _queueWait);
            }
            throw new DotNetWorkQueueException(
                "A null messageID was returned; this generally indicates a setup problem. Verify your queue types");
        }
        /// <summary>
        /// Sends a message, and awaits a response
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Start() must be called before sending messages</exception>
        /// <exception cref="System.TimeoutException"></exception>
        public async Task<IReceivedMessage<TReceivedMessage>> SendAsync(TSendMessage message, TimeSpan timeout, IAdditionalMessageData data = null)
        {
            ThrowIfDisposed();
            CheckStarted();

            Guard.NotNull(() => message, message);

            Interlocked.Increment(ref _waitingOnAsyncTasks);
            try
            {
                //send the request
                var sm = data == null ? await _messageProcessingRpcSend.HandleAsync(message, timeout).ConfigureAwait(false) : await _messageProcessingRpcSend.HandleAsync(message, data, timeout).ConfigureAwait(false);

                //look for the response
                return await _messageProcessingRpcReceive.HandleAsync(sm.MessageId, timeout, _queueWait).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _waitingOnAsyncTasks);
            }
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            if (Interlocked.Read(ref _waitingOnAsyncTasks) > 0)
            {
                WaitOnAsyncTask.Wait(() => Interlocked.Read(ref _waitingOnAsyncTasks) > 0,
                    () => Log.Warn(
                        $"Unable to terminate because async requests have not finished. Current task count is {Interlocked.Read(ref _waitingOnAsyncTasks)}"));
            }

            _clearQueue.Stop();
            base.Dispose(true);
        }

        /// <summary>
        /// Throws an exception if <see cref="Start"/> has not been called.
        /// </summary>
        /// <exception cref="DotNetWorkQueueException">Start() must be called before sending messages</exception>
        private void CheckStarted()
        {
            if (!Started)
            {
                throw new DotNetWorkQueueException("Start() must be called before sending messages");
            }
        }
    }
}
