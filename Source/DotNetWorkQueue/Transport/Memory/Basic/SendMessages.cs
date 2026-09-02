// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// Sends a new message to an existing queue
    /// </summary>
    public class SendMessages : ISendMessages
    {
        #region Member Level Variables
        private readonly ISentMessageFactory _sentMessageFactory;
        private readonly IDataStorageSendMessage _dataStorage;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessages" /> class.
        /// </summary>
        /// <param name="sentMessageFactory">The sent message factory.</param>
        /// <param name="dataStorage">The data storage.</param>
        public SendMessages(ISentMessageFactory sentMessageFactory,
            IDataStorageSendMessage dataStorage)
        {
            Guard.NotNull(sentMessageFactory);
            Guard.NotNull(dataStorage);
            _sentMessageFactory = sentMessageFactory;
            _dataStorage = dataStorage;
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
                var id = _dataStorage.SendMessage(messageToSend, data);
                return new QueueOutputMessage(_sentMessageFactory.Create(new MessageQueueId(id), data.CorrelationId));
            }
            catch (Exception exception)
            {
                return new QueueOutputMessage(_sentMessageFactory.Create(null, data.CorrelationId), exception);
            }
        }

        /// <summary>Sends one message, reporting a failure rather than throwing.</summary>
        private IQueueOutputMessage SendOne(QueueMessage<IMessage, IAdditionalMessageData> message)
        {
            try
            {
                var id = _dataStorage.SendMessage(message.Message, message.MessageData);
                return new QueueOutputMessage(
                    _sentMessageFactory.Create(new MessageQueueId(id), message.MessageData.CorrelationId));
            }
            catch (Exception error)
            {
                return new QueueOutputMessage(
                    _sentMessageFactory.Create(null, message.MessageData.CorrelationId), error);
            }
        }
        #endregion

        /// <summary>
        /// Sends a new message to an existing queue
        /// </summary>
        /// <param name="messages">The messages, in caller order.</param>
        /// <returns>One result per message, in the same order.</returns>
        /// <remarks>
        /// A straight loop rather than a <see cref="Parallel"/> fan-out. The fan-out returned its
        /// results in whatever order the threads finished, while every other transport returns
        /// them in caller order - which is what a caller needs to match a generated id back to
        /// the message it sent, and what 0.9.41 declared the contract to be.
        /// <para>
        /// It bought nothing to offset that. The store behind this is an in-memory concurrent
        /// dictionary, so a send waits on nothing the parallelism could overlap; measured, the two
        /// shapes were the same speed to within the noise of the measurement.
        /// </para>
        /// </remarks>
        public IQueueOutputMessages Send(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            try
            {
                var rc = new List<IQueueOutputMessage>(messages.Count);
                foreach (var m in messages)
                {
                    rc.Add(SendOne(m));
                }
                return new QueueOutputMessages(rc);
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
                var id = await _dataStorage.SendMessageAsync(messageToSend, data).ConfigureAwait(false);
                return new QueueOutputMessage(_sentMessageFactory.Create(new MessageQueueId(id), data.CorrelationId));
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
                //a List rather than the ConcurrentBag this used to fill from a sequential loop:
                //the bag hands its contents back in no particular order, so the results did not
                //match the caller's input order either
                var rc = new List<IQueueOutputMessage>(messages.Count);
                foreach (var m in messages)
                {
                    try
                    {
                        var id = await _dataStorage.SendMessageAsync(m.Message, m.MessageData).ConfigureAwait(false);
                        rc.Add(new QueueOutputMessage(_sentMessageFactory.Create(new MessageQueueId(id), m.MessageData.CorrelationId)));
                    }
                    catch (Exception error)
                    {
                        rc.Add(new QueueOutputMessage(_sentMessageFactory.Create(null, m.MessageData.CorrelationId), error));
                    }
                }
                return new QueueOutputMessages(rc);
            }
            catch (Exception exception)
            {
                throw new DotNetWorkQueueException("An error occurred while sending a message", exception);
            }
        }
    }
}
