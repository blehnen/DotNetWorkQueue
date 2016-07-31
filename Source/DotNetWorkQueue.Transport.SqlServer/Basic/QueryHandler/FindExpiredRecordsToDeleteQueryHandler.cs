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
using System.Data.SqlClient;
using DotNetWorkQueue.Transport.SqlServer.Basic.Query;
namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler
{
    /// <summary>
    /// Finds expired messages that should be removed from the queue.
    /// </summary>
    internal class FindExpiredRecordsToDeleteQueryHandler : IQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindExpiredRecordsToDeleteQueryHandler" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="options">The options.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public FindExpiredRecordsToDeleteQueryHandler(IConnectionInformation connectionInformation,
            ISqlServerMessageQueueTransportOptionsFactory options, 
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
            _options = new Lazy<SqlServerMessageQueueTransportOptions>(options.Create);
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public IEnumerable<long> Handle(FindExpiredMessagesToDeleteQuery query)
        {
            var sqlCommand = _options.Value.EnableStatus 
                ? 
                $"select queueid from {_tableNameHelper.MetaDataName} with (updlock, readpast, rowlock) where status = {Convert.ToInt16(QueueStatuses.Waiting)} and GETUTCDate() > ExpirationTime" 
                : 
                $"select queueid from {_tableNameHelper.MetaDataName} with (updlock, readpast, rowlock) where GETUTCDate() > ExpirationTime";

            using (var connection = new SqlConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlCommand;
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
