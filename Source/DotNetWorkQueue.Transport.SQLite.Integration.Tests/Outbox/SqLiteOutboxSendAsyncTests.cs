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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Outbox
{
    /// <summary>
    /// Async method-matrix integration tests for the SQLite outbox producer.
    /// System.Data.SQLite's async methods delegate to the sync implementation, but
    /// the producer's SendAsync code path is distinct from Send — async branch
    /// coverage requires its own integration tests per CLAUDE.md.
    /// </summary>
    [TestClass]
    public class SqLiteOutboxSendAsyncTests : SqLiteOutboxIntegrationTestBase
    {
        [ClassInitialize]
        public static void Init(TestContext _) => EnsureActivityListenerRegistered();

        [TestMethod]
        public async Task SendAsync_Commit_BothRowsVisible()
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
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = await producer.RelationalProducer.SendAsync(msg, transaction);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    AssertCallerResourcesUnmutated(conn, transaction);

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
        public async Task SendAsync_Rollback_NeitherRowVisible()
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
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = await producer.RelationalProducer.SendAsync(msg, transaction);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    AssertCallerResourcesUnmutated(conn, transaction);

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
    }
}
