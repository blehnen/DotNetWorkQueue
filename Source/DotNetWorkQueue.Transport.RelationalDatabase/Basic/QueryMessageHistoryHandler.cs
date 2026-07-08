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
using System.Data.Common;
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Queries message history records for relational database transports.
    /// </summary>
    /// <remarks>
    /// Read methods do not check <see cref="IBaseTransportOptions.EnableHistory"/>. That flag gates
    /// history WRITES only; reads should succeed whenever the history table exists and has data,
    /// regardless of the current dashboard container's cached options. If the history table does
    /// not exist (queue was never created with history enabled), reads gracefully return empty.
    /// </remarks>
    public class QueryMessageHistoryHandler : IQueryMessageHistory
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ITableNameHelper _tableNameHelper;
        private readonly IDbPaginationSyntax _paginationSyntax;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryMessageHistoryHandler"/> class.
        /// </summary>
        public QueryMessageHistoryHandler(IDbConnectionFactory connectionFactory,
            ITableNameHelper tableNameHelper,
            IDbPaginationSyntax paginationSyntax)
        {
            _connectionFactory = connectionFactory;
            _tableNameHelper = tableNameHelper;
            _paginationSyntax = paginationSyntax;
        }

        /// <inheritdoc />
        public IReadOnlyList<MessageHistoryRecord> Get(int pageIndex, int pageSize, MessageHistoryStatus? statusFilter)
        {
            var results = new List<MessageHistoryRecord>();
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        var where = statusFilter.HasValue ? "WHERE Status = @Status" : "";
                        var pagination = _paginationSyntax.BuildPaginationClause("@Offset", "@PageSize");
                        command.CommandText = $@"SELECT QueueID, CorrelationID, Status, EnqueuedUtc, StartedUtc, CompletedUtc,
                            DurationMs, ExceptionText, RetryCount, Route, MessageType
                            FROM {_tableNameHelper.HistoryName} {where}
                            ORDER BY EnqueuedUtc DESC
                            {pagination}";

                        if (statusFilter.HasValue)
                            AddParameter(command, "@Status", DbType.Int32, (int)statusFilter.Value);

                        AddParameter(command, "@Offset", DbType.Int32, pageIndex * pageSize);
                        AddParameter(command, "@PageSize", DbType.Int32, pageSize);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                                results.Add(MapRecord(reader));
                        }
                    }
                }
            }
            catch (DbException)
            {
                // History table does not exist (queue was never created with history enabled).
                // Return empty rather than surfacing a 500 to the dashboard UI.
            }
            return results;
        }

        /// <inheritdoc />
        public MessageHistoryRecord GetByQueueId(string queueId)
        {
            try
            {
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
            }
            catch (DbException)
            {
                // History table does not exist; treat as not-found.
            }
            return null;
        }

        /// <inheritdoc />
        public long GetCount(MessageHistoryStatus? statusFilter)
        {
            try
            {
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
            catch (DbException)
            {
                return 0;
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
