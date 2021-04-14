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
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    /// <summary>
    /// Finds error messages that need to be deleted
    /// </summary>
    public class FindErrorRecordsToDeleteQueryHandler : IQueryHandler<FindErrorMessagesToDeleteQuery<int>, IEnumerable<int>>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly IMessageErrorConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindErrorRecordsToDeleteQueryHandler"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="configuration">The configuration.</param>
        public FindErrorRecordsToDeleteQueryHandler(LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper,
            IMessageErrorConfiguration configuration)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => configuration, configuration);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
            _configuration = configuration;
        }

        /// <inheritdoc />
        public IEnumerable<int> Handle(FindErrorMessagesToDeleteQuery<int> query)
        {
            if (query.Cancellation.IsCancellationRequested)
            {
                return Enumerable.Empty<int>();
            }

            using (var db = _connectionInformation.GetDatabase())
            {
                //before executing a query, double check that we aren't stopping
                //otherwise, there is a chance that the tables no longer exist in memory mode
                if (query.Cancellation.IsCancellationRequested)
                {
                    return Enumerable.Empty<int>();
                }

                var col = db.Database.GetCollection<Schema.MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);

                var date = DateTime.UtcNow.Subtract(_configuration.MessageAge);
                var results = col.Query()
                    .Where(x => x.LastExceptionDate < date)
                    .ToList();

                var data = new List<int>(results.Count);
                foreach (var record in results)
                {
                    data.Add(record.QueueId);
                }

                return data;
            }
        }
    }
}
