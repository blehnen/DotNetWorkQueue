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
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    internal class GetDashboardErrorMessagesQueryHandlerAsync : IQueryHandlerAsync<GetDashboardErrorMessagesQuery, IReadOnlyList<DashboardErrorMessage>>
    {
        private readonly IPrepareQueryHandler<GetDashboardErrorMessagesQuery, IReadOnlyList<DashboardErrorMessage>> _prepareQuery;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IReadColumn _readColumn;

        public GetDashboardErrorMessagesQueryHandlerAsync(
            IDbConnectionFactory dbConnectionFactory,
            IPrepareQueryHandler<GetDashboardErrorMessagesQuery, IReadOnlyList<DashboardErrorMessage>> prepareQuery,
            IReadColumn readColumn)
        {
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => readColumn, readColumn);

            _prepareQuery = prepareQuery;
            _dbConnectionFactory = dbConnectionFactory;
            _readColumn = readColumn;
        }

        public async Task<IReadOnlyList<DashboardErrorMessage>> HandleAsync(GetDashboardErrorMessagesQuery query)
        {
            var results = new List<DashboardErrorMessage>();
            using (var connection = (DbConnection)_dbConnectionFactory.Create())
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetDashboardErrorMessages);
                    using (var reader = await ((DbCommand)command).ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            results.Add(ReadErrorMessage(reader));
                        }
                    }
                }
            }
            return results;
        }

        private DashboardErrorMessage ReadErrorMessage(IDataReader reader)
        {
            var message = new DashboardErrorMessage();
            var columnIndex = 0;

            message.Id = _readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorMessages, columnIndex++, reader);
            message.QueueId = _readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorMessages, columnIndex++, reader);
            message.LastException = _readColumn.ReadAsString(CommandStringTypes.GetDashboardErrorMessages, columnIndex++, reader);
            message.LastExceptionDate = _readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardErrorMessages, columnIndex++, reader);

            return message;
        }
    }
}
