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


using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler
{
    /// <summary>
    /// Finds expired messages that should be removed from the queue.
    /// </summary>
    internal class FindExpiredRecordsToDeleteQueryHandler : IQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindExpiredRecordsToDeleteQueryHandler" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="options">The options.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public FindExpiredRecordsToDeleteQueryHandler(IConnectionInformation connectionInformation,
            IPostgreSqlMessageQueueTransportOptionsFactory options, 
            TableNameHelper tableNameHelper,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
            _options = new Lazy<PostgreSqlMessageQueueTransportOptions>(options.Create);
            _getTime = getTimeFactory.Create();
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public IEnumerable<long> Handle(FindExpiredMessagesToDeleteQuery query)
        {
            var npgsqlCommand = _options.Value.EnableStatus 
                ? 
                $"select queueid from {_tableNameHelper.MetaDataName} where status = {Convert.ToInt16(QueueStatuses.Waiting)} and @CurrentDate > ExpirationTime FOR UPDATE SKIP LOCKED" 
                : 
                $"select queueid from {_tableNameHelper.MetaDataName} where @CurrentDate > ExpirationTime FOR UPDATE SKIP LOCKED";

            using (var connection = new NpgsqlConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.Parameters.Add("@CurrentDate", NpgsqlDbType.Bigint);
                    command.Parameters["@CurrentDate"].Value = _getTime.GetCurrentUtcDate().Ticks;
                    command.CommandText = npgsqlCommand;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (query.Cancellation.IsCancellationRequested)
                            {
                                break;
                            }
                            yield return reader.GetInt64(0);
                        }
                    }
                }
            }
        }
    }
}
