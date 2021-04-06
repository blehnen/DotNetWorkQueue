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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Determines if a specific table exists in the schema
    /// </summary>
    internal class GetTableExistsQueryHandler : IQueryHandler<GetTableExistsQuery, bool>
    {
        private readonly IPrepareQueryHandler<GetTableExistsQuery, bool> _prepareQuery;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetTableExistsQueryHandler" /> class.
        /// </summary>
        /// <param name="prepareQuery">The prepare query.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        public GetTableExistsQueryHandler(IPrepareQueryHandler<GetTableExistsQuery, bool> prepareQuery,
            IDbConnectionFactory dbConnectionFactory)
        {
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            _prepareQuery = prepareQuery;
            _dbConnectionFactory = dbConnectionFactory;
        }

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public bool Handle(GetTableExistsQuery query)
        {
            using (var conn = _dbConnectionFactory.Create())
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetTableExists);
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }
    }
}
