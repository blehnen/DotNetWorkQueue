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
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    internal class GetDashboardErrorRetriesQueryHandlerAsync : IQueryHandlerAsync<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        public GetDashboardErrorRetriesQueryHandlerAsync(
            LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        public Task<IReadOnlyList<DashboardErrorRetry>> HandleAsync(GetDashboardErrorRetriesQuery query)
        {
            var id = int.Parse(query.MessageId);

            using (var db = _connectionInformation.GetDatabase())
            {
                var errorTrack = db.Database.GetCollection<Schema.ErrorTrackingTable>(_tableNameHelper.ErrorTrackingName);
                var records = errorTrack.Query()
                    .Where(x => x.QueueId == id)
                    .ToList();

                var results = new List<DashboardErrorRetry>(records.Count);
                foreach (var record in records)
                {
                    results.Add(new DashboardErrorRetry
                    {
                        ErrorTrackingId = record.Id,
                        QueueId = record.QueueId.ToString(),
                        ExceptionType = record.ExceptionType,
                        RetryCount = record.RetryCount
                    });
                }

                return Task.FromResult<IReadOnlyList<DashboardErrorRetry>>(results);
            }
        }
    }
}
