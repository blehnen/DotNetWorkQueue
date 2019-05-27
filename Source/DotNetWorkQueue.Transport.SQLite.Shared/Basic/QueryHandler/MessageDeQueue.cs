using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic.QueryHandler
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

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        internal IReceivedMessageInternal HandleMessage(IDbConnection connection, IDbTransaction transaction, IDataReader reader, CommandString commandString)
        {
            if (!reader.Read())
            {
                return null;
            }

            //load up the message from the DB
            long id = 0;
            var correlationId = Guid.Empty;
            byte[] headerPayload = null;
            byte[] messagePayload = null;
            try
            {
                id = (long)reader["QueueID"];
                var cId = (string)reader["CorrelationID"];
                headerPayload = (byte[])reader["Headers"];
                messagePayload = (byte[])reader["Body"];

                correlationId = new Guid(cId);
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
                    new MessageQueueId(id),
                    new MessageCorrelationId(correlationId));
            }
            catch (Exception err)
            {
                //at this point, the record has been de-queued, but it can't be processed.
                throw new PoisonMessageException(
                    "An error has occured trying to re-assemble a message de-queued from SQLite",
                    err, new MessageQueueId(id),
                    new MessageCorrelationId(correlationId),
                    messagePayload,
                    headerPayload);
            }
        }
    }
}
