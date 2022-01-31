﻿// ---------------------------------------------------------------------
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
    public class FindExpiredRecordsToDeleteQueryHandler : IQueryHandler<FindExpiredMessagesToDeleteQuery<int>, IEnumerable<int>>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindExpiredRecordsToDeleteQueryHandler"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public FindExpiredRecordsToDeleteQueryHandler(LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        /// <inheritdoc />
        public IEnumerable<int> Handle(FindExpiredMessagesToDeleteQuery<int> query)
        {
            if (query.Cancellation.IsCancellationRequested)
            {
                return Enumerable.Empty<int>();
            }

            using (var db = _connectionInformation.GetDatabase())
            {
                //before executing a query, double check that we aren't stopping
                if (query.Cancellation.IsCancellationRequested)
                {
                    return Enumerable.Empty<int>();
                }

                var col = db.Database.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);

                var results = col.Query()
                    .Where(x => x.ExpirationTime < DateTime.UtcNow)
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
