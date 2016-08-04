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
using System.Data.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    /// <summary>
    /// Determines if a specific table exists in the schema
    /// </summary>
    internal class GetTableExistsQueryHandler : IQueryHandler<GetTableExistsQuery, bool>
    {
        private readonly SqLiteCommandStringCache _commandCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetTableExistsQueryHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public GetTableExistsQueryHandler(SqLiteCommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public bool Handle(GetTableExistsQuery query)
        {
            if (!DatabaseExists.Exists(query.ConnectionString))
            {
                return false;
            }

            using (var conn = new SQLiteConnection(query.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = _commandCache.GetCommand(SqLiteCommandStringTypes.GetTableExists);
                    command.Parameters.AddWithValue("@Table", query.TableName);
                    using (var reader = command.ExecuteReader())
                    {
                        var result = reader.Read();
                        return result;
                    }
                }
            }
        }
    }
}
