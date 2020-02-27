// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler
{
    /// <inheritdoc />
    internal class ReceiveMessageQueryHandlerAsync : IQueryHandler<
        ReceiveMessageQueryAsync<NpgsqlConnection, NpgsqlTransaction>, Task<IReceivedMessageInternal>>
    {
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly IReceivedMessageFactory _receivedMessageFactory;
        private readonly PostgreSqlCommandStringCache _commandCache;
        private readonly IMessageFactory _messageFactory;
        private readonly ICompositeSerialization _serialization;
        private readonly IHeaders _headers;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler.ReceiveMessageQueryHandler" /> class.
        /// </summary>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="receivedMessageFactory">The received message factory.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="messageFactory">The message factory.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="serialization">The serialization.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public ReceiveMessageQueryHandlerAsync(IPostgreSqlMessageQueueTransportOptionsFactory optionsFactory,
            TableNameHelper tableNameHelper,
            IReceivedMessageFactory receivedMessageFactory,
            PostgreSqlCommandStringCache commandCache,
            IMessageFactory messageFactory,
            IHeaders headers,
            ICompositeSerialization serialization,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => receivedMessageFactory, receivedMessageFactory);
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => messageFactory, messageFactory);
            Guard.NotNull(() => serialization, serialization);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);

            _options = new Lazy<PostgreSqlMessageQueueTransportOptions>(optionsFactory.Create);
            _tableNameHelper = tableNameHelper;
            _receivedMessageFactory = receivedMessageFactory;
            _commandCache = commandCache;
            _messageFactory = messageFactory;
            _headers = headers;
            _serialization = serialization;
            _getTime = getTimeFactory.Create();
        }

        /// <inheritdoc />
        public async Task<IReceivedMessageInternal> Handle(
            ReceiveMessageQueryAsync<NpgsqlConnection, NpgsqlTransaction> query)
        {
            using (var selectCommand = query.Connection.CreateCommand())
            {
                selectCommand.Transaction = query.Transaction;
                selectCommand.CommandText =
                    ReceiveMessage.GetDeQueueCommand(_commandCache, _tableNameHelper, _options.Value,
                        query.Routes);


                selectCommand.Parameters.Add("@CurrentDate", NpgsqlDbType.Bigint);
                selectCommand.Parameters["@CurrentDate"].Value = _getTime.GetCurrentUtcDate().Ticks;

                if (_options.Value.EnableRoute && query.Routes != null && query.Routes.Count > 0)
                {
                    var routeCounter = 1;
                    foreach (var route in query.Routes)
                    {
                        selectCommand.Parameters.Add("@Route" + routeCounter, NpgsqlDbType.Varchar);
                        selectCommand.Parameters["@Route" + routeCounter].Value = route;
                        routeCounter++;
                    }
                }

                using (var reader = await selectCommand.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    if (!reader.Read()) return null;

                    //load up the message from the DB
                    long id = 0;
                    var correlationId = Guid.Empty;
                    byte[] headerPayload = null;
                    byte[] messagePayload = null;

                    try
                    {
                        id = (long) reader["queueid"];
                        correlationId = (Guid) reader["CorrelationID"];
                        headerPayload = (byte[]) reader["Headers"];
                        messagePayload = (byte[]) reader["body"];

                        var headers =
                            _serialization.InternalSerializer
                                .ConvertBytesTo<IDictionary<string, object>>(headerPayload);
                        var messageGraph =
                            (MessageInterceptorsGraph) headers[_headers.StandardHeaders.MessageInterceptorGraph.Name];
                        var message = _serialization.Serializer
                            .BytesToMessage<MessageBody>(messagePayload, messageGraph, headers).Body;
                        var newMessage = _messageFactory.Create(message, headers);

                        return _receivedMessageFactory.Create(newMessage,
                            new MessageQueueId(id),
                            new MessageCorrelationId(correlationId));
                    }
                    catch (Exception error)
                    {
                        //at this point, the record has been de-queued, but it can't be processed.
                        throw new PoisonMessageException(
                            "An error has occurred trying to re-assemble a message de-queued from the server", error,
                            new MessageQueueId(id), new MessageCorrelationId(correlationId),
                            messagePayload, headerPayload);

                    }
                }
            }
        }
    }
}
