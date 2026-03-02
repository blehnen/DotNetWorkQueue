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
using System;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    internal class GetDashboardMessageDetailQueryHandler : IQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage>
    {
        private readonly IPrepareQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage> _prepareQuery;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IReadColumn _readColumn;
        private readonly Lazy<ITransportOptions> _options;

        public GetDashboardMessageDetailQueryHandler(
            IDbConnectionFactory dbConnectionFactory,
            IPrepareQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage> prepareQuery,
            IReadColumn readColumn,
            ITransportOptionsFactory options)
        {
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => readColumn, readColumn);
            Guard.NotNull(() => options, options);

            _prepareQuery = prepareQuery;
            _dbConnectionFactory = dbConnectionFactory;
            _readColumn = readColumn;
            _options = new Lazy<ITransportOptions>(options.Create);
        }

        public DashboardMessage Handle(GetDashboardMessageDetailQuery query)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetDashboardMessageDetail);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return DashboardMessageReader.ReadMessage(reader, _readColumn,
                                CommandStringTypes.GetDashboardMessageDetail, _options.Value);
                        }
                    }
                }
            }
            return null;
        }
    }
}
