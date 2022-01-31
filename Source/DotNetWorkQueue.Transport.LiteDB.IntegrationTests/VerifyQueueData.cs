#region Using

using System;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using LiteDB;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests
{
    public class VerifyQueueData
    {
        private readonly LiteDbMessageQueueTransportOptions _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly LiteDbConnectionInformation _connection;
        private readonly ICreationScope _scope;

        public VerifyQueueData(QueueConnection queueConnection, LiteDbMessageQueueTransportOptions options, ICreationScope scope)
        {
            _options = options;
            _connection = new LiteDbConnectionInformation(queueConnection);
            _tableNameHelper = new TableNameHelper(_connection);
            _scope = scope;
        }
        public void Verify(long expectedMessageCount, string route)
        {
            VerifyCount(expectedMessageCount, route);

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
                VerifyStatusTable(expectedMessageCount, route);
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities",
            Justification = "Query OK")]
        private void VerifyCount(long messageCount, string route)
        {
            var connScope = _scope.GetDisposable<LiteDbConnectionManager>();
            if (connScope == null)
            {
                using (var conn = new LiteDatabase(_connection.ConnectionString))
                {
                    if (string.IsNullOrEmpty(route))
                    {
                        var col = conn.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                        Assert.Equal(messageCount, col.Count());
                    }
                    else
                    {
                        var col = conn.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                        var results = col.Query()
                            .Where(x => x.Route.Equals(route))
                            .ToList();
                        Assert.Equal(messageCount, results.Count);
                    }
                }
            }
            else
            {

            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyDelayedProcessing()
        {
            var connScope = _scope.GetDisposable<LiteDbConnectionManager>();
            if (connScope == null)
            {
                using (var conn = new LiteDatabase(_connection.ConnectionString))
                {

                    var col = conn.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                    var results = col.Query()
                        .ToList();
                    foreach (var record in results)
                    {
                        Assert.NotEqual(record.QueueProcessTime, record.QueuedDateTime);
                    }
                }
            }
            else
            {
                using (var conn = connScope.GetDatabase())
                {

                    var col = conn.Database.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                    var results = col.Query()
                        .ToList();
                    foreach (var record in results)
                    {
                        Assert.NotEqual(record.QueueProcessTime, record.QueuedDateTime);
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyMessageExpiration()
        {
            var connScope = _scope.GetDisposable<LiteDbConnectionManager>();
            if (connScope == null)
            {
                using (var conn = new LiteDatabase(_connection.ConnectionString))
                {
                    var col = conn.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                    var results = col.Query()
                        .ToList();
                    foreach (var record in results)
                    {
                        Assert.NotEqual(record.ExpirationTime, record.QueuedDateTime);
                    }
                }
            }
            else
            {
                using (var conn = connScope.GetDatabase())
                {
                    var col = conn.Database.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                    var results = col.Query()
                        .ToList();
                    foreach (var record in results)
                    {
                        Assert.NotEqual(record.ExpirationTime, record.QueuedDateTime);
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void VerifyStatus()
        {
            var connScope = _scope.GetDisposable<LiteDbConnectionManager>();
            if (connScope == null)
            {
                using (var conn = new LiteDatabase(_connection.ConnectionString))
                {
                    var col = conn.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                    var results = col.Query()
                        .ToList();
                    foreach (var record in results)
                    {
                        Assert.Equal(QueueStatuses.Waiting, record.Status);
                    }
                }
            }
            else
            {
                using (var conn = connScope.GetDatabase())
                {
                    var col = conn.Database.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                    var results = col.Query()
                        .ToList();
                    foreach (var record in results)
                    {
                        Assert.Equal(QueueStatuses.Waiting, record.Status);
                    }
                }
            }
        }

        private void VerifyStatusTable(long expectedMessageCount, string route)
        {
            var connScope = _scope.GetDisposable<LiteDbConnectionManager>();
            if (connScope == null)
            {
                using (var conn = new LiteDatabase(_connection.ConnectionString))
                {
                    var col = conn.GetCollection<Schema.StatusTable>(_tableNameHelper.StatusName);
                    var results = col.Query()
                        .ToList();

                    if (string.IsNullOrWhiteSpace(route))
                        Assert.Equal(results.Count, expectedMessageCount);

                    foreach (var record in results)
                    {
                        Assert.Equal(QueueStatuses.Waiting, record.Status);
                    }
                }
            }
            else
            {
                using (var conn = connScope.GetDatabase())
                {
                    var col = conn.Database.GetCollection<Schema.StatusTable>(_tableNameHelper.StatusName);
                    var results = col.Query()
                        .ToList();

                    if (string.IsNullOrWhiteSpace(route))
                        Assert.Equal(results.Count, expectedMessageCount);

                    foreach (var record in results)
                    {
                        Assert.Equal(QueueStatuses.Waiting, record.Status);
                    }
                }
            }
        }
    }

    public class VerifyQueueRecordCount
    {
        private readonly LiteDbMessageQueueTransportOptions _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly LiteDbConnectionInformation _connection;
        private readonly ICreationScope _scope;

        public VerifyQueueRecordCount(string queueName, string connectionString, LiteDbMessageQueueTransportOptions options, ICreationScope scope)
        {
            _options = options;
            _connection = new LiteDbConnectionInformation(new QueueConnection(queueName, connectionString));
            _tableNameHelper = new TableNameHelper(_connection);
            _scope = scope;
        }

        public void Verify(int recordCount, bool ignoreMeta, bool ignoreErrorTracking)
        {
            AllTablesRecordCount(recordCount, ignoreMeta, ignoreErrorTracking);
        }

        private void AllTablesRecordCount(int recordCount, bool ignoreMeta, bool ignoreErrorTracking)
        {
            var connScope = _scope.GetDisposable<LiteDbConnectionManager>();
            if (connScope == null)
            {
                using (var conn = new LiteDatabase(_connection.ConnectionString))
                {
                    if (!ignoreMeta)
                    {
                        var col = conn.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                        Assert.Equal(recordCount, col.Count());
                    }

                    if (!ignoreErrorTracking)
                    {
                        var col = conn.GetCollection<Schema.ErrorTrackingTable>(_tableNameHelper.ErrorTrackingName);
                        Assert.Equal(recordCount, col.Count());
                    }

                    var col2 = conn.GetCollection<Schema.MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);
                    Assert.Equal(recordCount, col2.Count());

                    var col3 = conn.GetCollection<Schema.QueueTable>(_tableNameHelper.QueueName);
                    Assert.Equal(recordCount, col3.Count());

                    if (_options.EnableStatusTable)
                    {
                        var col = conn.GetCollection<Schema.StatusTable>(_tableNameHelper.StatusName);
                        Assert.Equal(recordCount, col.Count());
                    }
                }
            }
            else
            {
                using (var conn = connScope.GetDatabase())
                {
                    if (!ignoreMeta)
                    {
                        var col = conn.Database.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                        Assert.Equal(recordCount, col.Count());
                    }

                    if (!ignoreErrorTracking)
                    {
                        var col = conn.Database.GetCollection<Schema.ErrorTrackingTable>(_tableNameHelper.ErrorTrackingName);
                        Assert.Equal(recordCount, col.Count());
                    }

                    var col2 = conn.Database.GetCollection<Schema.MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);
                    Assert.Equal(recordCount, col2.Count());

                    var col3 = conn.Database.GetCollection<Schema.QueueTable>(_tableNameHelper.QueueName);
                    Assert.Equal(recordCount, col3.Count());

                    if (_options.EnableStatusTable)
                    {
                        var col = conn.Database.GetCollection<Schema.StatusTable>(_tableNameHelper.StatusName);
                        Assert.Equal(recordCount, col.Count());
                    }
                }
            }
        }
    }

    public class VerifyErrorCounts
    {
        private readonly TableNameHelper _tableNameHelper;
        private readonly LiteDbConnectionInformation _connection;
        private readonly ICreationScope _scope;
        public VerifyErrorCounts(string queueName, string connectionString, ICreationScope scope)
        {
            _connection = new LiteDbConnectionInformation(new QueueConnection(queueName, connectionString));
            _tableNameHelper = new TableNameHelper(_connection);
            _scope = scope;
        }

        public void Verify(long messageCount, int errorCount)
        {
            var connScope = _scope.GetDisposable<LiteDbConnectionManager>();
            if (connScope == null)
            {
                using (var conn = new LiteDatabase(_connection.ConnectionString))
                {
                    var col = conn.GetCollection<Schema.MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);
                    Assert.Equal(messageCount, col.Count());

                    //only check the two below tables if the error count is > 0.
                    //error count of 0 means we are processing poison messages
                    //poison messages go right into the error queue, without updating the tracking table
                    if (errorCount > 0)
                    {
                        var col2 = conn.GetCollection<Schema.ErrorTrackingTable>(_tableNameHelper.ErrorTrackingName);
                        Assert.Equal(messageCount, col2.Count());

                        var results = col2.Query()
                            .ToList();
                        foreach (var record in results)
                        {
                            Assert.Equal(errorCount, record.RetryCount);
                        }
                    }
                }
            }
            else
            {
                using (var conn = connScope.GetDatabase())
                {
                    var col = conn.Database.GetCollection<Schema.MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);
                    Assert.Equal(messageCount, col.Count());

                    //only check the two below tables if the error count is > 0.
                    //error count of 0 means we are processing poison messages
                    //poison messages go right into the error queue, without updating the tracking table
                    if (errorCount > 0)
                    {
                        var col2 = conn.Database.GetCollection<Schema.ErrorTrackingTable>(_tableNameHelper.ErrorTrackingName);
                        Assert.Equal(messageCount, col2.Count());

                        var results = col2.Query()
                            .ToList();
                        foreach (var record in results)
                        {
                            Assert.Equal(errorCount, record.RetryCount);
                        }
                    }
                }
            }
        }
    }
}
