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
    /// <inheritdoc />
    /// <summary>
    /// Updates the error count for a record
    /// </summary>
    internal class SetErrorCountCommandHandler : ICommandHandler<SetErrorCountCommand<int>>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetErrorCountCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public SetErrorCountCommandHandler(
            IConnectionInformation connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        /// <inheritdoc />
        public void Handle(SetErrorCountCommand<int> command)
        {
            using (var db = new LiteDatabase(_connectionInformation.ConnectionString))
            {
                db.BeginTrans();
                try
                {
                    var meta = db.GetCollection<Schema.ErrorTrackingTable>(_tableNameHelper.ErrorTrackingName);
                    var results = meta.Query()
                        .Where(x => x.QueueId == command.QueueId)
                        .Where(x => x.ExceptionType == command.ExceptionType)
                        .Limit(1)
                        .ToList();

                    if (results != null && results.Count == 1)
                    {
                        //update
                        results[0].RetryCount = results[0].RetryCount + 1;
                        meta.Update(results[0]);
                    }
                    else
                    {
                        var record = new Schema.ErrorTrackingTable()
                        {
                            QueueId = command.QueueId, ExceptionType = command.ExceptionType, RetryCount = 1
                        };
                        meta.Insert(record);
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
    }
}
