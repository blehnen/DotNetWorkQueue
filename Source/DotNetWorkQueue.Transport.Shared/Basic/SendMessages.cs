// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Shared.Basic
{
    /// <inheritdoc />
    public class SendMessages<T> : ISendMessages
        where T : struct, IComparable<T>
    {
        #region Member Level Variables

        private readonly ISentMessageFactory _sentMessageFactory;
        private readonly ICommandHandlerWithOutput<SendMessageCommand, T> _sendMessage;
        private readonly ICommandHandlerWithOutputAsync<SendMessageCommand, T> _sendMessageAsync;
        private readonly ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages> _sendMessageBatch;
        private readonly ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages> _sendMessageBatchAsync;
        private readonly bool _batchSupported;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessages{T}" /> class.
        /// </summary>
        /// <param name="sentMessageFactory">The sent message factory.</param>
        /// <param name="sendMessage">The send message.</param>
        /// <param name="sendMessageAsync">The send message asynchronous.</param>
        /// <param name="sendMessageBatch">The batch send handler. Transports that provide a true
        /// bulk insert register a real handler here; transports that do not register a no-op
        /// placeholder that is never invoked.</param>
        /// <param name="sendMessageBatchAsync">The asynchronous batch send handler.</param>
        /// <param name="batchSupport">Capability flag indicating whether the transport provides a
        /// real batch handler. A dedicated, non-decorated service is used rather than a type check
        /// on <paramref name="sendMessageBatch"/>, because the injected handler is wrapped by
        /// command-handler decorators that would hide a marker on the underlying no-op.</param>
        public SendMessages(ISentMessageFactory sentMessageFactory,
            ICommandHandlerWithOutput<SendMessageCommand, T> sendMessage,
            ICommandHandlerWithOutputAsync<SendMessageCommand, T> sendMessageAsync,
            ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages> sendMessageBatch,
            ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages> sendMessageBatchAsync,
            ISendMessageBatchSupport batchSupport)
        {
            Guard.NotNull(() => sentMessageFactory, sentMessageFactory);
            Guard.NotNull(() => sendMessage, sendMessage);
            Guard.NotNull(() => sendMessageAsync, sendMessageAsync);
            Guard.NotNull(() => sendMessageBatch, sendMessageBatch);
            Guard.NotNull(() => sendMessageBatchAsync, sendMessageBatchAsync);
            Guard.NotNull(() => batchSupport, batchSupport);
            _sentMessageFactory = sentMessageFactory;
            _sendMessage = sendMessage;
            _sendMessageAsync = sendMessageAsync;
            _sendMessageBatch = sendMessageBatch;
            _sendMessageBatchAsync = sendMessageBatchAsync;
            _batchSupported = batchSupport.IsSupported;
        }
        #endregion

        #region ISendMessages
        /// <inheritdoc />
        public IQueueOutputMessage Send(IMessage messageToSend, IAdditionalMessageData data)
        {
            try
            {
                var id = _sendMessage.Handle(
                    new SendMessageCommand(messageToSend, data));
                return new QueueOutputMessage(_sentMessageFactory.Create(new MessageQueueId<T>(id), data.CorrelationId));
            }
            catch (Exception exception)
            {
                return new QueueOutputMessage(_sentMessageFactory.Create(null, data.CorrelationId), exception);
            }
        }

        /// <inheritdoc />
        public IQueueOutputMessages Send(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            try
            {
                if (_batchSupported)
                {
                    return _sendMessageBatch.Handle(new SendMessageCommandBatch(messages));
                }

                var rc = new ConcurrentBag<IQueueOutputMessage>();
                Parallel.ForEach(messages, m =>
                {
                    try
                    {
                        var id = _sendMessage.Handle(
                            new SendMessageCommand(m.Message, m.MessageData));
                        rc.Add(
                            new QueueOutputMessage(_sentMessageFactory.Create(new MessageQueueId<T>(id),
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

        /// <inheritdoc />
        public async Task<IQueueOutputMessage> SendAsync(IMessage messageToSend, IAdditionalMessageData data)
        {
            try
            {
                var id = await _sendMessageAsync.HandleAsync(
                    new SendMessageCommand(messageToSend, data)).ConfigureAwait(false);
                return new QueueOutputMessage(_sentMessageFactory.Create(new MessageQueueId<T>(id), data.CorrelationId));
            }
            catch (Exception exception)
            {
                return new QueueOutputMessage(_sentMessageFactory.Create(null, data.CorrelationId), exception);
            }
        }

        /// <inheritdoc />
        public async Task<IQueueOutputMessages> SendAsync(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            try
            {
                if (_batchSupported)
                {
                    return await _sendMessageBatchAsync.HandleAsync(new SendMessageCommandBatch(messages))
                        .ConfigureAwait(false);
                }

                var rc = new ConcurrentBag<IQueueOutputMessage>();
                foreach (var m in messages)
                {
                    try
                    {
                        var id = await _sendMessageAsync.HandleAsync(
                            new SendMessageCommand(m.Message, m.MessageData)).ConfigureAwait(false);
                        rc.Add(new QueueOutputMessage(_sentMessageFactory.Create(new MessageQueueId<T>(id), m.MessageData.CorrelationId)));
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
        #endregion
    }
}
