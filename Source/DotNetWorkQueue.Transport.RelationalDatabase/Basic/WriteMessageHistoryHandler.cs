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
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Writes message history records for relational database transports.
    /// </summary>
    public class WriteMessageHistoryHandler : IWriteMessageHistory
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ITableNameHelper _tableNameHelper;
        private readonly IHistoryConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteMessageHistoryHandler"/> class.
        /// </summary>
        public WriteMessageHistoryHandler(IDbConnectionFactory connectionFactory,
            ITableNameHelper tableNameHelper,
            IHistoryConfiguration config)
        {
            _connectionFactory = connectionFactory;
            _tableNameHelper = tableNameHelper;
            _config = config;
        }

        /// <inheritdoc />
        public void RecordEnqueue(string queueId, string correlationId, string route, string messageType,
            byte[] body, byte[] headers)
        {
            if (!_config.Enabled) return;
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"INSERT INTO {_tableNameHelper.HistoryName}
                        (QueueID, CorrelationID, Status, EnqueuedUtc, RetryCount, Route, MessageType, Body, Headers)
                        VALUES (@QueueID, @CorrelationID, @Status, @EnqueuedUtc, 0, @Route, @MessageType, @Body, @Headers)";

                    AddParameter(command, "@QueueID", DbType.String, queueId);
                    AddParameter(command, "@CorrelationID", DbType.String, (object)correlationId ?? DBNull.Value);
                    AddParameter(command, "@Status", DbType.Int32, (int)MessageHistoryStatus.Enqueued);
                    AddParameter(command, "@EnqueuedUtc", DbType.DateTime, DateTime.UtcNow);
                    AddParameter(command, "@Route", DbType.String, (object)route ?? DBNull.Value);
                    AddParameter(command, "@MessageType", DbType.String, (object)messageType ?? DBNull.Value);
                    AddParameter(command, "@Body", DbType.Binary, _config.StoreBody ? (object)body ?? DBNull.Value : DBNull.Value);
                    AddParameter(command, "@Headers", DbType.Binary, _config.StoreBody ? (object)headers ?? DBNull.Value : DBNull.Value);

                    command.ExecuteNonQuery();
                }
            }
        }

        /// <inheritdoc />
        public void RecordProcessingStart(string queueId)
        {
            if (!_config.Enabled) return;
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"UPDATE {_tableNameHelper.HistoryName}
                        SET Status = @Status, StartedUtc = @StartedUtc
                        WHERE QueueID = @QueueID AND Status = @PrevStatus";

                    AddParameter(command, "@Status", DbType.Int32, (int)MessageHistoryStatus.Processing);
                    AddParameter(command, "@StartedUtc", DbType.DateTime, DateTime.UtcNow);
                    AddParameter(command, "@QueueID", DbType.String, queueId);
                    AddParameter(command, "@PrevStatus", DbType.Int32, (int)MessageHistoryStatus.Enqueued);

                    command.ExecuteNonQuery();
                }
            }
        }

        /// <inheritdoc />
        public void RecordComplete(string queueId)
        {
            if (!_config.Enabled) return;
            var now = DateTime.UtcNow;
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"UPDATE {_tableNameHelper.HistoryName}
                        SET Status = @Status, CompletedUtc = @CompletedUtc,
                            DurationMs = CASE WHEN StartedUtc IS NOT NULL THEN @DurationPlaceholder ELSE NULL END
                        WHERE QueueID = @QueueID AND Status = @PrevStatus";

                    AddParameter(command, "@Status", DbType.Int32, (int)MessageHistoryStatus.Complete);
                    AddParameter(command, "@CompletedUtc", DbType.DateTime, now);
                    AddParameter(command, "@DurationPlaceholder", DbType.Int64, 0L); // will be overridden below
                    AddParameter(command, "@QueueID", DbType.String, queueId);
                    AddParameter(command, "@PrevStatus", DbType.Int32, (int)MessageHistoryStatus.Processing);

                    // Calculate duration in a separate step for cross-db compatibility
                    command.CommandText = $@"UPDATE {_tableNameHelper.HistoryName}
                        SET Status = @Status, CompletedUtc = @CompletedUtc
                        WHERE QueueID = @QueueID AND Status = @PrevStatus";

                    command.ExecuteNonQuery();
                }

                // Update duration separately
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"UPDATE {_tableNameHelper.HistoryName}
                        SET DurationMs = @DurationMs
                        WHERE QueueID = @QueueID AND StartedUtc IS NOT NULL AND CompletedUtc IS NOT NULL AND DurationMs IS NULL";

                    // Read start time to calculate duration
                    var startTime = GetStartedUtc(connection, queueId);
                    var durationMs = startTime.HasValue ? (long)(now - startTime.Value).TotalMilliseconds : 0L;

                    AddParameter(command, "@DurationMs", DbType.Int64, durationMs);
                    AddParameter(command, "@QueueID", DbType.String, queueId);

                    command.ExecuteNonQuery();
                }
            }
        }

        /// <inheritdoc />
        public void RecordError(string queueId, string exception)
        {
            if (!_config.Enabled) return;
            var now = DateTime.UtcNow;
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();

                var startTime = GetStartedUtc(connection, queueId);
                var durationMs = startTime.HasValue ? (long)(now - startTime.Value).TotalMilliseconds : (long?)null;

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"UPDATE {_tableNameHelper.HistoryName}
                        SET Status = @Status, CompletedUtc = @CompletedUtc, DurationMs = @DurationMs, ExceptionText = @ExceptionText
                        WHERE QueueID = @QueueID AND (Status = @PrevStatus1 OR Status = @PrevStatus2)";

                    AddParameter(command, "@Status", DbType.Int32, (int)MessageHistoryStatus.Error);
                    AddParameter(command, "@CompletedUtc", DbType.DateTime, now);
                    AddParameter(command, "@DurationMs", DbType.Int64, (object)durationMs ?? DBNull.Value);
                    AddParameter(command, "@ExceptionText", DbType.String, (object)exception ?? DBNull.Value);
                    AddParameter(command, "@QueueID", DbType.String, queueId);
                    AddParameter(command, "@PrevStatus1", DbType.Int32, (int)MessageHistoryStatus.Processing);
                    AddParameter(command, "@PrevStatus2", DbType.Int32, (int)MessageHistoryStatus.Enqueued);

                    command.ExecuteNonQuery();
                }
            }
        }

        /// <inheritdoc />
        public void RecordRollback(string queueId)
        {
            if (!_config.Enabled) return;
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"UPDATE {_tableNameHelper.HistoryName}
                        SET Status = @Status, RetryCount = RetryCount + 1, StartedUtc = NULL, CompletedUtc = NULL, DurationMs = NULL
                        WHERE QueueID = @QueueID";

                    AddParameter(command, "@Status", DbType.Int32, (int)MessageHistoryStatus.Enqueued);
                    AddParameter(command, "@QueueID", DbType.String, queueId);

                    command.ExecuteNonQuery();
                }
            }
        }

        /// <inheritdoc />
        public void RecordDelete(string queueId)
        {
            if (!_config.Enabled) return;
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"UPDATE {_tableNameHelper.HistoryName}
                        SET Status = @Status, CompletedUtc = @CompletedUtc
                        WHERE QueueID = @QueueID";

                    AddParameter(command, "@Status", DbType.Int32, (int)MessageHistoryStatus.Deleted);
                    AddParameter(command, "@CompletedUtc", DbType.DateTime, DateTime.UtcNow);
                    AddParameter(command, "@QueueID", DbType.String, queueId);

                    command.ExecuteNonQuery();
                }
            }
        }

        /// <inheritdoc />
        public void RecordExpire(string queueId)
        {
            if (!_config.Enabled) return;
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"UPDATE {_tableNameHelper.HistoryName}
                        SET Status = @Status, CompletedUtc = @CompletedUtc
                        WHERE QueueID = @QueueID";

                    AddParameter(command, "@Status", DbType.Int32, (int)MessageHistoryStatus.Expired);
                    AddParameter(command, "@CompletedUtc", DbType.DateTime, DateTime.UtcNow);
                    AddParameter(command, "@QueueID", DbType.String, queueId);

                    command.ExecuteNonQuery();
                }
            }
        }

        private DateTime? GetStartedUtc(IDbConnection connection, string queueId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT StartedUtc FROM {_tableNameHelper.HistoryName} WHERE QueueID = @QueueID";
                AddParameter(command, "@QueueID", DbType.String, queueId);

                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return (DateTime)result;
                return null;
            }
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
