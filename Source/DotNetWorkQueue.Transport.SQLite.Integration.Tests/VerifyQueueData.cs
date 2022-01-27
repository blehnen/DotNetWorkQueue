﻿#region Using

using System;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests
{
    public class VerifyQueueData
    {
        private readonly SqLiteMessageQueueTransportOptions _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly SqliteConnectionInformation _connection;

        public VerifyQueueData(QueueConnection queueConnection, SqLiteMessageQueueTransportOptions options)
        {
            _options = options;
            _connection = new SqliteConnectionInformation(queueConnection, new DbDataSource());
            _tableNameHelper = new TableNameHelper(_connection);
        }
        public void Verify(long expectedMessageCount, string route, int orderId = 0)
        {
            VerifyCount(expectedMessageCount, route, orderId);

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

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyCount(long messageCount, string route, int orderId = 0)
        {
            using (var conn = new SQLiteConnection(_connection.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"select count(*) from {_tableNameHelper.MetaDataName}";
                    if (!string.IsNullOrEmpty(route))
                    {
                        command.CommandText += " where route = @route";
                        command.Parameters.AddWithValue("@Route", route);
                    }
                    else if (orderId > 0)
                    {
                        command.CommandText += " where OrderID = @OrderID";
                        command.Parameters.AddWithValue("@OrderID", orderId);
                    }
                    using (var reader = command.ExecuteReader())
                    {
                        Assert.True(reader.Read());
                        var records = reader.GetInt32(0);
                        Assert.Equal(messageCount, records);
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyPriority()
        {
            using (var conn = new SQLiteConnection(_connection.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"select priority from {_tableNameHelper.MetaDataName}";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var priority = Convert.ToInt32(reader[0]);
                            Assert.Equal(5, priority);
                        }
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyDelayedProcessing()
        {
            using (var conn = new SQLiteConnection(_connection.ConnectionString))
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
                            Assert.NotEqual(reader.GetInt64(1), reader.GetInt64(0));
                        }
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyMessageExpiration()
        {
            using (var conn = new SQLiteConnection(_connection.ConnectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"select ExpirationTime, QueuedDateTime from {_tableNameHelper.MetaDataName}";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Assert.NotEqual(reader.GetInt64(1), reader.GetInt64(0));
                        }
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyStatus()
        {
            using (var conn = new SQLiteConnection(_connection.ConnectionString))
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

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyStatusTable()
        {
            using (var conn = new SQLiteConnection(_connection.ConnectionString))
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
        private readonly SqLiteMessageQueueTransportOptions _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly SqliteConnectionInformation _connection;

        public VerifyQueueRecordCount(string queueName, string connectionString, SqLiteMessageQueueTransportOptions options)
        {
            _options = options;
            _connection = new SqliteConnectionInformation(new QueueConnection(queueName, connectionString), new DbDataSource());
            _tableNameHelper = new TableNameHelper(_connection);
        }

        public void Verify(int recordCount, bool ignoreMeta, bool ignoreErrorTracking)
        {
            AllTablesRecordCount(recordCount, ignoreMeta, ignoreErrorTracking);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query Ok")]
        private void AllTablesRecordCount(int recordCount, bool ignoreMeta, bool ignoreErrorTracking)
        {
            using (var conn = new SQLiteConnection(_connection.ConnectionString))
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
        private readonly SqliteConnectionInformation _connection;

        public VerifyErrorCounts(string queueName, string connectionString)
        {
            _connection = new SqliteConnectionInformation(new QueueConnection(queueName, connectionString), new DbDataSource());
            _tableNameHelper = new TableNameHelper(_connection);
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        public void Verify(long messageCount, int errorCount)
        {
            using (var conn = new SQLiteConnection(_connection.ConnectionString))
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
