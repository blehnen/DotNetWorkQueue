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
using DotNetWorkQueue.Transport.LiteDb.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    /// <summary>
    ///  Returns if a job is currently pending execution. 'NotQueued' means no
    /// </summary>
    public class DoesJobExistQueryHandler : IQueryHandler<DoesJobExistQuery, QueueStatuses>
    {
        private readonly IQueryHandler<GetTableExistsQuery, bool> _tableExists;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoesJobExistQueryHandler" /> class.
        /// </summary>
        /// <param name="tableExists">The table exists.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public DoesJobExistQueryHandler(
            IQueryHandler<GetTableExistsQuery, bool> tableExists, 
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => tableExists, tableExists);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _tableExists = tableExists;
            _tableNameHelper = tableNameHelper;
        }

        /// <inheritdoc />
        public QueueStatuses Handle(DoesJobExistQuery query)
        {
            QueueStatuses returnStatus = QueueStatuses.NotQueued;

            var col = query.Database.GetCollection<Schema.StatusTable>(_tableNameHelper.StatusName);

            var results = col.Query()
                .Where(x => x.JobName == query.JobName)
                .Limit(1)
                .ToList();

            if (results.Count == 1)
            { 
                returnStatus = results[0].Status;
            }

            if (returnStatus == QueueStatuses.NotQueued &&
                _tableExists.Handle(new GetTableExistsQuery(query.Database,
                    _tableNameHelper.JobTableName)))
            {
                var colJob = query.Database.GetCollection<Schema.JobsTable>(_tableNameHelper.JobTableName);
                var resultJob = colJob.Query()
                    .Where(x => x.JobName == query.JobName)
                    .Limit(1)
                    .ToList();

                if (resultJob.Count == 1)
                {
                    var scheduleTime = resultJob[0].JobScheduledTime;
                    if (scheduleTime == query.ScheduledTime)
                    {
                        return QueueStatuses.Processed;
                    }
                }
            }
            return returnStatus;
        }
    }
}
