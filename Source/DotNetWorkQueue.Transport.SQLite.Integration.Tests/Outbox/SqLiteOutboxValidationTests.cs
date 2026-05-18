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
using System.Data.SQLite;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Outbox
{
    /// <summary>
    /// Validation integration tests for the SQLite outbox producer's caller-transaction
    /// path. Proves the ExternalTransactionValidator runs BEFORE any SQL write — cross-DB
    /// (different file path) and closed-connection cases both throw InvalidOperationException
    /// and leave the queue's MetaData table untouched. Closes PROJECT.md §SC #6.
    ///
    /// SQLite cross-DB semantics: the SqLiteExternalDbNameExtractor canonicalizes
    /// DbConnection.DataSource via Path.GetFullPath().ToUpperInvariant() (with a
    /// :memory: short-circuit). Two distinct on-disk file paths → different DB names
    /// → validator rejects.
    /// </summary>
    [TestClass]
    public class SqLiteOutboxValidationTests : SqLiteOutboxIntegrationTestBase
    {
        [ClassInitialize]
        public static void Init(TestContext _) => EnsureActivityListenerRegistered();

        [TestMethod]
        public void CrossDatabase_FilePath_Mismatch_ValidatorRejects()
        {
            // Two distinct file-backed SQLite databases. The queue's catalog is dbScope's file;
            // the caller's transaction runs on wrongDbScope's file. The validator must throw
            // before any INSERT lands.
            using var dbScope = new IntegrationConnectionInfo(inMemory: false);
            using var wrongDbScope = new IntegrationConnectionInfo(inMemory: false);

            var qc = new QueueConnection(NewQueueName(), dbScope.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            using var wrongConn = new SQLiteConnection(wrongDbScope.ConnectionString);
            wrongConn.Open();
            using var wrongTransaction = wrongConn.BeginTransaction();

            var msg = GenerateMessage.Create<FakeMessage>();

            Assert.ThrowsExactly<InvalidOperationException>(
                () => producer.RelationalProducer.Send(msg, wrongTransaction));

            // No partial write — queue MetaData table count must be 0.
            AssertQueueRowCount(qc, 0);

            try { wrongTransaction.Rollback(); } catch { /* ignore — transaction may already be invalidated */ }
        }

        [TestMethod]
        public void ClosedConnection_ValidatorRejects()
        {
            using var dbScope = new IntegrationConnectionInfo(inMemory: false);
            var qc = new QueueConnection(NewQueueName(), dbScope.ConnectionString);

            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            var conn = new SQLiteConnection(dbScope.ConnectionString);
            conn.Open();
            var transaction = conn.BeginTransaction();
            // Close the connection — validator's connection-state check must reject before any INSERT.
            conn.Close();

            var msg = GenerateMessage.Create<FakeMessage>();

            Assert.ThrowsExactly<InvalidOperationException>(
                () => producer.RelationalProducer.Send(msg, transaction));

            AssertQueueRowCount(qc, 0);

            try { transaction.Dispose(); } catch { /* ignore */ }
            try { conn.Dispose(); } catch { /* ignore */ }
        }
    }
}
