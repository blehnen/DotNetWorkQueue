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
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    /// <summary>
    /// Moves a record from the meta table to the error table
    /// </summary>
    public class MoveRecordToErrorQueueCommandHandler : ICommandHandler<MoveRecordToErrorQueueCommand<int>>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly Lazy<LiteDbMessageQueueTransportOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveRecordToErrorQueueCommandHandler"/> class.
        /// </summary>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public MoveRecordToErrorQueueCommandHandler(
            ILiteDbMessageQueueTransportOptionsFactory optionsFactory,
            LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => optionsFactory, optionsFactory);

            _options = new Lazy<LiteDbMessageQueueTransportOptions>(optionsFactory.Create);
            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        /// <inheritdoc />
        public void Handle(MoveRecordToErrorQueueCommand<int> command)
        {
            using (var db = _connectionInformation.GetDatabase())
            {
                bool delete = false;
                db.Database.BeginTrans();
                try
                {
                    var meta = db.Database.GetCollection<MetaDataTable>(_tableNameHelper.MetaDataName);
                    var results = meta.Query()
                        .Where(x => x.QueueId == command.QueueId)
                        .ToList();

                    if (results != null && results.Count == 1)
                    {
                        //move record to error table
                        var errorRecord = new MetaDataErrorsTable()
                        {
                            LastExceptionDate = DateTime.UtcNow,
                            LastException = command.Exception.ToString(),
                            QueueId = results[0].QueueId,
                            Status = QueueStatuses.Error,
                            CorrelationId = results[0].CorrelationId,
                            HeartBeat = results[0].HeartBeat,
                            ExpirationTime = results[0].ExpirationTime,
                            QueueProcessTime = results[0].QueueProcessTime,
                            QueuedDateTime = results[0].QueuedDateTime,
                            Route = results[0].Route
                        };

                        var errorCol =
                            db.Database.GetCollection<MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);
                        errorCol.Insert(errorRecord);

                        //delete record from metadata table
                        delete = meta.Delete(results[0].Id);

                        //update status table
                        if (_options.Value.EnableStatusTable)
                        {
                            var colStatus = db.Database.GetCollection<StatusTable>(_tableNameHelper.StatusName);
                            var resultsStatus = colStatus.Query()
                                .Where(x => x.QueueId == command.QueueId)
                                .ToList();

                            if (resultsStatus != null && resultsStatus.Count == 1)
                            {
                                resultsStatus[0].Status = QueueStatuses.Error;
                                colStatus.Update(resultsStatus[0]);
                            }
                        }
                    }

                    if (delete)
                        db.Database.Commit();
                    else
                        db.Database.Rollback();
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
