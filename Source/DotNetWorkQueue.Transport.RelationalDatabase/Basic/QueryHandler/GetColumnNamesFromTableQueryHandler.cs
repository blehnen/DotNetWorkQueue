// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Returns all standard column names for a specific table.
    /// </summary>
    internal class GetColumnNamesFromTableQueryHandler : IQueryHandler<GetColumnNamesFromTableQuery, List<string>>
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IReadColumn _readColumn;
        private readonly IPrepareQueryHandler<GetColumnNamesFromTableQuery, List<string>> _prepareQuery;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetColumnNamesFromTableQueryHandler" /> class.
        /// </summary>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="readColumn">The read column.</param>
        /// <param name="prepareQuery">The prepare query.</param>
        public GetColumnNamesFromTableQueryHandler(IDbConnectionFactory dbConnectionFactory,
            IReadColumn readColumn,
            IPrepareQueryHandler<GetColumnNamesFromTableQuery, List<string>> prepareQuery)
        {
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => readColumn, readColumn);
            Guard.NotNull(() => prepareQuery, prepareQuery);

            _dbConnectionFactory = dbConnectionFactory;
            _readColumn = readColumn;
            _prepareQuery = prepareQuery;
        }
        /// <inheritdoc />
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public List<string> Handle(GetColumnNamesFromTableQuery query)
        {
            var columns = new List<string>();
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetColumnNamesFromTable); 
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columns.Add(_readColumn.ReadAsString(CommandStringTypes.GetColumnNamesFromTable, 0, reader));
                        }
                    }
                }
            }
            return columns;
        }
    }
}
