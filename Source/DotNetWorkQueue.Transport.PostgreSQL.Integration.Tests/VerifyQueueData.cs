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
#region Using

using System.Data.SqlClient;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Npgsql;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests
{
    public class VerifyQueueData
    {
        private readonly PostgreSqlMessageQueueTransportOptions _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly SqlConnectionInformation _connection;

        public VerifyQueueData(string queueName, PostgreSqlMessageQueueTransportOptions options)
        {
            _options = options;
            _connection = new SqlConnectionInformation(queueName, ConnectionInfo.ConnectionString);
            _tableNameHelper = new TableNameHelper(_connection);
        }
        public void Verify(long expectedMessageCount)
        {
            VerifyCount(expectedMessageCount);

            if (_options.EnablePriority)
            {
                VerifyPriority();
            }

            if (_options.EnableDelayedProcessing)
            {
                VerifyDelayedProcessing();
            }

            if (_options.EnableMessageExpiration)
            {
                VerifyMessageExpiration();
            }

            if (_options.EnableStatus)
            {
                VerifyStatus();
            }

            if (_options.EnableStatusTable)
            {
                VerifyStatusTable();
            }
        }

        // ReSharper disable once UnusedParameter.Local
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyCount(long messageCount)
        {
            using (var conn = new NpgsqlConnection(_connection.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"select count(*) from {_tableNameHelper.MetaDataName}";
                    using (var reader = command.ExecuteReader())
                    {
                        Assert.True(reader.Read());
                        var records = reader.GetInt32(0);
                        Assert.Equal(messageCount, records);
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyPriority()
        {
            using (var conn = new NpgsqlConnection(_connection.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"select priority from {_tableNameHelper.MetaDataName}";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var priority = (int)reader[0];
                            Assert.Equal((byte)5, priority);
                        }
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyDelayedProcessing()
        {
            using (var conn = new NpgsqlConnection(_connection.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText =
                        $"select QueueProcessTime, QueuedDateTime from {_tableNameHelper.MetaDataName}";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Assert.NotEqual(reader.GetDateTime(1).Ticks, reader.GetInt64(0));
                        }
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyMessageExpiration()
        {
            using (var conn = new NpgsqlConnection(_connection.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"select ExpirationTime, QueuedDateTime from {_tableNameHelper.MetaDataName}";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Assert.NotEqual(reader.GetInt64(0), reader.GetDateTime(1).Ticks);
                        }
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyStatus()
        {
            using (var conn = new NpgsqlConnection(_connection.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"select status from {_tableNameHelper.MetaDataName}";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Assert.Equal(0, reader.GetInt32(0));
                        }
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyStatusTable()
        {
            using (var conn = new NpgsqlConnection(_connection.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"select status from {_tableNameHelper.StatusName}";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Assert.Equal(0, reader.GetInt32(0));
                        }
                    }
                }
            }
        }
    }

    public class VerifyQueueRecordCount
    {
        private readonly PostgreSqlMessageQueueTransportOptions _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly SqlConnectionInformation _connection;

        public VerifyQueueRecordCount(string queueName, PostgreSqlMessageQueueTransportOptions options)
        {
            _options = options;
            _connection = new SqlConnectionInformation(queueName, ConnectionInfo.ConnectionString);
            _tableNameHelper = new TableNameHelper(_connection);
        }

        public void Verify(int recordCount, bool ignoreMeta, bool ignoreErrorTracking)
        {
            AllTablesRecordCount(recordCount, ignoreMeta, ignoreErrorTracking);
        }

        // ReSharper disable once UnusedParameter.Local
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void AllTablesRecordCount(int recordCount, bool ignoreMeta, bool ignoreErrorTracking)
        {
            using (var conn = new NpgsqlConnection(_connection.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    if (!ignoreMeta)
                    {
                        command.CommandText = $"select count(*) from {_tableNameHelper.MetaDataName}";
                        using (var reader = command.ExecuteReader())
                        {
                            Assert.True(reader.Read());
                            var records = reader.GetInt32(0);
                            Assert.Equal(recordCount, records);
                        }
                    }

                    if (!ignoreErrorTracking)
                    {
                        command.CommandText = $"select count(*) from {_tableNameHelper.ErrorTrackingName}";
                        using (var reader = command.ExecuteReader())
                        {
                            Assert.True(reader.Read());
                            var records = reader.GetInt32(0);
                            Assert.Equal(recordCount, records);
                        }
                    }

                    command.CommandText = $"select count(*) from {_tableNameHelper.MetaDataErrorsName}";
                    using (var reader = command.ExecuteReader())
                    {
                        Assert.True(reader.Read());
                        var records = reader.GetInt32(0);
                        Assert.Equal(recordCount, records);
                    }

                    command.CommandText = $"select count(*) from {_tableNameHelper.QueueName}";
                    using (var reader = command.ExecuteReader())
                    {
                        Assert.True(reader.Read());
                        var records = reader.GetInt32(0);
                        Assert.Equal(recordCount, records);
                    }

                    if (_options.EnableStatusTable)
                    {
                        command.CommandText = $"select count(*) from {_tableNameHelper.StatusName}";
                        using (var reader = command.ExecuteReader())
                        {
                            Assert.True(reader.Read());
                            var records = reader.GetInt32(0);
                            Assert.Equal(recordCount, records);
                        }
                    }
                }
            }
        }
    }
    public class VerifyErrorCounts
    {
        private readonly TableNameHelper _tableNameHelper;
        private readonly SqlConnectionInformation _connection;

        public VerifyErrorCounts(string queueName)
        {
            _connection = new SqlConnectionInformation(queueName, ConnectionInfo.ConnectionString);
            _tableNameHelper = new TableNameHelper(_connection);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        public void Verify(long messageCount, int errorCount)
        {
            using (var conn = new NpgsqlConnection(_connection.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"select count(*) from {_tableNameHelper.MetaDataErrorsName}";
                    using (var reader = command.ExecuteReader())
                    {
                        Assert.True(reader.Read());
                        var records = reader.GetInt32(0);
                        Assert.Equal(messageCount, records);
                    }
                }

                //only check the two below tables if the error count is > 0.
                //error count of 0 means we are processing poison messages
                //poison messages go right into the error queue, without updating the tracking table
                if (errorCount > 0)
                {
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = $"select count(*) from {_tableNameHelper.ErrorTrackingName}";
                        using (var reader = command.ExecuteReader())
                        {
                            Assert.True(reader.Read());
                            var records = reader.GetInt32(0);
                            Assert.Equal(messageCount, records);
                        }
                    }

                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = $"select RetryCount from {_tableNameHelper.ErrorTrackingName}";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Assert.Equal(errorCount, reader.GetInt32(0));
                            }
                        }
                    }
                }
            }
        }
    }
}
