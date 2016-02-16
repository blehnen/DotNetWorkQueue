// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Sends a new message or group of messages
    /// </summary>
    internal class RedisQueueSend : ISendMessages
    {
        #region Member Level Variables
        private readonly ISentMessageFactory _sentMessageFactory;
        private readonly ICommandHandlerWithOutput<SendMessageCommand, string> _sendMessage;
        private readonly ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages> _sendMessageBatch;
        private readonly ICommandHandlerWithOutput<SendMessageCommand, Task<string>> _sendMessageAsync;

        private readonly ICommandHandlerWithOutput<SendMessageCommandBatch, Task<QueueOutputMessages>>
            _sendMessageBatchAsync;

        private readonly RedisHeaders _headers;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueSend" /> class.
        /// </summary>
        /// <param name="sentMessageFactory">The sent message factory.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="sendMessage">The send message.</param>
        /// <param name="sendMessageBatch">The send message batch.</param>
        /// <param name="sendMessageAsync">The send message asynchronous.</param>
        /// <param name="sendMessageBatchAsync">The send message batch asynchronous.</param>
        public RedisQueueSend(ISentMessageFactory sentMessageFactory,
            RedisHeaders headers, ICommandHandlerWithOutput<SendMessageCommand, string> sendMessage, ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages> sendMessageBatch, ICommandHandlerWithOutput<SendMessageCommand, Task<string>> sendMessageAsync, ICommandHandlerWithOutput<SendMessageCommandBatch, Task<QueueOutputMessages>> sendMessageBatchAsync)
        {
            Guard.NotNull(() => sentMessageFactory, sentMessageFactory);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => sendMessage, sendMessage);
            Guard.NotNull(() => sendMessageBatch, sendMessageBatch);
            Guard.NotNull(() => sendMessageBatchAsync, sendMessageBatchAsync);
            Guard.NotNull(() => sentMessageFactory, sentMessageFactory);

            _headers = headers;
            _sendMessage = sendMessage;
            _sendMessageBatch = sendMessageBatch;
            _sendMessageAsync = sendMessageAsync;
            _sendMessageBatchAsync = sendMessageBatchAsync;
            _sentMessageFactory = sentMessageFactory;
        }
        #endregion

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">An error occurred while sending a message</exception>
        public IQueueOutputMessage Send(IMessage messageToSend, IAdditionalMessageData data)
        {
            try
            {
                //correlationID must be stored as a message header
                messageToSend.SetHeader(_headers.CorelationId, new RedisQueueCorrelationIdSerialized((Guid)data.CorrelationId.Id.Value));

                var messageId = _sendMessage.Handle(new SendMessageCommand(messageToSend, data));
                return new QueueOutputMessage(_sentMessageFactory.Create(new RedisQueueId(messageId), data.CorrelationId));
            }
            catch (Exception exception)
            {
                return new QueueOutputMessage(_sentMessageFactory.Create(null, data.CorrelationId), exception);
            }
        }

        /// <summary>
        /// Sends the specified messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        public IQueueOutputMessages Send(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            try
            {       
                return _sendMessageBatch.Handle(new SendMessageCommandBatch(messages));
            }
            catch (Exception exception)
            {
                throw new DotNetWorkQueueException("An error occurred while sending a message", exception);
            }
        }

        /// <summary>
        /// Sends the message async.
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public async Task<IQueueOutputMessage> SendAsync(IMessage messageToSend, IAdditionalMessageData data)
        {
            try
            {
                //correlationID must be stored as a message header
                messageToSend.SetHeader(_headers.CorelationId, new RedisQueueCorrelationIdSerialized((Guid)data.CorrelationId.Id.Value));
                var messageId = await _sendMessageAsync.Handle(new SendMessageCommand(messageToSend, data)).ConfigureAwait(false);
                return new QueueOutputMessage(_sentMessageFactory.Create(new RedisQueueId(messageId), data.CorrelationId));
            }
            catch (Exception exception)
            {
                return new QueueOutputMessage(_sentMessageFactory.Create(null, data.CorrelationId), exception);
            }
        }

        /// <summary>
        /// Sends the messages async
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">An error occurred while sending a message</exception>
        public async Task<IQueueOutputMessages> SendAsync(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            try
            {
                return await _sendMessageBatchAsync.Handle(new SendMessageCommandBatch(messages)).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw new DotNetWorkQueueException("An error occurred while sending a message", exception);
            }
        }
    }
}
