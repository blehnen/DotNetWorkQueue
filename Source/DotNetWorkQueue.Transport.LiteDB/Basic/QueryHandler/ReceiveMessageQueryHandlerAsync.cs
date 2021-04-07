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
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.LiteDb.Basic.Query;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    /// <summary>
    /// Dequeues a message.
    /// </summary>
    internal class ReceiveMessageQueryHandlerAsync : IQueryHandler<ReceiveMessageQueryAsync, Task<IReceivedMessageInternal>>
    {
        private readonly Lazy<LiteDbMessageQueueTransportOptions> _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly IConnectionInformation _connectionInformation;
        private readonly MessageDeQueue _messageDeQueue;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryHandlerAsync"/> class.
        /// </summary>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="messageDeQueue">The message de queue.</param>
        /// <param name="databaseExists">The database exists.</param>
        public ReceiveMessageQueryHandlerAsync(ILiteDbMessageQueueTransportOptionsFactory optionsFactory,
            TableNameHelper tableNameHelper,
            IConnectionInformation connectionInformation,
            MessageDeQueue messageDeQueue,
            DatabaseExists databaseExists)
        {
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => messageDeQueue, messageDeQueue);
            Guard.NotNull(() => databaseExists, databaseExists);

            _options = new Lazy<LiteDbMessageQueueTransportOptions>(optionsFactory.Create);
            _tableNameHelper = tableNameHelper;
            _connectionInformation = connectionInformation;
            _messageDeQueue = messageDeQueue;
            _databaseExists = databaseExists;
            
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public async Task<IReceivedMessageInternal> Handle(ReceiveMessageQueryAsync query)
        {
            return await Task.Run(() => //hack until litedb adds async methods
            {
                if (!_databaseExists.Exists(_connectionInformation.ConnectionString))
                {
                    return null;
                }

                //ensure created
                if (!_options.IsValueCreated)
                    _options.Value.ValidConfiguration();

                // Open database (or create if doesn't exist)
                using (var db = new LiteDatabase(_connectionInformation.ConnectionString))
                {
                    db.BeginTrans();
                    try
                    {
                        var record = DequeueRecord(query, db);
                        if (record != null)
                        {
                            return _messageDeQueue.HandleMessage(record.Item1, record.Item2.QueueId,
                                record.Item2.CorrelationId);
                        }
                    }
                    finally
                    {
                        db.Commit();
                    }
                }

                return null;
            });
        }

        private Tuple<Schema.QueueTable, Schema.MetaDataTable, Schema.StatusTable> DequeueRecord(ReceiveMessageQueryAsync query, LiteDatabase db)
        {
            var col = db.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);

            List<Schema.MetaDataTable> results;
            if (_options.Value.EnableRoute && query.Routes != null && query.Routes.Count > 0)
            {
                results = col.Query()
                    .Where(x => x.Status == QueueStatuses.Waiting)
                    .Where(x => x.HeartBeat == null)
                    .Where(x => x.QueueProcessTime == null || x.QueueProcessTime < DateTime.UtcNow)
                    .Where(x => x.ExpirationTime == null || x.ExpirationTime > DateTime.UtcNow)
                    .Where(x => query.Routes.Contains(x.Route))
                    .OrderBy(x => x.QueuedDateTime)
                    .Limit(1)
                    .ToList();
            }
            else
            {
                results = col.Query()
                    .Where(x => x.Status == QueueStatuses.Waiting)
                    .Where(x => x.HeartBeat == null)
                    .Where(x => x.QueueProcessTime == null || x.QueueProcessTime < DateTime.UtcNow)
                    .Where(x => x.ExpirationTime == null || x.ExpirationTime > DateTime.UtcNow)
                    .OrderBy(x => x.QueuedDateTime)
                    .Limit(1)
                    .ToList();
            }

            if (results.Count == 1)
            {
                var record = results[0];
                record.HeartBeat = DateTime.UtcNow;
                record.Status = QueueStatuses.Processing;

                col.Update(record);

                var colData = db.GetCollection<Schema.QueueTable>(_tableNameHelper.QueueName);
                var data = colData.FindById(record.QueueId);

                Schema.StatusTable status = null;
                if (_options.Value.EnableStatusTable)
                {
                    var statusCol = db.GetCollection<Schema.StatusTable>(_tableNameHelper.StatusName);
                    var resultsStatus = statusCol.Query()
                        .Where(x => x.QueueId.Equals(record.QueueId))
                        .Limit(1)
                        .ToList();

                    if (resultsStatus.Count == 1)
                    {
                        status = resultsStatus[0];
                        status.Status = QueueStatuses.Processing;
                        statusCol.Update(status);
                    }
                }

                return new Tuple<QueueTable, MetaDataTable, StatusTable>(data, record, status);
            }

            return null;
        }
    }
}
