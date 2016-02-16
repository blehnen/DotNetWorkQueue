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
using System.Data.SQLite;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;
namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    internal class MessageDeQueue
    {
        private readonly ICompositeSerialization _serialization;
        private readonly IHeaders _headers;
        private readonly IMessageFactory _messageFactory;
        private readonly IReceivedMessageFactory _receivedMessageFactory;

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        internal IReceivedMessageInternal HandleMessage(SQLiteConnection connection, SQLiteTransaction transaction, System.Data.Common.DbDataReader reader, CommandString commandString)
        {
            if (!reader.Read())
            {
                return null;
            }

            //load up the message from the DB
            long id = 0;
            var correlationId = Guid.Empty;
            try
            {
                id = (long)reader["queueid"];
                var cId = (string)reader["CorrelationID"];
                correlationId = new Guid(cId);
                var headers =
                    _serialization.InternalSerializer
                        .ConvertBytesTo<IDictionary<string, object>>(
                            (byte[])reader["Headers"]);

                var messageGraph =
                    (MessageInterceptorsGraph)
                        headers[_headers.StandardHeaders.MessageInterceptorGraph.Name];

                var message =
                    _serialization.Serializer.BytesToMessage<MessageBody>(
                        (byte[])reader["body"],
                        messageGraph).Body;
                var newMessage = _messageFactory.Create(message, headers);

                foreach (var additionalCommand in commandString.AdditionalCommands)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = additionalCommand;
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();

                return _receivedMessageFactory.Create(newMessage,
                    new SqLiteMessageQueueId(id),
                    new SqLiteMessageQueueCorrelationId(correlationId));
            }
            catch (Exception err)
            {
                //at this point, the record has been de-queued, but it can't be processed.
                throw new PoisonMessageException(
                    "An error has occured trying to re-assemble a message de-queued from SQLite",
                    err, new SqLiteMessageQueueId(id),
                    new SqLiteMessageQueueCorrelationId(correlationId));
            }
        }
    }
}
