// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.SQLite.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    /// <summary>
    /// Dequeues a message.
    /// </summary>
    internal class ReceiveMessageQueryHandlerAsync : IQueryHandler<ReceiveMessageQueryAsync, Task<IReceivedMessageInternal>>
    {
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly SqLiteCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        private readonly BuildDequeueCommand _buildDequeueCommand;
        private readonly MessageDeQueue _messageDeQueue;
        private readonly ISqLiteTransactionFactory _transactionFactory;

        private const string RpcdequeueKey = "dequeueCommandRpc";
        private const string DequeueKey = "dequeueCommand";

        /// <summary>
        /// Initializes a new instance of the <see cref="DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler.ReceiveMessageQueryHandler" /> class.
        /// </summary>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="buildDequeueCommand">The build dequeue command.</param>
        /// <param name="messageDeQueue">The message de queue.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        public ReceiveMessageQueryHandlerAsync(ISqLiteMessageQueueTransportOptionsFactory optionsFactory,
            TableNameHelper tableNameHelper,
            SqLiteCommandStringCache commandCache,
            IConnectionInformation connectionInformation,
            BuildDequeueCommand buildDequeueCommand,
            MessageDeQueue messageDeQueue,
            ISqLiteTransactionFactory transactionFactory)
        {
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => buildDequeueCommand, buildDequeueCommand);
            Guard.NotNull(() => messageDeQueue, messageDeQueue);

            _options = new Lazy<SqLiteMessageQueueTransportOptions>(optionsFactory.Create);
            _tableNameHelper = tableNameHelper;
            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
            _buildDequeueCommand = buildDequeueCommand;
            _messageDeQueue = messageDeQueue;
            _transactionFactory = transactionFactory;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        /// <exception cref="PoisonMessageException">An error has occured trying to re-assemble a message de-queued from SQLite</exception>
        /// <exception cref="SqLiteMessageQueueId"></exception>
        /// <exception cref="SqLiteMessageQueueCorrelationId"></exception>
        public async Task<IReceivedMessageInternal> Handle(ReceiveMessageQueryAsync query)
        {
            if (!DatabaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return null;
            }

            using (var connection = new SQLiteConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var transaction = _transactionFactory.Create(connection).BeginTransaction())
                {
                    using (var selectCommand = connection.CreateCommand())
                    {
                        selectCommand.Transaction = transaction;
                        CommandString commandString;
                        if (query.MessageId != null && query.MessageId.HasValue)
                        {
                            commandString = GetDeQueueCommand(_tableNameHelper.MetaDataName, _tableNameHelper.QueueName,
                                    true,
                                    _tableNameHelper.StatusName, query.Routes);
                        }
                        else
                        {
                            commandString =
                                  GetDeQueueCommand(_tableNameHelper.MetaDataName, _tableNameHelper.QueueName,
                                    false,
                                    _tableNameHelper.StatusName, query.Routes);
                        }

                        if (commandString == null)
                            throw new DotNetWorkQueueException("Failed to generate command text for de-queue of messages");

                        _buildDequeueCommand.BuildCommand(selectCommand, query.MessageId, commandString, _options.Value, query.Routes);
                        using (var reader = await selectCommand.ExecuteReaderAsync().ConfigureAwait(false))
                        {
                            return _messageDeQueue.HandleMessage(connection, transaction, reader, commandString);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Gets the de queue command.
        /// </summary>
        /// <param name="metaTableName">Name of the meta table.</param>
        /// <param name="queueTableName">Name of the queue table.</param>
        /// <param name="forRpc">if set to <c>true</c> [for RPC].</param>
        /// <param name="statusTableName">Name of the status table.</param>
        /// <param name="routes">The routes.</param>
        /// <returns></returns>
        private CommandString GetDeQueueCommand(string metaTableName, string queueTableName, bool forRpc, string statusTableName, List<string> routes)
        {
            if (routes == null || routes.Count == 0)
            {
                if (forRpc && _commandCache.Contains(RpcdequeueKey))
                {
                    return _commandCache.Get(RpcdequeueKey);
                }
                if (_commandCache.Contains(DequeueKey))
                {
                    return _commandCache.Get(DequeueKey);
                }
            }

            var command = ReceiveMessage.GetDeQueueCommand(metaTableName, queueTableName, forRpc, statusTableName, _options.Value, routes);
            if (routes != null && routes.Count > 0)
            { //TODO - cache based on route
                return command;
            }
            else
            {
                return _commandCache.Add(forRpc ? RpcdequeueKey : DequeueKey, command);
            }
        }
    }
}
