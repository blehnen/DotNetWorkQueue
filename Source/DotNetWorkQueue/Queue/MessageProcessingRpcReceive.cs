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
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Receives a response from an RPC request.
    /// </summary>
    /// <typeparam name="TReceivedMessage">The type of the received message.</typeparam>
    public class MessageProcessingRpcReceive<TReceivedMessage>: IMessageProcessingRpcReceive<TReceivedMessage>
         where TReceivedMessage : class
    {
        private readonly IMessageContextFactory _messageContextFactory;
        private readonly IReceiveMessagesFactory _receiveMessagesFactory;
        private readonly QueueConsumerConfiguration _configurationReceive;
        private readonly IRpcContextFactory _rpcContextFactory;
        private readonly ICommitMessage _commitMessage;
        private readonly IMessageHandlerRegistration _messageHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageProcessingRpcReceive{TReceivedMessage}" /> class.
        /// </summary>
        /// <param name="configurationReceive">The configuration receive.</param>
        /// <param name="receiveMessagesFactory">The receive messages factory.</param>
        /// <param name="messageContextFactory">The message context factory.</param>
        /// <param name="messageHandler">The message handler.</param>
        /// <param name="rpcContextFactory">The RPC context factory.</param>
        /// <param name="commitMessage">The commit message.</param>
        public MessageProcessingRpcReceive(
            QueueConsumerConfiguration configurationReceive,
            IReceiveMessagesFactory receiveMessagesFactory,
            IMessageContextFactory messageContextFactory,
            IMessageHandlerRegistration messageHandler,
            IRpcContextFactory rpcContextFactory,
            ICommitMessage commitMessage)
        {
            Guard.NotNull(() => configurationReceive, configurationReceive);
            Guard.NotNull(() => receiveMessagesFactory, receiveMessagesFactory);
            Guard.NotNull(() => messageContextFactory, messageContextFactory);
            Guard.NotNull(() => messageHandler, messageHandler);
            Guard.NotNull(() => rpcContextFactory, rpcContextFactory);
            Guard.NotNull(() => commitMessage, commitMessage);

            _configurationReceive = configurationReceive;
            _receiveMessagesFactory = receiveMessagesFactory;
            _messageContextFactory = messageContextFactory;
            _messageHandler = messageHandler;
            _rpcContextFactory = rpcContextFactory;
            _commitMessage = commitMessage;

            void Action(IReceivedMessage<TReceivedMessage> message, IWorkerNotification worker)
            {
            }

            messageHandler.Set((Action<IReceivedMessage<TReceivedMessage>, IWorkerNotification>) Action);
        }
        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="queueWait">The queue wait.</param>
        /// <returns></returns>
        /// <exception cref="System.TimeoutException"></exception>
        public IReceivedMessage<TReceivedMessage> Handle(IMessageId messageId, TimeSpan timeOut, IQueueWait queueWait)
        {
            Guard.NotNull(() => messageId, messageId);
            Guard.NotNull(() => queueWait, queueWait);

            //use a message context, and talk to the transport directly
            //we are not going to use the consumer queue, because we are going to re-use the calling thread for all of the work below
            using (var context = _messageContextFactory.Create())
            {
                var recMessage = _receiveMessagesFactory.Create();

                //set message Id on the context, so that the transport knows we want a particular message
                context.Set(_configurationReceive.HeaderNames.StandardHeaders.RpcContext, _rpcContextFactory.Create(messageId, timeOut));

                //use a stop watch to determine when we have timed out
                var sw = new Stopwatch();
                sw.Start();
                while (true)
                {
                    var messageRec = recMessage.ReceiveMessage(context);
                    if (messageRec != null)
                    {
                        _commitMessage.Commit(context);
                        return (IReceivedMessage<TReceivedMessage>)_messageHandler.GenerateMessage(messageRec);
                    }
                    if (sw.ElapsedMilliseconds >= timeOut.TotalMilliseconds) throw new TimeoutException();
                    queueWait.Wait();
                }
            }
        }
        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="queueWait">The queue wait.</param>
        /// <returns></returns>
        /// <exception cref="System.TimeoutException"></exception>
        public async Task<IReceivedMessage<TReceivedMessage>> HandleAsync(IMessageId messageId, TimeSpan timeOut, IQueueWait queueWait)
        {
            Guard.NotNull(() => messageId, messageId);
            Guard.NotNull(() => queueWait, queueWait);

            //use a message context, and talk to the transport directly
            //we are not going to use the consumer queue, because we are going to re-use the calling thread for all of the work below
            using (var context = _messageContextFactory.Create())
            {
                var recMessage = _receiveMessagesFactory.Create();

                //set message Id on the context, so that the transport knows we want a particular message
                context.Set(_configurationReceive.HeaderNames.StandardHeaders.RpcContext, _rpcContextFactory.Create(messageId, timeOut));

                //use a stop watch to determine when we have timed out
                var sw = new Stopwatch();
                sw.Start();
                while (true)
                {
                    var messageRec = await recMessage.ReceiveMessageAsync(context).ConfigureAwait(false);
                    if (messageRec != null)
                    {
                        _commitMessage.Commit(context);
                        return (IReceivedMessage<TReceivedMessage>)_messageHandler.GenerateMessage(messageRec);
                    }
                    if (sw.ElapsedMilliseconds >= timeOut.TotalMilliseconds) throw new TimeoutException();
                    queueWait.Wait();
                }
            }
        }
    }
}
