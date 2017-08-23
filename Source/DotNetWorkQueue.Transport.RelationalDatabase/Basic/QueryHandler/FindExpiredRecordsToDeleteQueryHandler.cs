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
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <summary>
    /// Finds expired messages that should be removed from the queue.
    /// </summary>
    internal class FindExpiredRecordsToDeleteQueryHandler : IQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>>
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IPrepareQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>> _prepareQuery;
        private readonly Lazy<ITransportOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindExpiredRecordsToDeleteQueryHandler" /> class.
        /// </summary>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="options">The options.</param>
        /// <param name="prepareQuery">The prepare query.</param>
        public FindExpiredRecordsToDeleteQueryHandler(
            IDbConnectionFactory dbConnectionFactory,
            ITransportOptionsFactory options, 
            IPrepareQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>> prepareQuery)
        {
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => prepareQuery, prepareQuery);

            _dbConnectionFactory = dbConnectionFactory;
            _prepareQuery = prepareQuery;
            _options = new Lazy<ITransportOptions>(options.Create);
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public IEnumerable<long> Handle(FindExpiredMessagesToDeleteQuery query)
        {
            if (query.Cancellation.IsCancellationRequested)
            {
                return Enumerable.Empty<long>();
            }

            var results = new List<long>();
            var commandType = _options.Value.EnableStatus
                ? CommandStringTypes.FindExpiredRecordsWithStatusToDelete
                : CommandStringTypes.FindExpiredRecordsToDelete;
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();

                //before executing a query, double check that we aren't stopping
                //otherwise, there is a chance that the tables no longer exist in memory mode
                if (query.Cancellation.IsCancellationRequested)
                {
                    return Enumerable.Empty<long>();
                }

                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, commandType);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (query.Cancellation.IsCancellationRequested)
                            {
                                break;
                            }
                            results.Add(reader.GetInt64(0));
                        }
                    }
                }
            }
            return results;
        }
    }
}
