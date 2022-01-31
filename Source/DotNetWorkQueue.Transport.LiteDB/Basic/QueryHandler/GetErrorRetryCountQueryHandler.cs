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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    /// <summary>
    /// Returns the current retry count for a message and a specific exception type
    /// </summary>
    internal class GetErrorRetryCountQueryHandler : IQueryHandler<GetErrorRetryCountQuery<int>, int>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetErrorRetryCountQueryHandler"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public GetErrorRetryCountQueryHandler(LiteDbConnectionManager connectionInformation,
        TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        /// <inheritdoc />
        public int Handle(GetErrorRetryCountQuery<int> query)
        {
            using (var db = _connectionInformation.GetDatabase())
            {
                var col = db.Database.GetCollection<Schema.ErrorTrackingTable>(_tableNameHelper.ErrorTrackingName);

                var results = col.Query()
                    .Where(x => x.QueueId.Equals(query.QueueId))
                    .Where(x => x.ExceptionType == query.ExceptionType)
                    .Limit(1)
                    .ToList();

                if (results != null && results.Count == 1)
                {
                    var record = results[0];
                    return record.RetryCount;
                }
            }

            return 0;
        }
    }
}
