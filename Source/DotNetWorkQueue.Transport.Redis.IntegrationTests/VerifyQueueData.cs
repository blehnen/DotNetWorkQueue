using System;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests
{
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
    public class VerifyQueueData : IDisposable
    {
        private readonly QueueProducerConfiguration _configuration;
        private readonly RedisNames _redisNames;
        private readonly RedisConnection _connection;

        public VerifyQueueData(QueueConnection queueConnection, QueueProducerConfiguration configuration)
        {
            _configuration = configuration;
            var connection = new BaseConnectionInformation(queueConnection);
            _redisNames = new RedisNames(connection);
            _connection = new RedisConnection(connection);
        }
        public void Verify(long expectedMessageCount, int expectedStatus, string route)
        {
            VerifyCount(expectedMessageCount, route);
            VerifyStatus(expectedMessageCount, expectedStatus, route);

            // ReSharper disable once PossibleInvalidOperationException
            if (_configuration.GetMessageDelay().HasValue && _configuration.GetMessageDelay().Value)
            {
                VerifyDelayedProcessing(expectedMessageCount);
            }

            // ReSharper disable once PossibleInvalidOperationException
            if (_configuration.GetMessageExpiration().HasValue && _configuration.GetMessageExpiration().Value)
            {
                VerifyMessageExpiration(expectedMessageCount);
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void VerifyCount(long messageCount, string route)
        {
            var db = _connection.Connection.GetDatabase();
            var records = !string.IsNullOrEmpty(route) ? db.ListLength(_redisNames.PendingRoute(route)) : db.HashLength(_redisNames.Status);
            Assert.Equal(messageCount, records);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void VerifyStatus(long messageCount,
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            int expectedStatus, string route)
        {
            var db = _connection.Connection.GetDatabase();
            var records = !string.IsNullOrEmpty(route) ? db.ListLength(_redisNames.PendingRoute(route)) : db.HashLength(_redisNames.Status);
            Assert.Equal(messageCount, records);

            var hashes = db.HashGetAll(_redisNames.Status);
            foreach (var hash in hashes)
            {
                Assert.Equal(expectedStatus.ToString(), hash.Value.ToString());
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void VerifyDelayedProcessing(long messageCount)
        {
            var db = _connection.Connection.GetDatabase();
            var records = db.SortedSetLength(_redisNames.Delayed);
            Assert.Equal(messageCount, records);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void VerifyMessageExpiration(long messageCount)
        {
            var db = _connection.Connection.GetDatabase();
            var records = db.SortedSetLength(_redisNames.Expiration);
            Assert.Equal(messageCount, records);
        }

        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
        public void Dispose()
        {
            _connection.Dispose();
        }
    }

    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
    public class VerifyQueueRecordCount : IDisposable
    {
        private readonly RedisNames _redisNames;
        private readonly RedisConnection _connection;

        public VerifyQueueRecordCount(string queueName, string connectionString)
        {
            var connection = new BaseConnectionInformation(new QueueConnection(queueName, connectionString));
            _redisNames = new RedisNames(connection);
            _connection = new RedisConnection(connection);
        }

        public void Verify(int recordCount, bool ignoreErrorTracking, int expectedStatus)
        {
            AllTablesRecordCount(recordCount, ignoreErrorTracking, expectedStatus);
        }

        private void AllTablesRecordCount(int recordCount, bool ignoreErrorTracking, int expectedStatus)
        {
            long records;
            var db = _connection.Connection.GetDatabase();

            if (!ignoreErrorTracking)
            {
                records = db.ListLength(_redisNames.Error);
                Assert.Equal(recordCount, records);
            }

            records = db.HashLength(_redisNames.Values);
            Assert.Equal(recordCount, records);

            VerifyStatus(recordCount, expectedStatus);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void VerifyStatus(long messageCount,
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            int expectedStatus)
        {
            var db = _connection.Connection.GetDatabase();
            var records = db.HashLength(_redisNames.Status);
            Assert.Equal(messageCount, records);

            var hashes = db.HashGetAll(_redisNames.Status);
            foreach (var hash in hashes)
            {
                Assert.Equal(expectedStatus.ToString(), hash.Value.ToString());
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
        public void Dispose()
        {
            _connection.Dispose();
        }
    }
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
    public class VerifyErrorCounts : IDisposable
    {
        private readonly RedisNames _redisNames;
        private readonly RedisConnection _connection;

        public VerifyErrorCounts(string queueName, string connectionString)
        {
            var connection = new BaseConnectionInformation(new QueueConnection(queueName, connectionString));
            _redisNames = new RedisNames(connection);
            _connection = new RedisConnection(connection);
        }

        public void Verify(long messageCount, int errorCount)
        {
            var db = _connection.Connection.GetDatabase();
            var records = db.HashLength(_redisNames.MetaData);
            Assert.Equal(messageCount, records);
        }

        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
