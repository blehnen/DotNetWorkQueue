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
using System;

using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Query;
using DotNetWorkQueue.Validation;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler
{
    /// <summary>
    /// Gets the current UTC date from the server
    /// </summary>
    internal class GetUtcDateQueryHandler : IQueryHandler<GetUtcDateQuery, DateTime>
    {
        private readonly PostgreSqlCommandStringCache _commandCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetUtcDateQueryHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public GetUtcDateQueryHandler(PostgreSqlCommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Failed to obtain the UTC date from the server</exception>
        public DateTime Handle(GetUtcDateQuery query)
        {
            using (var connection = new NpgsqlConnection(query.ConnectionString))
            {
                connection.Open();
                using (var npgsqlCommand = new NpgsqlCommand(_commandCache.GetCommand(PostgreSqlCommandStringTypes.GetUtcDate), connection))
                {
                    using (var reader = npgsqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetDateTime(0);
                        }
                        throw new DotNetWorkQueueException("Failed to obtain the UTC date from the server");
                    }
                }
            }
        }
    }
}
