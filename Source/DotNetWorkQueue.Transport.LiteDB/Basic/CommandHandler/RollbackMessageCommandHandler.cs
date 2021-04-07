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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    /// <summary>
    /// Marks a message as no longer being processed; i.e. waiting to be processed.
    /// </summary>
    internal class RollbackMessageCommandHandler : ICommandHandler<RollbackMessageCommand<int>>
    {
        private readonly IGetTimeFactory _getUtcDateQuery;
        private readonly Lazy<LiteDbMessageQueueTransportOptions> _options;
        private readonly IConnectionInformation _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="getUtcDateQuery">The get UTC date query.</param>
        /// <param name="options">The options.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="databaseExists">The database exists.</param>
        public RollbackMessageCommandHandler(IGetTimeFactory getUtcDateQuery,
            ILiteDbMessageQueueTransportOptionsFactory options, 
            TableNameHelper tableNameHelper,
            IConnectionInformation connectionInformation,
            DatabaseExists databaseExists)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => getUtcDateQuery, getUtcDateQuery);
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => databaseExists, databaseExists);

            _getUtcDateQuery = getUtcDateQuery;
            _options = new Lazy<LiteDbMessageQueueTransportOptions>(options.Create);
            _tableNameHelper = tableNameHelper;
            _connectionInformation = connectionInformation;
            _databaseExists = databaseExists;
        }
        /// <summary>
        /// Handles the specified rollback command.
        /// </summary>
        /// <param name="rollBackCommand">The rollBackCommand.</param>
        public void Handle(RollbackMessageCommand<int> rollBackCommand)
        {
            if (!_databaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return;
            }

            using (var db = new LiteDatabase(_connectionInformation.ConnectionString))
            {
                db.BeginTrans();
                try
                {
                    var col = db.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);

                    var results2 = col.Query()
                        .Where(x => x.QueueId.Equals(rollBackCommand.QueueId))              
                        .Limit(1)
                        .ToList();

                    if (results2 != null && results2.Count == 1)
                    {
                        var record = results2[0];
                        if (record != null)
                        {
                            var process = true;
                            if (rollBackCommand.LastHeartBeat.HasValue)
                            {
                                //heartbeat must match
                                if (TrimMilliseconds(record.HeartBeat) != TrimMilliseconds(rollBackCommand.LastHeartBeat))
                                {
                                    process = false;
                                }
                            }

                            if (process)
                            {
                                if (_options.Value.EnableDelayedProcessing &&
                                    rollBackCommand.IncreaseQueueDelay.HasValue)
                                {
                                    var dtUtcDate = _getUtcDateQuery.Create().GetCurrentUtcDate();
                                    var dtUtcDateIncreased = dtUtcDate.Add(rollBackCommand.IncreaseQueueDelay.Value);

                                    //move to future
                                    record.QueueProcessTime = dtUtcDateIncreased;
                                }

                                if (_options.Value.EnableHeartBeat)
                                    record.HeartBeat = null;

                                record.Status = QueueStatuses.Waiting;

                                col.Update(record);

                                if (_options.Value.EnableStatusTable)
                                {
                                    var statusCol = db.GetCollection<Schema.StatusTable>(_tableNameHelper.StatusName);
                                    var results = statusCol.Query()
                                        .Where(x => x.QueueId.Equals(record.QueueId))
                                        .Limit(1)
                                        .ToList();

                                    if (results.Count == 1)
                                    {
                                        var statusRecord = results[0];
                                        statusRecord.Status = record.Status;
                                        statusCol.Update(statusRecord);
                                    }
                                }
                            }
                        }
                    }

                    db.Commit();
                }
                catch
                {
                    db.Rollback();
                    throw;
                }
            }
        }

        private DateTime? TrimMilliseconds(DateTime? dt)
        {
            if (!dt.HasValue)
                return null;

            return new DateTime(dt.Value.Year, dt.Value.Month, dt.Value.Day, dt.Value.Hour, dt.Value.Minute, dt.Value.Second, 0, dt.Value.Kind);
        }
    }
}
