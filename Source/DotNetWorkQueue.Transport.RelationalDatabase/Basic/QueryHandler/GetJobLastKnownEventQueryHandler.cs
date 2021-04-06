// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <inheritdoc />
    public class GetJobLastKnownEventQueryHandler : IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>
    {
        private readonly IPrepareQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset> _prepareQuery;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IReadColumn _readColumn;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetJobLastKnownEventQueryHandler" /> class.
        /// </summary>
        /// <param name="prepareQuery">The prepare query.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="readColumn">The read column.</param>
        public GetJobLastKnownEventQueryHandler(IPrepareQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset> prepareQuery,
            IDbConnectionFactory dbConnectionFactory,
            IReadColumn readColumn)
        {
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => readColumn, readColumn);

            _prepareQuery = prepareQuery;
            _dbConnectionFactory = dbConnectionFactory;
            _readColumn = readColumn;
        }
        /// <inheritdoc />
        public DateTimeOffset Handle(GetJobLastKnownEventQuery query)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetJobLastKnownEvent);
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read() ? _readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetJobLastKnownEvent, 0, reader) : default(DateTimeOffset);
                    }
                }
            }
        }
    }
}
