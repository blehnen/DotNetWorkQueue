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
using System.Data.SQLite;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.SQLite.Basic.Query;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    /// <summary>
    /// Dequeues a message.
    /// </summary>
    internal class ReceiveMessageQueryHandler : IQueryHandler<ReceiveMessageQuery, IReceivedMessageInternal>
    {
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly IConnectionInformation _connectionInformation;
        private readonly MessageDeQueue _messageDeQueue;
        private readonly BuildDequeueCommand _buildDequeueCommand;
        private readonly ISqLiteTransactionFactory _transactionFactory;
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryHandler" /> class.
        /// </summary>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="buildDequeueCommand">The build dequeue command.</param>
        /// <param name="messageDeQueue">The message de queue.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        public ReceiveMessageQueryHandler(ISqLiteMessageQueueTransportOptionsFactory optionsFactory, 
            TableNameHelper tableNameHelper, 
            IConnectionInformation connectionInformation,
            BuildDequeueCommand buildDequeueCommand,
            MessageDeQueue messageDeQueue,
            ISqLiteTransactionFactory transactionFactory)
        {
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => buildDequeueCommand, buildDequeueCommand);
            Guard.NotNull(() => messageDeQueue, messageDeQueue);

            _options = new Lazy<SqLiteMessageQueueTransportOptions>(optionsFactory.Create);
            _tableNameHelper = tableNameHelper;
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
        /// <exception cref="PoisonMessageException">An error has occured trying to re-assemble a message de-queued from the SQLite</exception>
        /// <exception cref="SqLiteMessageQueueId"></exception>
        /// <exception cref="SqLiteMessageQueueCorrelationId"></exception>
        public IReceivedMessageInternal Handle(ReceiveMessageQuery query)
        {
            if(!DatabaseExists.Exists(_connectionInformation.ConnectionString))
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
                                    _tableNameHelper.StatusName);
                        }
                        else
                        {
                            commandString =
                                  GetDeQueueCommand(_tableNameHelper.MetaDataName, _tableNameHelper.QueueName,
                                    false,
                                    _tableNameHelper.StatusName);
                        }
                        _buildDequeueCommand.BuildCommand(selectCommand, query.MessageId, commandString);
                        using (var reader = selectCommand.ExecuteReader())
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
        /// <returns></returns>
        private CommandString GetDeQueueCommand(string metaTableName, string queueTableName, bool forRpc, string statusTableName)
        {
            return ReceiveMessage.GetDeQueueCommand(metaTableName, queueTableName, forRpc, statusTableName, _options.Value);
        }
    }
}
