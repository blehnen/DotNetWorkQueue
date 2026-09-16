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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Validation;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler
{
    internal class ReadMessage
    {
        private readonly IReceivedMessageFactory _receivedMessageFactory;
        private readonly ICompositeSerialization _serialization;
        private readonly IMessageFactory _messageFactory;
        private readonly IHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryHandler" /> class.
        /// </summary>
        /// <param name="receivedMessageFactory">The received message factory.</param>
        /// <param name="messageFactory">The message factory.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="serialization">The serialization.</param>
        public ReadMessage(IReceivedMessageFactory receivedMessageFactory,
            IMessageFactory messageFactory,
            IHeaders headers,
            ICompositeSerialization serialization)
        {
            Guard.NotNull(receivedMessageFactory);
            Guard.NotNull(messageFactory);
            Guard.NotNull(serialization);
            Guard.NotNull(headers);

            _receivedMessageFactory = receivedMessageFactory;
            _messageFactory = messageFactory;
            _headers = headers;
            _serialization = serialization;
        }

        /// <summary>Reads the de-queued message, and the heartbeat the de-queue stamped on it.</summary>
        /// <param name="reader">The reader.</param>
        /// <param name="claimedAt">
        /// The stamped heartbeat, or null when this de-queue shape does not write one - holding the
        /// transaction, or deleting rather than marking, or heartbeats off (GitHub #336).
        /// </param>
        public IReceivedMessageInternal Read(SqlDataReader reader, out DateTime? claimedAt)
        {
            claimedAt = null;
            if (!reader.Read()) return null;
            claimedAt = TryReadHeartBeat(reader);

            //load up the message from the DB
            long id = 0;
            var correlationId = Guid.Empty;
            IDictionary<string, object> headers = null;
            byte[] headerPayload = null;
            byte[] messagePayload = null;
            try
            {
                id = (long)reader["queueid"];
                correlationId = (Guid)reader["CorrelationID"];
                headerPayload = (byte[])reader["Headers"];
                messagePayload = (byte[])reader["body"];

                headers = _serialization.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(headerPayload);
                var messageGraph = (MessageInterceptorsGraph)headers[_headers.StandardHeaders.MessageInterceptorGraph.Name];
                var message = _serialization.Serializer.BytesToMessage<MessageBody>(messagePayload, messageGraph, headers).Body;
                var newMessage = _messageFactory.Create(message, headers);

                return _receivedMessageFactory.Create(newMessage,
                    new MessageQueueId<long>(id),
                    new MessageCorrelationId<Guid>(correlationId));
            }
            catch (Exception error)
            {
                var headersLocal = headers != null ? new Dictionary<string, object>(headers) : new Dictionary<string, object>();
                //at this point, the record has been de-queued, but it can't be processed.
                throw new PoisonMessageException(
                    "An error has occurred trying to re-assemble a message de-queued from the SQL server", error, new MessageQueueId<long>(id), new MessageCorrelationId<Guid>(correlationId), headersLocal, messagePayload, headerPayload);

            }
        }

        /// <summary>
        /// Reads the heartbeat column when the de-queue asked for one.
        /// </summary>
        /// <remarks>
        /// Found by name rather than position because only one of the three de-queue shapes selects it,
        /// and this reader serves all three. Looking it up keeps the reader from having to know which
        /// shape produced the row.
        /// </remarks>
        private static DateTime? TryReadHeartBeat(SqlDataReader reader)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (!string.Equals(reader.GetName(i), "HeartBeat", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (reader.IsDBNull(i))
                    return null;

                //GetUTCDate() wrote it, so it is UTC - but a datetime column carries no zone and the
                //reader hands back Unspecified. Saying so here stops anything downstream treating it as
                //local and shifting it by the machine's offset.
                return DateTime.SpecifyKind(reader.GetDateTime(i), DateTimeKind.Utc);
            }

            return null;
        }

    }
}
