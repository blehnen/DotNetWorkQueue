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
    internal class DashboardResetAllStaleMessagesCommandHandler : ICommandHandlerWithOutput<DashboardResetAllStaleMessagesCommand, long>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly Lazy<LiteDbMessageQueueTransportOptions> _options;

        public DashboardResetAllStaleMessagesCommandHandler(
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

        public long Handle(DashboardResetAllStaleMessagesCommand command)
        {
            using (var db = _connectionInformation.GetDatabase())
            {
                db.Database.BeginTrans();
                try
                {
                    var meta = db.Database.GetCollection<MetaDataTable>(_tableNameHelper.MetaDataName);
                    var staleRecords = meta.Query()
                        .Where(x => x.Status == QueueStatuses.Processing)
                        .ToList();

                    if (staleRecords.Count == 0)
                    {
                        db.Database.Commit();
                        return 0;
                    }

                    foreach (var record in staleRecords)
                    {
                        record.Status = QueueStatuses.Waiting;
                        record.HeartBeat = null;
                        meta.Update(record);
                    }

                    if (_options.Value.EnableStatusTable)
                    {
                        var statusCol = db.Database.GetCollection<StatusTable>(_tableNameHelper.StatusName);
                        foreach (var record in staleRecords)
                        {
                            var statusRecord = statusCol.Query()
                                .Where(x => x.QueueId == record.QueueId)
                                .FirstOrDefault();

                            if (statusRecord != null)
                            {
                                statusRecord.Status = QueueStatuses.Waiting;
                                statusCol.Update(statusRecord);
                            }
                        }
                    }

                    db.Database.Commit();
                    return staleRecords.Count;
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
