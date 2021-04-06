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
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    /// <summary>
    /// Dequeues a message.
    /// </summary>
    internal class ReceiveMessageQueryHandler : IQueryHandler<ReceiveMessageQuery<IDbConnection, IDbTransaction>, IReceivedMessageInternal>
    {
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;
        private readonly ITableNameHelper _tableNameHelper;
        private readonly IConnectionInformation _connectionInformation;
        private readonly MessageDeQueue _messageDeQueue;
        private readonly BuildDequeueCommand _buildDequeueCommand;
        private readonly IDbFactory _dbFactory;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryHandler" /> class.
        /// </summary>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="buildDequeueCommand">The build dequeue command.</param>
        /// <param name="messageDeQueue">The message de queue.</param>
        /// <param name="dbFactory">The transaction factory.</param>
        /// <param name="databaseExists">The database exists.</param>
        public ReceiveMessageQueryHandler(ISqLiteMessageQueueTransportOptionsFactory optionsFactory, 
            ITableNameHelper tableNameHelper, 
            IConnectionInformation connectionInformation,
            BuildDequeueCommand buildDequeueCommand,
            MessageDeQueue messageDeQueue,
            IDbFactory dbFactory,
            DatabaseExists databaseExists)
        {
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => buildDequeueCommand, buildDequeueCommand);
            Guard.NotNull(() => messageDeQueue, messageDeQueue);
            Guard.NotNull(() => databaseExists, databaseExists);
            Guard.NotNull(() => dbFactory, dbFactory);

            _options = new Lazy<SqLiteMessageQueueTransportOptions>(optionsFactory.Create);
            _tableNameHelper = tableNameHelper;
            _connectionInformation = connectionInformation;
            _buildDequeueCommand = buildDequeueCommand;
            _messageDeQueue = messageDeQueue;
            _dbFactory = dbFactory;
            _databaseExists = databaseExists;
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public IReceivedMessageInternal Handle(ReceiveMessageQuery<IDbConnection, IDbTransaction> query)
        {
            if(!_databaseExists.Exists(_connectionInformation.ConnectionString))
            {           
                return null;
            }

            using (var connection = _dbFactory.CreateConnection(_connectionInformation.ConnectionString, false))
            {
                connection.Open();
                using (var transaction = _dbFactory.CreateTransaction(connection).BeginTransaction())
                {
                    using (var selectCommand = connection.CreateCommand())
                    {
                        selectCommand.Transaction = transaction;
                        CommandString commandString =
                                  GetDeQueueCommand(_tableNameHelper.MetaDataName, _tableNameHelper.QueueName,
                                    _tableNameHelper.StatusName, query.Routes);
                        
                        _buildDequeueCommand.BuildCommand(selectCommand, commandString, _options.Value, query.Routes);
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
        /// <param name="statusTableName">Name of the status table.</param>
        /// <param name="routes">The routes.</param>
        /// <returns></returns>
        private CommandString GetDeQueueCommand(string metaTableName, string queueTableName, string statusTableName, List<string> routes )
        {
            return ReceiveMessage.GetDeQueueCommand(metaTableName, queueTableName, statusTableName, _options.Value, routes);
        }
    }
}
