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
using System;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Gets the current UTC date from the server
    /// </summary>
    internal class GetUtcDateQueryHandler : IQueryHandler<GetUtcDateQuery, DateTime>
    {
        private readonly IPrepareQueryHandler<GetUtcDateQuery, DateTime> _prepareQuery;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IReadColumn _readColumn;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetUtcDateQueryHandler" /> class.
        /// </summary>
        /// <param name="prepareQuery">The prepare query.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="readColumn">The read column.</param>
        public GetUtcDateQueryHandler(IPrepareQueryHandler<GetUtcDateQuery, DateTime> prepareQuery,
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
        public DateTime Handle(GetUtcDateQuery query)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetUtcDate);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return _readColumn.ReadAsDateTime(CommandStringTypes.GetUtcDate, 0, reader);
                        }
                        throw new DotNetWorkQueueException("Failed to obtain the UTC date from the server");
                    }
                }
            }
        }
    }
}
