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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Sends a new message to an existing queue
    /// </summary>
    internal class SqLiteMessageQueueSend : ISendMessages
    {
        #region Member Level Variables

        private readonly ISentMessageFactory _sentMessageFactory;
        private readonly ICommandHandlerWithOutput<SendMessageCommand, long> _sendMessage;
        private readonly ICommandHandlerWithOutputAsync<SendMessageCommand, long> _sendMessageAsync;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteMessageQueueSend" /> class.
        /// </summary>
        /// <param name="sentMessageFactory">The sent message factory.</param>
        /// <param name="sendMessage">The send message.</param>
        /// <param name="sendMessageAsync">The send message asynchronous.</param>
        public SqLiteMessageQueueSend(ISentMessageFactory sentMessageFactory,
            ICommandHandlerWithOutput<SendMessageCommand, long> sendMessage, 
            ICommandHandlerWithOutputAsync<SendMessageCommand, long> sendMessageAsync)
        {
            Guard.NotNull(() => sentMessageFactory, sentMessageFactory);
            Guard.NotNull(() => sendMessage, sendMessage);
            Guard.NotNull(() => sendMessageAsync, sendMessageAsync);
            _sentMessageFactory = sentMessageFactory;
            _sendMessage = sendMessage;
            _sendMessageAsync = sendMessageAsync;
        }
        #endregion

        #region ISendMessages
        /// <summary>
        /// Sends a new message to an existing queue
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="data">The additional data.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException"></exception>
        /// <exception cref="System.Exception">Failed to insert record</exception>
        /// <exception cref="System.ApplicationException"></exception>
        public IQueueOutputMessage Send(IMessage messageToSend, IAdditionalMessageData data)
        {
            try
            {
                var id = _sendMessage.Handle(
                    new SendMessageCommand(messageToSend, data));
                return new QueueOutputMessage(_sentMessageFactory.Create(new SqLiteMessageQueueId(id), data.CorrelationId));
            }
            catch (Exception exception)
            {
                return new QueueOutputMessage(_sentMessageFactory.Create(null, data.CorrelationId), exception);
            }
        }
        #endregion

        /// <summary>
        /// Sends a new message to an existing queue
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        public IQueueOutputMessages Send(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            try
            {
                var rc = new ConcurrentBag<IQueueOutputMessage>();
                Parallel.ForEach(messages, m =>
                {
                    try
                    {
                        var id = _sendMessage.Handle(
                            new SendMessageCommand(m.Message, m.MessageData));
                        rc.Add(
                            new QueueOutputMessage(_sentMessageFactory.Create(new SqLiteMessageQueueId(id),
                                m.MessageData.CorrelationId)));
                    }
                    catch (Exception error)
                    {
                        rc.Add(
                            new QueueOutputMessage(_sentMessageFactory.Create(null,
                                m.MessageData.CorrelationId), error));
                    }
                });
                return new QueueOutputMessages(rc.ToList());
            }
            catch (Exception exception)
            {
                throw new DotNetWorkQueueException("An error occurred while sending a message", exception);
            }
        }

        /// <summary>
        /// Sends a new message to an existing queue
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">An error occurred while sending a message</exception>
        public async Task<IQueueOutputMessage> SendAsync(IMessage messageToSend, IAdditionalMessageData data)
        {
            try
            {
                var id = await _sendMessageAsync.Handle(
                    new SendMessageCommand(messageToSend, data)).ConfigureAwait(false);
                return new QueueOutputMessage(_sentMessageFactory.Create(new SqLiteMessageQueueId(id), data.CorrelationId));
            }
            catch (Exception exception)
            {
                return new QueueOutputMessage(_sentMessageFactory.Create(null, data.CorrelationId), exception);
            }
        }

        /// <summary>
        /// Sends new messages to an existing queue
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">An error occurred while sending a message</exception>
        public async Task<IQueueOutputMessages> SendAsync(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            try
            {
                var rc = new ConcurrentBag<IQueueOutputMessage>();
                foreach (var m in messages)
                {
                    try
                    {
                        var id = await _sendMessageAsync.Handle(
                            new SendMessageCommand(m.Message, m.MessageData)).ConfigureAwait(false);
                        rc.Add(new QueueOutputMessage(_sentMessageFactory.Create(new SqLiteMessageQueueId(id), m.MessageData.CorrelationId)));
                    }
                    catch (Exception error)
                    {
                        rc.Add(new QueueOutputMessage(_sentMessageFactory.Create(null, m.MessageData.CorrelationId), error));
                    }
                }
                return new QueueOutputMessages(rc.ToList());
            }
            catch (Exception exception)
            {
                throw new DotNetWorkQueueException("An error occurred while sending a message", exception);
            }
        }
    }
}
