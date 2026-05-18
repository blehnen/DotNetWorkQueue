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
using System.Diagnostics;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Outbox
{
    /// <summary>
    /// Integration test proving the SQLite outbox caller-transaction Send path does NOT
    /// retry on failure. The IRetrySkippable marker on RelationalSendMessageCommand
    /// tells the RetryCommandHandlerOutputDecorator to short-circuit the Polly retry
    /// chain so the caller's external transaction sees the failure immediately rather
    /// than spinning through 3 attempts. Closes PROJECT.md SC #8 at the integration
    /// level for the SQLite transport.
    /// </summary>
    [TestClass]
    public class SqLiteOutboxRetryBypassTests : SqLiteOutboxIntegrationTestBase
    {
        [ClassInitialize]
        public static void Init(TestContext _) => EnsureActivityListenerRegistered();

        [TestMethod]
        public void Transient_Error_Propagates_On_First_Attempt_Retry_Decorator_Not_Invoked()
        {
            using var dbScope = new IntegrationConnectionInfo(inMemory: false);
            var qc = new QueueConnection(NewQueueName(), dbScope.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            // Committed-transaction technique: open a connection, begin a transaction,
            // and immediately commit it. The transaction is now in a completed state;
            // the validator's connection-state / transaction-connection check trips, so
            // the next Send against this transaction throws on the very first attempt.
            using var conn = new SQLiteConnection(dbScope.ConnectionString);
            conn.Open();
            var transaction = conn.BeginTransaction();
            transaction.Commit();

            var msg = GenerateMessage.Create<FakeMessage>();

            var sw = Stopwatch.StartNew();
            Exception caught = null;
            try
            {
                producer.RelationalProducer.Send(msg, transaction);
                Assert.Fail("Expected an exception from the caller-transaction Send on a completed transaction.");
            }
            catch (Exception ex)
            {
                caught = ex;
            }
            sw.Stop();

            Assert.IsNotNull(caught, "Caller-transaction Send must throw on completed transaction.");

            // Single-attempt assertion: the 3x Polly retry chain would take seconds.
            // The bypass means the throw is essentially immediate. 2000ms cap leaves slack
            // for slow CI hosts; raise to 3000ms only if proven flaky — DO NOT remove the
            // timing assertion, it is the integration-level pin against the "retry decorator
            // silently regressed" failure mode (CLAUDE.md polling-not-snapshot lesson applies
            // to metric assertions but the elapsed-wall-clock check here measures the entire
            // Send path which is deterministic).
            Assert.IsTrue(sw.ElapsedMilliseconds < 2000,
                $"Caller-transaction Send took {sw.ElapsedMilliseconds}ms — expected < 2000ms " +
                "for single-attempt failure (3x retry chain would exceed this).");

            // Belt-and-suspenders: no row landed in the queue MetaData table.
            AssertQueueRowCount(qc, 0);

            try { transaction.Dispose(); } catch { /* ignore */ }
        }
    }
}
