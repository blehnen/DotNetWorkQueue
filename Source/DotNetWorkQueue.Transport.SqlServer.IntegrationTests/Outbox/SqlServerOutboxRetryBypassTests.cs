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
using System.Diagnostics;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Outbox
{
    /// <summary>
    /// Integration test proving the SqlServer outbox caller-tx Send path does NOT
    /// retry on failure. The IRetrySkippable marker on RelationalSendMessageCommand
    /// (Phase 3) tells the RetryCommandHandlerOutputDecorator to short-circuit the
    /// Polly retry chain so the caller's external transaction sees the failure
    /// immediately rather than spinning through 3 attempts. Closes PROJECT.md SC #8
    /// at the integration level (structural unit pin lives in Phase 3).
    /// </summary>
    [TestClass]
    public class SqlServerOutboxRetryBypassTests : SqlServerOutboxIntegrationTestBase
    {
        [TestMethod]
        public void RetryBypass_TransientError_SingleAttempt()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            // Committed-tx technique (RESEARCH §4): open a connection, begin a tx,
            // and immediately commit it. The transaction is now in a completed state;
            // SqlClient nulls tx.Connection after Commit() (validator check 2 trips),
            // so the next Send against this tx must throw on the very first attempt.
            using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();
            var tx = conn.BeginTransaction();
            tx.Commit();

            var msg = GenerateMessage.Create<FakeMessage>();

            var sw = Stopwatch.StartNew();
            Exception caught = null;
            try
            {
                producer.RelationalProducer.Send(msg, tx);
                Assert.Fail("Expected an exception from the caller-tx Send on a completed tx.");
            }
            catch (Exception ex)
            {
                caught = ex;
            }
            sw.Stop();

            Assert.IsNotNull(caught, "Caller-tx Send must throw on completed tx.");

            // Single-attempt assertion: a 3x retry chain with Polly back-off would take
            // seconds. The bypass means the throw is essentially immediate. The 2000ms
            // cap distinguishes single vs. 3-attempt outcomes while leaving slack for
            // slow CI hosts. If this proves flaky raise to 3000ms; DO NOT remove the
            // timing assertion -- it is the only integration-level pin against the
            // "retry decorator silently regressed" failure mode.
            Assert.IsTrue(sw.ElapsedMilliseconds < 2000,
                $"Caller-tx Send took {sw.ElapsedMilliseconds}ms -- expected < 2000ms " +
                "for single-attempt failure (3x retry chain would exceed this).");

            // Belt-and-suspenders: no row landed in the queue MetaData table.
            AssertQueueRowCount(qc, 0);

            try { tx.Dispose(); } catch { /* ignore */ }
        }
    }
}
