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
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Outbox
{
    /// <summary>
    /// Async method-matrix integration tests for the SqlServer outbox producer.
    /// Single + batch x commit + rollback = 4 tests.
    /// Async branch coverage requires its own integration tests per CLAUDE.md
    /// (sync handler coverage cannot be inferred from the async handler).
    /// </summary>
    [TestClass]
    public class SqlServerOutboxSendAsyncTests : SqlServerOutboxIntegrationTestBase
    {
        [TestMethod]
        public async Task SendAsync_Commit_BothRowsVisible()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            await using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                await using (var tx = (SqlTransaction)await conn.BeginTransactionAsync())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = await producer.RelationalProducer.SendAsync(msg, tx);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, tx, businessTable, 1, "first");
                    await tx.CommitAsync();
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
        public async Task SendAsync_Rollback_NeitherRowVisible()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            await using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                await using (var tx = (SqlTransaction)await conn.BeginTransactionAsync())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = await producer.RelationalProducer.SendAsync(msg, tx);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, tx, businessTable, 1, "first");
                    await tx.RollbackAsync();
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
            const int batchSize = 5;
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            await using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                await using (var tx = (SqlTransaction)await conn.BeginTransactionAsync())
                {
                    var batch = BuildBatch(batchSize);
                    var result = await producer.RelationalProducer.SendAsync(batch, tx);
                    Assert.IsFalse(result.HasErrors, "batch async send reported errors");
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRow(conn, tx, businessTable, i, $"row{i}");
                    await tx.CommitAsync();
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
        public async Task SendBatchAsync_Rollback_NeitherRowVisible()
        {
            const int batchSize = 5;
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            await using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                await using (var tx = (SqlTransaction)await conn.BeginTransactionAsync())
                {
                    var batch = BuildBatch(batchSize);
                    var result = await producer.RelationalProducer.SendAsync(batch, tx);
                    Assert.IsFalse(result.HasErrors, "batch async send reported errors");
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRow(conn, tx, businessTable, i, $"row{i}");
                    await tx.RollbackAsync();
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
