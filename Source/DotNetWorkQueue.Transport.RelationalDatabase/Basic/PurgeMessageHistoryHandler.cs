// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Data;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Purges old message history records for relational database transports.
    /// </summary>
    public class PurgeMessageHistoryHandler : IPurgeMessageHistory
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ITableNameHelper _tableNameHelper;
        private readonly IHistoryConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="PurgeMessageHistoryHandler"/> class.
        /// </summary>
        public PurgeMessageHistoryHandler(IDbConnectionFactory connectionFactory,
            ITableNameHelper tableNameHelper,
            IHistoryConfiguration config)
        {
            _connectionFactory = connectionFactory;
            _tableNameHelper = tableNameHelper;
            _config = config;
        }

        /// <inheritdoc />
        public long Purge(DateTime olderThan)
        {
            if (!_config.Enabled) return 0;

            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    // Delete records where the completed date is older than the cutoff,
                    // or if never completed, where the enqueued date is older
                    command.CommandText = $@"DELETE FROM {_tableNameHelper.HistoryName}
                        WHERE (CompletedUtc IS NOT NULL AND CompletedUtc < @OlderThan)
                           OR (CompletedUtc IS NULL AND EnqueuedUtc < @OlderThan)";

                    var param = command.CreateParameter();
                    param.ParameterName = "@OlderThan";
                    param.DbType = DbType.DateTime;
                    param.Value = olderThan;
                    command.Parameters.Add(param);

                    return command.ExecuteNonQuery();
                }
            }
        }
    }
}
