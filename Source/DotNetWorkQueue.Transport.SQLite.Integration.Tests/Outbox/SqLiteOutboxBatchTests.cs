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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Outbox
{
    /// <summary>
    /// Batch method-matrix integration tests for the SQLite outbox producer.
    /// (sync + async) x (commit + rollback) = 4 tests.
    /// </summary>
    [TestClass]
    public class SqLiteOutboxBatchTests : SqLiteOutboxIntegrationTestBase
    {
        private const int BatchSize = 5;

        [ClassInitialize]
        public static void Init(TestContext _) => EnsureActivityListenerRegistered();

        [TestMethod]
        public void SendBatch_Commit_AllRowsVisible()
        {
            using var dbScope = new IntegrationConnectionInfo(inMemory: false);
            var qc = new QueueConnection(NewQueueName(), dbScope.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new SQLiteConnection(dbScope.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var transaction = conn.BeginTransaction())
                {
                    var batch = BuildBatch(BatchSize);
                    var result = producer.RelationalProducer.Send(batch, transaction);
                    Assert.IsFalse(result.HasErrors, "batch send reported errors");
                    AssertCallerResourcesUnmutated(conn, transaction);

                    for (var i = 0; i < BatchSize; i++)
                        InsertBusinessRow(conn, transaction, businessTable, i, $"row{i}");
                    transaction.Commit();
                }

                AssertQueueRowCount(qc, BatchSize);
                AssertBusinessRowExists(conn, businessTable, BatchSize);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        public void SendBatch_Rollback_NoRowsVisible()
        {
            using var dbScope = new IntegrationConnectionInfo(inMemory: false);
            var qc = new QueueConnection(NewQueueName(), dbScope.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new SQLiteConnection(dbScope.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var transaction = conn.BeginTransaction())
                {
                    var batch = BuildBatch(BatchSize);
                    var result = producer.RelationalProducer.Send(batch, transaction);
                    Assert.IsFalse(result.HasErrors, "batch send reported errors");
                    AssertCallerResourcesUnmutated(conn, transaction);

                    for (var i = 0; i < BatchSize; i++)
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

        [TestMethod]
        public async Task SendBatchAsync_Commit_AllRowsVisible()
        {
            using var dbScope = new IntegrationConnectionInfo(inMemory: false);
            var qc = new QueueConnection(NewQueueName(), dbScope.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new SQLiteConnection(dbScope.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var transaction = conn.BeginTransaction())
                {
                    var batch = BuildBatch(BatchSize);
                    var result = await producer.RelationalProducer.SendAsync(batch, transaction);
                    Assert.IsFalse(result.HasErrors, "batch send reported errors");
                    AssertCallerResourcesUnmutated(conn, transaction);

                    for (var i = 0; i < BatchSize; i++)
                        InsertBusinessRow(conn, transaction, businessTable, i, $"row{i}");
                    transaction.Commit();
                }

                AssertQueueRowCount(qc, BatchSize);
                AssertBusinessRowExists(conn, businessTable, BatchSize);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        public async Task SendBatchAsync_Rollback_NoRowsVisible()
        {
            using var dbScope = new IntegrationConnectionInfo(inMemory: false);
            var qc = new QueueConnection(NewQueueName(), dbScope.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new SQLiteConnection(dbScope.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var transaction = conn.BeginTransaction())
                {
                    var batch = BuildBatch(BatchSize);
                    var result = await producer.RelationalProducer.SendAsync(batch, transaction);
                    Assert.IsFalse(result.HasErrors, "batch send reported errors");
                    AssertCallerResourcesUnmutated(conn, transaction);

                    for (var i = 0; i < BatchSize; i++)
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
