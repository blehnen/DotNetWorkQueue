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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    internal class DashboardDeleteMessageCommandHandler : ICommandHandlerWithOutput<DashboardDeleteMessageCommand, long>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        public DashboardDeleteMessageCommandHandler(
            LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        public long Handle(DashboardDeleteMessageCommand command)
        {
            var id = int.Parse(command.MessageId);

            using (var db = _connectionInformation.GetDatabase())
            {
                db.Database.BeginTrans();
                try
                {
                    var col = db.Database.GetCollection(_tableNameHelper.QueueName);
                    var result = col.Delete(id);

                    // Continue in case we have orphaned records, regardless of result
                    var meta = db.Database.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                    meta.DeleteMany(x => x.QueueId == id);

                    var status = db.Database.GetCollection<Schema.StatusTable>(_tableNameHelper.StatusName);
                    status.DeleteMany(x => x.QueueId == id);

                    var errorTrack = db.Database.GetCollection<Schema.ErrorTrackingTable>(_tableNameHelper.ErrorTrackingName);
                    errorTrack.DeleteMany(x => x.QueueId == id);

                    var metaErrors = db.Database.GetCollection<Schema.MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);
                    metaErrors.DeleteMany(x => x.QueueId == id);

                    db.Database.Commit();
                    return result ? 1 : 0;
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
