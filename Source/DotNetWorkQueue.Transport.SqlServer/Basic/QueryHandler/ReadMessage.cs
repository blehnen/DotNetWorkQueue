using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Validation;

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
            Guard.NotNull(() => receivedMessageFactory, receivedMessageFactory);
            Guard.NotNull(() => messageFactory, messageFactory);
            Guard.NotNull(() => serialization, serialization);
            Guard.NotNull(() => headers, headers);

            _receivedMessageFactory = receivedMessageFactory;
            _messageFactory = messageFactory;
            _headers = headers;
            _serialization = serialization;
        }

        public IReceivedMessageInternal Read(SqlDataReader reader)
        {
            if (!reader.Read()) return null;

            //load up the message from the DB
            long id = 0;
            var correlationId = Guid.Empty;
            byte[] headerPayload = null;
            byte[] messagePayload = null;
            try
            {
                id = (long)reader["queueid"];
                correlationId = (Guid)reader["CorrelationID"];
                headerPayload = (byte[])reader["Headers"];
                messagePayload = (byte[])reader["body"];

                var headers = _serialization.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(headerPayload);
                var messageGraph = (MessageInterceptorsGraph)headers[_headers.StandardHeaders.MessageInterceptorGraph.Name];
                var message = _serialization.Serializer.BytesToMessage<MessageBody>(messagePayload, messageGraph, headers).Body;
                var newMessage = _messageFactory.Create(message, headers);

                return _receivedMessageFactory.Create(newMessage,
                    new MessageQueueId(id),
                    new MessageCorrelationId(correlationId));
            }
            catch (Exception error)
            {
                //at this point, the record has been de-queued, but it can't be processed.
                throw new PoisonMessageException(
                    "An error has occurred trying to re-assemble a message de-queued from the SQL server", error, new MessageQueueId(id), new MessageCorrelationId(correlationId), messagePayload, headerPayload);

            }
        }
    }
}
