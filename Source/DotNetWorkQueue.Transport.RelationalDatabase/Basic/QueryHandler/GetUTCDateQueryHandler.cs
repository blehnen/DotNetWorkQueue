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
using System;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <summary>
    /// Gets the current UTC date from the server
    /// </summary>
    internal class GetUtcDateQueryHandler : IQueryHandler<GetUtcDateQuery, DateTime>
    {
        private readonly IPrepareQueryHandler<GetUtcDateQuery, DateTime> _prepareQuery;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetUtcDateQueryHandler" /> class.
        /// </summary>
        /// <param name="prepareQuery">The prepare query.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        public GetUtcDateQueryHandler(IPrepareQueryHandler<GetUtcDateQuery, DateTime> prepareQuery,
            IDbConnectionFactory dbConnectionFactory)
        {
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            _prepareQuery = prepareQuery;
            _dbConnectionFactory = dbConnectionFactory;
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Failed to obtain the UTC date from the server</exception>
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
                            return reader.GetDateTime(0);
                        }
                        throw new DotNetWorkQueueException("Failed to obtain the UTC date from the server");
                    }
                }
            }
        }
    }
}
