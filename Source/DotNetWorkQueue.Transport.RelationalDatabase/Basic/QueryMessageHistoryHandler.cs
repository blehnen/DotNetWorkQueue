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
using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Queries message history records for relational database transports.
    /// </summary>
    public class QueryMessageHistoryHandler : IQueryMessageHistory
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ITableNameHelper _tableNameHelper;
        private readonly IHistoryConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryMessageHistoryHandler"/> class.
        /// </summary>
        public QueryMessageHistoryHandler(IDbConnectionFactory connectionFactory,
            ITableNameHelper tableNameHelper,
            IHistoryConfiguration config)
        {
            _connectionFactory = connectionFactory;
            _tableNameHelper = tableNameHelper;
            _config = config;
        }

        /// <inheritdoc />
        public IReadOnlyList<MessageHistoryRecord> Get(int pageIndex, int pageSize, MessageHistoryStatus? statusFilter)
        {
            if (!_config.Enabled) return new List<MessageHistoryRecord>();

            var results = new List<MessageHistoryRecord>();
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    var where = statusFilter.HasValue ? "WHERE Status = @Status" : "";
                    command.CommandText = $@"SELECT QueueID, CorrelationID, Status, EnqueuedUtc, StartedUtc, CompletedUtc,
                        DurationMs, ExceptionText, RetryCount, Route, MessageType
                        FROM {_tableNameHelper.HistoryName} {where}
                        ORDER BY EnqueuedUtc DESC";

                    if (statusFilter.HasValue)
                        AddParameter(command, "@Status", DbType.Int32, (int)statusFilter.Value);

                    var skip = pageIndex * pageSize;
                    var count = 0;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (count >= skip && results.Count < pageSize)
                                results.Add(MapRecord(reader));
                            count++;
                            if (results.Count >= pageSize)
                                break;
                        }
                    }
                }
            }
            return results;
        }

        /// <inheritdoc />
        public MessageHistoryRecord GetByQueueId(string queueId)
        {
            if (!_config.Enabled) return null;

            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"SELECT QueueID, CorrelationID, Status, EnqueuedUtc, StartedUtc, CompletedUtc,
                        DurationMs, ExceptionText, RetryCount, Route, MessageType
                        FROM {_tableNameHelper.HistoryName}
                        WHERE QueueID = @QueueID";

                    AddParameter(command, "@QueueID", DbType.String, queueId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapRecord(reader);
                    }
                }
            }
            return null;
        }

        /// <inheritdoc />
        public long GetCount(MessageHistoryStatus? statusFilter)
        {
            if (!_config.Enabled) return 0;

            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    var where = statusFilter.HasValue ? "WHERE Status = @Status" : "";
                    command.CommandText = $"SELECT COUNT(*) FROM {_tableNameHelper.HistoryName} {where}";

                    if (statusFilter.HasValue)
                        AddParameter(command, "@Status", DbType.Int32, (int)statusFilter.Value);

                    return Convert.ToInt64(command.ExecuteScalar());
                }
            }
        }

        private static MessageHistoryRecord MapRecord(IDataReader reader)
        {
            return new MessageHistoryRecord
            {
                QueueId = reader.GetString(0),
                CorrelationId = reader.IsDBNull(1) ? null : reader.GetString(1),
                Status = (MessageHistoryStatus)reader.GetInt32(2),
                EnqueuedUtc = reader.GetDateTime(3),
                StartedUtc = reader.IsDBNull(4) ? null : (DateTime?)reader.GetDateTime(4),
                CompletedUtc = reader.IsDBNull(5) ? null : (DateTime?)reader.GetDateTime(5),
                DurationMs = reader.IsDBNull(6) ? null : (long?)reader.GetInt64(6),
                ExceptionText = reader.IsDBNull(7) ? null : reader.GetString(7),
                RetryCount = reader.GetInt32(8),
                Route = reader.IsDBNull(9) ? null : reader.GetString(9),
                MessageType = reader.IsDBNull(10) ? null : reader.GetString(10)
            };
        }

        private static void AddParameter(IDbCommand command, string name, DbType dbType, object value)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.DbType = dbType;
            param.Value = value;
            command.Parameters.Add(param);
        }
    }
}
