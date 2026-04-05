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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using NSubstitute;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic
{
    [TestClass]
    public class QueryMessageHistoryHandlerTests
    {
        private const string QueueName = "testQueue";

        private static (QueryMessageHistoryHandler handler, LiteDbConnectionManager connectionManager,
            TableNameHelper tableNameHelper) CreateHandler()
        {
            var connString = "Filename=:memory:;Connection=direct";

            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.ConnectionString.Returns(connString);
            connInfo.QueueName.Returns(QueueName);

            var scope = Substitute.For<ICreationScope>();
            var connectionManager = new LiteDbConnectionManager(connInfo, scope);
            var tableNameHelper = new TableNameHelper(connInfo);

            var handler = new QueryMessageHistoryHandler(connectionManager, tableNameHelper);
            return (handler, connectionManager, tableNameHelper);
        }

        private static void InsertRow(LiteDbConnectionManager cm, TableNameHelper tnh, HistoryTable row)
        {
            using (var db = cm.GetDatabase())
            {
                var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                col.Insert(row);
            }
        }

        // --- DurationMs=0 preservation tests ---

        [TestMethod]
        public void Query_CompletedRow_DurationZero_PreservesZero()
        {
            // Arrange: Complete row — DurationMs=0 stored (sub-ms completion)
            var (handler, cm, tnh) = CreateHandler();
            using (cm)
            {
                var completedTicks = DateTime.UtcNow.Ticks;
                InsertRow(cm, tnh, new HistoryTable
                {
                    QueueId = "q1",
                    CorrelationId = "c1",
                    Status = (int)MessageHistoryStatus.Complete,
                    EnqueuedUtc = completedTicks - 1000,
                    StartedUtc = completedTicks - 100,
                    CompletedUtc = completedTicks,
                    DurationMs = 0L,   // sub-ms completion — must survive the mapping
                    RetryCount = 0
                });

                // Act
                var record = handler.GetByQueueId("q1");

                // Assert: DurationMs must be 0, NOT null
                record.Should().NotBeNull();
                record.DurationMs.Should().Be(0L,
                    "DurationMs=0 on a completed row must be preserved as 0, not converted to null");
            }
        }

        [TestMethod]
        public void Query_EnqueuedRow_NoCompletion_DurationIsNull()
        {
            // Arrange: Enqueued row — CompletedUtc=0 means never completed
            var (handler, cm, tnh) = CreateHandler();
            using (cm)
            {
                InsertRow(cm, tnh, new HistoryTable
                {
                    QueueId = "q2",
                    CorrelationId = "",
                    Status = (int)MessageHistoryStatus.Enqueued,
                    EnqueuedUtc = DateTime.UtcNow.Ticks,
                    StartedUtc = 0L,
                    CompletedUtc = 0L,
                    DurationMs = 0L,   // stored as 0 but row never completed
                    RetryCount = 0
                });

                // Act
                var record = handler.GetByQueueId("q2");

                // Assert: DurationMs must be null — row never completed
                record.Should().NotBeNull();
                record.DurationMs.Should().BeNull(
                    "DurationMs must be null when CompletedUtc=0 (row never completed)");
            }
        }
    }
}
