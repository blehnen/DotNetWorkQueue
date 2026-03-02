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
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    internal class DashboardRequeueErrorMessageCommandHandler : ICommandHandlerWithOutput<DashboardRequeueErrorMessageCommand, long>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly Lazy<LiteDbMessageQueueTransportOptions> _options;

        public DashboardRequeueErrorMessageCommandHandler(
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

        public long Handle(DashboardRequeueErrorMessageCommand command)
        {
            var id = int.Parse(command.MessageId);

            using (var db = _connectionInformation.GetDatabase())
            {
                db.Database.BeginTrans();
                try
                {
                    var errorCol = db.Database.GetCollection<MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);
                    var errorRecord = errorCol.Query()
                        .Where(x => x.QueueId == id)
                        .FirstOrDefault();

                    if (errorRecord == null)
                    {
                        db.Database.Rollback();
                        return 0;
                    }

                    // Insert a new MetaData record with Waiting status
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

                    var meta = db.Database.GetCollection<MetaDataTable>(_tableNameHelper.MetaDataName);
                    meta.Insert(newMeta);

                    // Delete from error table
                    errorCol.DeleteMany(x => x.QueueId == id);

                    // Delete from error tracking table
                    var errorTrack = db.Database.GetCollection<ErrorTrackingTable>(_tableNameHelper.ErrorTrackingName);
                    errorTrack.DeleteMany(x => x.QueueId == id);

                    // Update status table if enabled
                    if (_options.Value.EnableStatusTable)
                    {
                        var statusCol = db.Database.GetCollection<StatusTable>(_tableNameHelper.StatusName);
                        var statusRecord = statusCol.Query()
                            .Where(x => x.QueueId == id)
                            .FirstOrDefault();

                        if (statusRecord != null)
                        {
                            statusRecord.Status = QueueStatuses.Waiting;
                            statusCol.Update(statusRecord);
                        }
                    }

                    db.Database.Commit();
                    return 1;
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
