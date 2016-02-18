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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;
namespace DotNetWorkQueue.Transport.Redis.IntegrationTests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
    public class VerifyQueueData: IDisposable
    {
        private readonly QueueProducerConfiguration _configuration;
        private readonly RedisNames _redisNames;
        private readonly RedisConnection _connection;

        public VerifyQueueData(string queueName, QueueProducerConfiguration configuration)
        {
            _configuration = configuration;
            var connection = new BaseConnectionInformation()
            {
                ConnectionString = ConnectionInfo.ConnectionString,
                QueueName = queueName
            };
            _redisNames = new RedisNames(connection);
            _connection = new RedisConnection(connection);
        }
        public void Verify(long expectedMessageCount)
        {
            VerifyCount(expectedMessageCount);

            // ReSharper disable once PossibleInvalidOperationException
            if(_configuration.GetMessageDelay().HasValue && _configuration.GetMessageDelay().Value)
            {
                VerifyDelayedProcessing(expectedMessageCount);
            }

            // ReSharper disable once PossibleInvalidOperationException
            if (_configuration.GetMessageExpiration().HasValue && _configuration.GetMessageExpiration().Value)
            {
                VerifyMessageExpiration(expectedMessageCount);
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private void VerifyCount(long messageCount)
        {
            var db = _connection.Connection.GetDatabase();
            var records = db.HashLength(_redisNames.Values);
            Assert.Equal(messageCount, records);
        }

        // ReSharper disable once UnusedParameter.Local
        private void VerifyDelayedProcessing(long messageCount)
        {
            var db = _connection.Connection.GetDatabase();
            var records = db.SortedSetLength(_redisNames.Delayed);
            Assert.Equal(messageCount, records);
        }

        // ReSharper disable once UnusedParameter.Local
        private void VerifyMessageExpiration(long messageCount)
        {
            var db = _connection.Connection.GetDatabase();
            var records = db.SortedSetLength(_redisNames.Expiration);
            Assert.Equal(messageCount, records);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
        public void Dispose()
        {
            _connection.Dispose();
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
    public class VerifyQueueRecordCount : IDisposable
    {
        private readonly RedisNames _redisNames;
        private readonly RedisConnection _connection;

        public VerifyQueueRecordCount(string queueName)
        {
            var connection = new BaseConnectionInformation()
            {
                ConnectionString = ConnectionInfo.ConnectionString,
                QueueName = queueName
            };
            _redisNames = new RedisNames(connection);
            _connection = new RedisConnection(connection);
        }

        public void Verify(int recordCount, bool ignoreErrorTracking)
        {
            AllTablesRecordCount(recordCount, ignoreErrorTracking);
        }

        // ReSharper disable once UnusedParameter.Local
        private void AllTablesRecordCount(int recordCount, bool ignoreErrorTracking)
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
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
        public void Dispose()
        {
            _connection.Dispose();
        }
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
    public class VerifyErrorCounts : IDisposable
    {
        private readonly RedisNames _redisNames;
        private readonly RedisConnection _connection;

        public VerifyErrorCounts(string queueName)
        {
            var connection = new BaseConnectionInformation()
            {
                ConnectionString = ConnectionInfo.ConnectionString,
                QueueName = queueName
            };
            _redisNames = new RedisNames(connection);
            _connection = new RedisConnection(connection);
        }

        public void Verify(long messageCount, int errorCount)
        {
            var db = _connection.Connection.GetDatabase();
            var records = db.HashLength(_redisNames.MetaData);
            Assert.Equal(messageCount, records);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
