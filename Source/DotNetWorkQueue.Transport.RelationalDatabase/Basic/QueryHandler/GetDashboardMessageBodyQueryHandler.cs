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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    internal class GetDashboardMessageBodyQueryHandler : IQueryHandler<GetDashboardMessageBodyQuery, DashboardMessageBody>
    {
        private readonly IPrepareQueryHandler<GetDashboardMessageBodyQuery, DashboardMessageBody> _prepareQuery;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IReadColumn _readColumn;

        public GetDashboardMessageBodyQueryHandler(
            IDbConnectionFactory dbConnectionFactory,
            IPrepareQueryHandler<GetDashboardMessageBodyQuery, DashboardMessageBody> prepareQuery,
            IReadColumn readColumn)
        {
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => readColumn, readColumn);

            _prepareQuery = prepareQuery;
            _dbConnectionFactory = dbConnectionFactory;
            _readColumn = readColumn;
        }

        public DashboardMessageBody Handle(GetDashboardMessageBodyQuery query)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetDashboardMessageBody);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new DashboardMessageBody
                            {
                                Body = _readColumn.ReadAsByteArray(CommandStringTypes.GetDashboardMessageBody, 0, reader),
                                Headers = _readColumn.ReadAsByteArray(CommandStringTypes.GetDashboardMessageBody, 1, reader)
                            };
                        }
                    }
                }
            }
            return null;
        }
    }
}
