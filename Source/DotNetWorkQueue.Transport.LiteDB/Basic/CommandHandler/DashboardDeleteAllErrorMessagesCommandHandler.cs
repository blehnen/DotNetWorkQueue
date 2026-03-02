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
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    internal class DashboardDeleteAllErrorMessagesCommandHandler : ICommandHandlerWithOutput<DashboardDeleteAllErrorMessagesCommand, long>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly Lazy<LiteDbMessageQueueTransportOptions> _options;

        public DashboardDeleteAllErrorMessagesCommandHandler(
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

        public long Handle(DashboardDeleteAllErrorMessagesCommand command)
        {
            using (var db = _connectionInformation.GetDatabase())
            {
                db.Database.BeginTrans();
                try
                {
                    var errorCol = db.Database.GetCollection<Schema.MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);
                    var errorRecords = errorCol.Query().ToList();

                    if (errorRecords.Count == 0)
                    {
                        db.Database.Commit();
                        return 0;
                    }

                    var queueIds = errorRecords.Select(x => x.QueueId).ToList();

                    // Delete error tracking records
                    var errorTrack = db.Database.GetCollection<Schema.ErrorTrackingTable>(_tableNameHelper.ErrorTrackingName);
                    errorTrack.DeleteMany(x => queueIds.Contains(x.QueueId));

                    // Delete queue body records (QueueId == Id in QueueName)
                    var queueCol = db.Database.GetCollection(_tableNameHelper.QueueName);
                    foreach (var queueId in queueIds)
                    {
                        queueCol.Delete(queueId);
                    }

                    // Delete status table records if enabled
                    if (_options.Value.EnableStatusTable)
                    {
                        var statusCol = db.Database.GetCollection<Schema.StatusTable>(_tableNameHelper.StatusName);
                        statusCol.DeleteMany(x => queueIds.Contains(x.QueueId));
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
