// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
    /// <inheritdoc />
    /// <summary>
    /// Sends a heart beat for a queue record
    /// </summary>
    internal class SendHeartBeatCommandHandler : ICommandHandlerWithOutput<SendHeartBeatCommand<int>, DateTime?>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatCommandHandler" /> class.
        /// </summary>
        public SendHeartBeatCommandHandler(LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        /// <inheritdoc />
        public DateTime? Handle(SendHeartBeatCommand<int> command)
        {
            using (var db = _connectionInformation.GetDatabase())
            {
                db.Database.BeginTrans();
                try
                {
                    var col = db.Database.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);

                    var results = col.Query()
                        .Where(x => x.QueueId == command.QueueId)
                        .Limit(1)
                        .ToList();

                    DateTime? date = null;
                    if (results.Count == 1)
                    {
                        var record = results[0];
                        date = DateTime.UtcNow;
                        record.HeartBeat = date;
                        col.Update(record);
                    }

                    db.Database.Commit();
                    return date;
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
