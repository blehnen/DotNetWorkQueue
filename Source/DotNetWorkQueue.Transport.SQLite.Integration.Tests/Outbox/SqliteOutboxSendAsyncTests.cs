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
using System.Data.SQLite;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Outbox
{
    [TestClass]
    public class SqliteOutboxSendAsyncTests : SqliteOutboxIntegrationTestBase
    {
        [ClassInitialize]
        public static void Init(TestContext _) => EnsureActivityListenerRegistered();

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task SendAsync_Commit_BothRowsVisible(bool inMemory)
        {
            using var connInfo = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), connInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new SQLiteConnection(connInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                // WHY: System.Data.SQLite does not expose BeginTransactionAsync — use sync BeginTransaction.
                //      The async surface under test is SendAsync, not the transaction begin.
                using (var transaction = conn.BeginTransaction())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = await producer.RelationalProducer.SendAsync(msg, transaction).ConfigureAwait(false);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, transaction, businessTable, 1, "first");
                    transaction.Commit();
                }

                AssertQueueRowCount(qc, 1);
                AssertBusinessRowExists(conn, businessTable, 1);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task SendAsync_Rollback_NeitherRowVisible(bool inMemory)
        {
            using var connInfo = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), connInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new SQLiteConnection(connInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                // WHY: System.Data.SQLite does not expose BeginTransactionAsync — use sync BeginTransaction.
                //      The async surface under test is SendAsync, not the transaction begin.
                using (var transaction = conn.BeginTransaction())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = await producer.RelationalProducer.SendAsync(msg, transaction).ConfigureAwait(false);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, transaction, businessTable, 1, "first");
                    transaction.Rollback();
                }

                AssertQueueRowCount(qc, 0);
                AssertBusinessRowExists(conn, businessTable, 0);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task SendBatchAsync_Commit_AllRowsVisible(bool inMemory)
        {
            const int batchSize = 5;
            using var connInfo = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), connInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new SQLiteConnection(connInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                // WHY: System.Data.SQLite does not expose BeginTransactionAsync — use sync BeginTransaction.
                //      The async surface under test is SendAsync, not the transaction begin.
                using (var transaction = conn.BeginTransaction())
                {
                    var batch = BuildBatch(batchSize);
                    var result = await producer.RelationalProducer.SendAsync(batch, transaction).ConfigureAwait(false);
                    Assert.IsFalse(result.HasErrors);
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRow(conn, transaction, businessTable, i, $"row{i}");
                    transaction.Commit();
                }

                AssertQueueRowCount(qc, batchSize);
                AssertBusinessRowExists(conn, businessTable, batchSize);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task SendBatchAsync_Rollback_NeitherRowVisible(bool inMemory)
        {
            const int batchSize = 5;
            using var connInfo = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), connInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new SQLiteConnection(connInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                // WHY: System.Data.SQLite does not expose BeginTransactionAsync — use sync BeginTransaction.
                //      The async surface under test is SendAsync, not the transaction begin.
                using (var transaction = conn.BeginTransaction())
                {
                    var batch = BuildBatch(batchSize);
                    var result = await producer.RelationalProducer.SendAsync(batch, transaction).ConfigureAwait(false);
                    Assert.IsFalse(result.HasErrors);
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRow(conn, transaction, businessTable, i, $"row{i}");
                    transaction.Rollback();
                }

                AssertQueueRowCount(qc, 0);
                AssertBusinessRowExists(conn, businessTable, 0);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }
    }
}
