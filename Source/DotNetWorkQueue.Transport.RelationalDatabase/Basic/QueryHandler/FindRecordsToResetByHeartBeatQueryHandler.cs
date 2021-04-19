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
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Finds records that are outside of the heartbeat window.
    /// </summary>
    internal class FindRecordsToResetByHeartBeatQueryHandler<T>
        : IQueryHandler<FindMessagesToResetByHeartBeatQuery<T>, IEnumerable<MessageToReset<T>>>
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        private readonly IPrepareQueryHandler<FindMessagesToResetByHeartBeatQuery<T>, IEnumerable<MessageToReset<T>>>
            _prepareQuery;

        private readonly IReadColumn _readColumn;

        private readonly ICompositeSerialization _serialization;


        /// <summary>
        /// Initializes a new instance of the <see cref="FindRecordsToResetByHeartBeatQueryHandler{T}"/> class.
        /// </summary>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="prepareQuery">The prepare query.</param>
        /// <param name="readColumn">The read column.</param>
        /// <param name="serialization">The serialization.</param>
        public FindRecordsToResetByHeartBeatQueryHandler(
            IDbConnectionFactory dbConnectionFactory,
            IPrepareQueryHandler<FindMessagesToResetByHeartBeatQuery<T>, IEnumerable<MessageToReset<T>>> prepareQuery,
            IReadColumn readColumn, ICompositeSerialization serialization)
        {
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => readColumn, readColumn);
            Guard.NotNull(() => serialization, serialization);

            _dbConnectionFactory = dbConnectionFactory;
            _prepareQuery = prepareQuery;
            _readColumn = readColumn;
            _serialization = serialization;
        }
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public IEnumerable<MessageToReset<T>> Handle(FindMessagesToResetByHeartBeatQuery<T> query)
        {
            var results = new List<MessageToReset<T>>();

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

                            var headers = _readColumn.ReadAsByteArray(CommandStringTypes.GetHeartBeatExpiredMessageIds, 2,
                                reader);
                            if (headers != null)
                            {
                                var allheaders = _serialization.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(headers);
                                results.Add(new MessageToReset<T>(_readColumn.ReadAsType<T>(CommandStringTypes.GetHeartBeatExpiredMessageIds, 0, reader), _readColumn.ReadAsDateTime(CommandStringTypes.GetHeartBeatExpiredMessageIds, 1, reader), new ReadOnlyDictionary<string, object>(allheaders)));
                            }
                            else
                                results.Add(new MessageToReset<T>(_readColumn.ReadAsType<T>(CommandStringTypes.GetHeartBeatExpiredMessageIds, 0, reader), _readColumn.ReadAsDateTime(CommandStringTypes.GetHeartBeatExpiredMessageIds, 1, reader), null));
                        }
                    }
                }
            }
            return results;
        }
    }
}
