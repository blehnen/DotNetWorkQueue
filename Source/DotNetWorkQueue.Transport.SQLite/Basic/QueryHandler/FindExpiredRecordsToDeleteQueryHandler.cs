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
using System.Collections.Generic;
using System.Data.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic.Query;
using System.Linq;
using System.Data;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    /// <summary>
    /// Finds expired messages that should be removed from the queue.
    /// </summary>
    internal class FindExpiredRecordsToDeleteQueryHandler : IQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindExpiredRecordsToDeleteQueryHandler" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="options">The options.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public FindExpiredRecordsToDeleteQueryHandler(IConnectionInformation connectionInformation,
            ISqLiteMessageQueueTransportOptionsFactory options, 
            TableNameHelper tableNameHelper,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
            _options = new Lazy<SqLiteMessageQueueTransportOptions>(options.Create);
            _getTime = getTimeFactory.Create();
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public IEnumerable<long> Handle(FindExpiredMessagesToDeleteQuery query)
        {
            if (!DatabaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return Enumerable.Empty<long>();
            }

            if (query.Cancellation.IsCancellationRequested)
            {
                return Enumerable.Empty<long>();
            }

            var results = new List<long>();
            var sqLiteCommand = _options.Value.EnableStatus 
                ? 
                $"select queueid from {_tableNameHelper.MetaDataName} where status = {Convert.ToInt16(QueueStatuses.Waiting)} and @CurrentDateTime > ExpirationTime" 
                : 
                $"select queueid from {_tableNameHelper.MetaDataName} where @CurrentDateTime > ExpirationTime";

            using (var connection = new SQLiteConnection(_connectionInformation.ConnectionString))
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
                    command.CommandText = sqLiteCommand;
                    command.Parameters.Add("@CurrentDateTime", DbType.Int64);
                    command.Parameters["@CurrentDateTime"].Value = _getTime.GetCurrentUtcDate().Ticks;
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
