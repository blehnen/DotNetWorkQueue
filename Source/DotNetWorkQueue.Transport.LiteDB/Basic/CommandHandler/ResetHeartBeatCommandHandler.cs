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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    /// <summary>
    /// Resets the status for a specific record
    /// </summary>
    internal class ResetHeartBeatCommandHandler : ICommandHandlerWithOutput<ResetHeartBeatCommand<int>, long>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHeartBeatCommandHandler"/> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public ResetHeartBeatCommandHandler(TableNameHelper tableNameHelper,
            IConnectionInformation connectionInformation)
        {
            _tableNameHelper = tableNameHelper;
            _connectionInformation = connectionInformation;

            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
        }
        /// <inheritdoc />
        public long Handle(ResetHeartBeatCommand<int> inputCommand)
        {
            using (var db = new LiteDatabase(_connectionInformation.ConnectionString))
            {
                db.BeginTrans();
                try
                {
                    var col = db.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);

                    var results = col.Query()
                        .Where(x => x.Status == QueueStatuses.Processing)
                        .Where(x => x.HeartBeat != null && x.HeartBeat.Value == inputCommand.MessageReset.HeartBeat)
                        .Where(x => x.QueueId == inputCommand.MessageReset.QueueId)
                        .Limit(1)
                        .ToList();

                    if (results.Count == 1)
                    {
                        results[0].Status = QueueStatuses.Waiting;
                        results[0].HeartBeat = null;
                        col.Update(results[0]);
                    }

                    db.Commit();
                    return results.Count;
                }
                catch
                {
                    db.Rollback();
                    throw;
                }
            }
        }
    }
}
