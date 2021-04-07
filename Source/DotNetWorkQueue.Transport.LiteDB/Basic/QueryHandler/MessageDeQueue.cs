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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    /// <summary>
    /// Assembles a message from the database tables
    /// </summary>
    internal class MessageDeQueue
    {
        private readonly ICompositeSerialization _serialization;
        private readonly IHeaders _headers;
        private readonly IMessageFactory _messageFactory;
        private readonly IReceivedMessageFactory _receivedMessageFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageDeQueue"/> class.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <param name="messageFactory">The message factory.</param>
        /// <param name="receivedMessageFactory">The received message factory.</param>
        /// <param name="serialization">The serialization.</param>
        public MessageDeQueue(IHeaders headers,
             IMessageFactory messageFactory,
             IReceivedMessageFactory receivedMessageFactory,
             ICompositeSerialization serialization)
        {
            Guard.NotNull(() => serialization, serialization);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => messageFactory, messageFactory);
            Guard.NotNull(() => receivedMessageFactory, receivedMessageFactory);

            _messageFactory = messageFactory;
            _headers = headers;
            _serialization = serialization;
            _receivedMessageFactory = receivedMessageFactory;
        }

        internal IReceivedMessageInternal HandleMessage(Schema.QueueTable queueRecord, int queueId, Guid correlationId)
        {
            if (queueRecord == null)
            {
                return null;
            }

            //load up the message from the DB
            int id = queueId;
            byte[] headerPayload = null;
            byte[] messagePayload = null;
            try
            {
                headerPayload = queueRecord.Headers;
                messagePayload = queueRecord.Body;

                var headers =
                    _serialization.InternalSerializer
                        .ConvertBytesTo<IDictionary<string, object>>(
                            headerPayload);

                var messageGraph =
                    (MessageInterceptorsGraph)
                        headers[_headers.StandardHeaders.MessageInterceptorGraph.Name];

                var message =
                    _serialization.Serializer.BytesToMessage<MessageBody>(
                        messagePayload,
                        messageGraph, headers).Body;
                var newMessage = _messageFactory.Create(message, headers);

                return _receivedMessageFactory.Create(newMessage,
                    new MessageQueueId<int>(id),
                    new MessageCorrelationId<Guid>(correlationId));
            }
            catch (Exception err)
            {
                //at this point, the record has been de-queued, but it can't be processed.
                throw new PoisonMessageException(
                    "An error has occurred trying to re-assemble a de-queued message",
                    err, new MessageQueueId<int>(id),
                    new MessageCorrelationId<Guid>(correlationId),
                    messagePayload,
                    headerPayload);
            }
        }
    }
}
