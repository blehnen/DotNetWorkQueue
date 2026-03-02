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
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    internal class GetDashboardStatusCountsQueryHandlerAsync : IQueryHandlerAsync<GetDashboardStatusCountsQuery, DashboardStatusCounts>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        public GetDashboardStatusCountsQueryHandlerAsync(
            LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        public Task<DashboardStatusCounts> HandleAsync(GetDashboardStatusCountsQuery query)
        {
            using (var db = _connectionInformation.GetDatabase())
            {
                var meta = db.Database.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                var waiting = meta.LongCount(x => x.Status == QueueStatuses.Waiting);
                var processing = meta.LongCount(x => x.Status == QueueStatuses.Processing);

                var errorCol = db.Database.GetCollection<Schema.MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);
                var error = errorCol.LongCount();

                var total = waiting + processing + error;

                return Task.FromResult(new DashboardStatusCounts
                {
                    Waiting = waiting,
                    Processing = processing,
                    Error = error,
                    Total = total
                });
            }
        }
    }
}
