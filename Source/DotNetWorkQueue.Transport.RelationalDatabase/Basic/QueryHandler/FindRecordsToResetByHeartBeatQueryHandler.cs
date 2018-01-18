// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Finds records that are outside of the heartbeat window.
    /// </summary>
    internal class FindRecordsToResetByHeartBeatQueryHandler
        : IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>>
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        private readonly IPrepareQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>>
            _prepareQuery;

        private readonly IReadColumn _readColumn;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindRecordsToResetByHeartBeatQueryHandler" /> class.
        /// </summary>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="prepareQuery">The setup command.</param>
        /// <param name="readColumn">The read column.</param>
        public FindRecordsToResetByHeartBeatQueryHandler(
            IDbConnectionFactory dbConnectionFactory,
            IPrepareQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>> prepareQuery,
            IReadColumn readColumn)
        {
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => readColumn, readColumn);

            _dbConnectionFactory = dbConnectionFactory;
            _prepareQuery = prepareQuery;
            _readColumn = readColumn;
        }
        /// <inheritdoc />
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public IEnumerable<MessageToReset> Handle(FindMessagesToResetByHeartBeatQuery query)
        {
            var results = new List<MessageToReset>();

            if (query.Cancellation.IsCancellationRequested)
            {
                return results;
            }

            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();

                //before executing a query, double check that we aren't stopping
                //otherwise, there is a chance that the tables no longer exist in memory mode
                if (query.Cancellation.IsCancellationRequested)
                {
                    return results;
                }

                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetHeartBeatExpiredMessageIds);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (query.Cancellation.IsCancellationRequested)
                            {
                                break;
                            }
                            results.Add(new MessageToReset(_readColumn.ReadAsInt64(CommandStringTypes.GetHeartBeatExpiredMessageIds, 0, reader), _readColumn.ReadAsDateTime(CommandStringTypes.GetHeartBeatExpiredMessageIds, 1, reader)));
                        }
                    }
                }
            }
            return results;
        }
    }
}
