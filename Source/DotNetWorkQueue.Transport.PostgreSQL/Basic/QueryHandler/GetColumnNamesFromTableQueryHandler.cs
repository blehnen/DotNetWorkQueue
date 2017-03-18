// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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

using DotNetWorkQueue.Transport.PostgreSQL.Basic.Query;
using DotNetWorkQueue.Validation;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler
{
    /// <summary>
    /// Returns all standard column names for a specific table.
    /// </summary>
    internal class GetColumnNamesFromTableQueryHandler : IQueryHandler<GetColumnNamesFromTableQuery, List<string>>
    {
        private readonly PostgreSqlCommandStringCache _commandCache;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetColumnNamesFromTableQueryHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public GetColumnNamesFromTableQueryHandler(PostgreSqlCommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public List<string> Handle(GetColumnNamesFromTableQuery query)
        {
            var columns = new List<string>();
            using (var connection = new NpgsqlConnection(query.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = _commandCache.GetCommand(PostgreSqlCommandStringTypes.GetColumnNamesFromTable);
                    command.Parameters.AddWithValue("@TableName", query.TableName.ToLowerInvariant());
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columns.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return columns;
        }
    }
}
