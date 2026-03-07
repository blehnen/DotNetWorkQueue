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
    internal class GetDashboardMessageHeadersQueryHandlerAsync : IQueryHandlerAsync<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        public GetDashboardMessageHeadersQueryHandlerAsync(
            LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        public Task<DashboardMessageHeaders> HandleAsync(GetDashboardMessageHeadersQuery query)
        {
            var id = int.Parse(query.MessageId);

            using (var db = _connectionInformation.GetDatabase())
            {
                var col = db.Database.GetCollection<Schema.QueueTable>(_tableNameHelper.QueueName);
                var record = col.FindById(id);

                if (record == null)
                    return Task.FromResult<DashboardMessageHeaders>(null);

                return Task.FromResult(new DashboardMessageHeaders
                {
                    Headers = record.Headers
                });
            }
        }
    }
}
