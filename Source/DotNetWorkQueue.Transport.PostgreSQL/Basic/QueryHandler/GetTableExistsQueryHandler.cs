// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Query;
using DotNetWorkQueue.Validation;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler
{
    /// <summary>
    /// Determines if a specific table exists in the schema
    /// </summary>
    internal class GetTableExistsQueryHandler : IQueryHandler<GetTableExistsQuery, bool>
    {
        private readonly PostgreSqlCommandStringCache _commandCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetTableExistsQueryHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public GetTableExistsQueryHandler(PostgreSqlCommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public bool Handle(GetTableExistsQuery query)
        {
            using (var conn = new NpgsqlConnection(query.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = _commandCache.GetCommand(PostgreSqlCommandStringTypes.GetTableExists);
                    command.Parameters.AddWithValue("@Database", new NpgsqlConnectionStringBuilder(query.ConnectionString).Database);
                    command.Parameters.AddWithValue("@Table", query.TableName.ToLowerInvariant());
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }
    }
}
