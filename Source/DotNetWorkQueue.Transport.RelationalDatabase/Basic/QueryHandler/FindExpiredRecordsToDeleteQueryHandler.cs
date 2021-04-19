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
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Finds expired messages that should be removed from the queue.
    /// </summary>
    internal class FindExpiredRecordsToDeleteQueryHandler<T> : IQueryHandler<FindExpiredMessagesToDeleteQuery<T>, IEnumerable<T>>
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IReadColumn _readColumn;
        private readonly IPrepareQueryHandler<FindExpiredMessagesToDeleteQuery<T>, IEnumerable<T>> _prepareQuery;
        private readonly Lazy<ITransportOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindExpiredRecordsToDeleteQueryHandler{T}" /> class.
        /// </summary>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="options">The options.</param>
        /// <param name="readColumn">The read column.</param>
        /// <param name="prepareQuery">The prepare query.</param>
        public FindExpiredRecordsToDeleteQueryHandler(
            IDbConnectionFactory dbConnectionFactory,
            ITransportOptionsFactory options, 
            IReadColumn readColumn,
            IPrepareQueryHandler<FindExpiredMessagesToDeleteQuery<T>, IEnumerable<T>> prepareQuery)
        {
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => readColumn, readColumn);

            _dbConnectionFactory = dbConnectionFactory;
            _readColumn = readColumn;
            _prepareQuery = prepareQuery;
            _options = new Lazy<ITransportOptions>(options.Create);
        }
        /// <inheritdoc />
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public IEnumerable<T> Handle(FindExpiredMessagesToDeleteQuery<T> query)
        {
            if (query.Cancellation.IsCancellationRequested)
            {
                return Enumerable.Empty<T>();
            }

            var results = new List<T>();
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
                    return Enumerable.Empty<T>();
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
                            results.Add(_readColumn.ReadAsType<T>(commandType, 0, reader));
                        }
                    }
                }
            }
            return results;
        }
    }
}
