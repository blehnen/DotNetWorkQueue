﻿// ---------------------------------------------------------------------
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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Validation;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler
{
    /// <inheritdoc />
    internal class ReceiveMessageQueryHandler : IQueryHandler<ReceiveMessageQuery<NpgsqlConnection, NpgsqlTransaction>,
        IReceivedMessageInternal>
    {
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;
        private readonly ITableNameHelper _tableNameHelper;
        private readonly IReceivedMessageFactory _receivedMessageFactory;
        private readonly PostgreSqlCommandStringCache _commandCache;
        private readonly IMessageFactory _messageFactory;
        private readonly ICompositeSerialization _serialization;
        private readonly IHeaders _headers;
        private readonly IGetTime _getTime;
        private readonly QueueConsumerConfiguration _configuration;


        /// <summary>Initializes a new instance of the <see cref="ReceiveMessageQueryHandler" /> class.</summary>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="receivedMessageFactory">The received message factory.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="messageFactory">The message factory.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="serialization">The serialization.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        /// <param name="configuration">Queue Configuration</param>
        public ReceiveMessageQueryHandler(IPostgreSqlMessageQueueTransportOptionsFactory optionsFactory,
            ITableNameHelper tableNameHelper,
            IReceivedMessageFactory receivedMessageFactory,
            PostgreSqlCommandStringCache commandCache,
            IMessageFactory messageFactory,
            IHeaders headers,
            ICompositeSerialization serialization,
            IGetTimeFactory getTimeFactory,
            QueueConsumerConfiguration configuration)
        {
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => receivedMessageFactory, receivedMessageFactory);
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => messageFactory, messageFactory);
            Guard.NotNull(() => serialization, serialization);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);
            Guard.NotNull(() => configuration, configuration);

            _options = new Lazy<PostgreSqlMessageQueueTransportOptions>(optionsFactory.Create);
            _tableNameHelper = tableNameHelper;
            _receivedMessageFactory = receivedMessageFactory;
            _commandCache = commandCache;
            _messageFactory = messageFactory;
            _headers = headers;
            _serialization = serialization;
            _getTime = getTimeFactory.Create();
            _configuration = configuration;
        }

        /// <inheritdoc />
        public IReceivedMessageInternal Handle(ReceiveMessageQuery<NpgsqlConnection, NpgsqlTransaction> query)
        {
            using (var selectCommand = query.Connection.CreateCommand())
            {
                selectCommand.Transaction = query.Transaction;
                selectCommand.CommandText =
                    ReceiveMessage.GetDeQueueCommand(_commandCache, _tableNameHelper, _options.Value,
                        _configuration, query.Routes, out var userParameters);

                selectCommand.Parameters.Add("@CurrentDate", NpgsqlDbType.Bigint);
                selectCommand.Parameters["@CurrentDate"].Value = _getTime.GetCurrentUtcDate().Ticks;


                if (_options.Value.AdditionalColumnsOnMetaData && userParameters != null && userParameters.Count > 0)
                {
                    foreach (var userParam in userParameters)
                    {
                        selectCommand.Parameters.Add(userParam.Clone()); //clone to avoid sharing
                    }
                }

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

                using (var reader = selectCommand.ExecuteReader())
                {
                    if (!reader.Read()) return null;

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

                        headers =
                            _serialization.InternalSerializer
                                .ConvertBytesTo<IDictionary<string, object>>(headerPayload);
                        var messageGraph =
                            (MessageInterceptorsGraph)headers[_headers.StandardHeaders.MessageInterceptorGraph.Name];
                        var message = _serialization.Serializer
                            .BytesToMessage<MessageBody>(messagePayload, messageGraph, headers).Body;
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
                            "An error has occurred trying to re-assemble a message de-queued from the server ", error,
                            new MessageQueueId<long>(id), new MessageCorrelationId<Guid>(correlationId), headersLocal, messagePayload,
                            headerPayload);

                    }
                }
            }
        }
    }
}
