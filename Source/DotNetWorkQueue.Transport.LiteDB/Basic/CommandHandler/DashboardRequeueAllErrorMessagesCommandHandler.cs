// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Linq;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    internal class DashboardRequeueAllErrorMessagesCommandHandler : ICommandHandlerWithOutput<DashboardRequeueAllErrorMessagesCommand, long>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly Lazy<LiteDbMessageQueueTransportOptions> _options;

        public DashboardRequeueAllErrorMessagesCommandHandler(
            ILiteDbMessageQueueTransportOptionsFactory optionsFactory,
            LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _options = new Lazy<LiteDbMessageQueueTransportOptions>(optionsFactory.Create);
            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        public long Handle(DashboardRequeueAllErrorMessagesCommand command)
        {
            using (var db = _connectionInformation.GetDatabase())
            {
                db.Database.BeginTrans();
                try
                {
                    var errorCol = db.Database.GetCollection<MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);
                    var errorRecords = errorCol.Query().ToList();

                    if (errorRecords.Count == 0)
                    {
                        db.Database.Commit();
                        return 0;
                    }

                    var meta = db.Database.GetCollection<MetaDataTable>(_tableNameHelper.MetaDataName);
                    var queueIds = errorRecords.Select(x => x.QueueId).ToList();

                    // For each error record, insert or update MetaData with Waiting status
                    foreach (var errorRecord in errorRecords)
                    {
                        var existing = meta.Query()
                            .Where(x => x.QueueId == errorRecord.QueueId)
                            .FirstOrDefault();

                        if (existing != null)
                        {
                            existing.Status = QueueStatuses.Waiting;
                            existing.HeartBeat = null;
                            meta.Update(existing);
                        }
                        else
                        {
                            var newMeta = new MetaDataTable
                            {
                                QueueId = errorRecord.QueueId,
                                CorrelationId = errorRecord.CorrelationId,
                                Status = QueueStatuses.Waiting,
                                QueuedDateTime = errorRecord.QueuedDateTime,
                                QueueProcessTime = errorRecord.QueueProcessTime,
                                HeartBeat = null,
                                ExpirationTime = errorRecord.ExpirationTime,
                                Route = errorRecord.Route
                            };
                            meta.Insert(newMeta);
                        }
                    }

                    // Delete error tracking records
                    var errorTrack = db.Database.GetCollection<ErrorTrackingTable>(_tableNameHelper.ErrorTrackingName);
                    errorTrack.DeleteMany(x => queueIds.Contains(x.QueueId));

                    // Update status table if enabled
                    if (_options.Value.EnableStatusTable)
                    {
                        var statusCol = db.Database.GetCollection<StatusTable>(_tableNameHelper.StatusName);
                        foreach (var queueId in queueIds)
                        {
                            var statusRecord = statusCol.Query()
                                .Where(x => x.QueueId == queueId)
                                .FirstOrDefault();

                            if (statusRecord != null)
                            {
                                statusRecord.Status = QueueStatuses.Waiting;
                                statusCol.Update(statusRecord);
                            }
                        }
                    }

                    // Delete all error metadata records
                    errorCol.DeleteAll();

                    db.Database.Commit();
                    return errorRecords.Count;
                }
                catch
                {
                    db.Database.Rollback();
                    throw;
                }
            }
        }
    }
}
