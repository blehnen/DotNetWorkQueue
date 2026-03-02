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
using System.Data.Common;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    internal class GetDashboardMessageHeadersQueryHandlerAsync : IQueryHandlerAsync<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>
    {
        private readonly IPrepareQueryHandler<GetDashboardMessageHeadersQuery, DashboardMessageHeaders> _prepareQuery;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IReadColumn _readColumn;

        public GetDashboardMessageHeadersQueryHandlerAsync(
            IDbConnectionFactory dbConnectionFactory,
            IPrepareQueryHandler<GetDashboardMessageHeadersQuery, DashboardMessageHeaders> prepareQuery,
            IReadColumn readColumn)
        {
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => readColumn, readColumn);

            _prepareQuery = prepareQuery;
            _dbConnectionFactory = dbConnectionFactory;
            _readColumn = readColumn;
        }

        public async Task<DashboardMessageHeaders> HandleAsync(GetDashboardMessageHeadersQuery query)
        {
            using (var connection = (DbConnection)_dbConnectionFactory.Create())
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetDashboardMessageHeaders);
                    using (var reader = await ((DbCommand)command).ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            return new DashboardMessageHeaders
                            {
                                Headers = _readColumn.ReadAsByteArray(CommandStringTypes.GetDashboardMessageHeaders, 0, reader)
                            };
                        }
                    }
                }
            }
            return null;
        }
    }
}
