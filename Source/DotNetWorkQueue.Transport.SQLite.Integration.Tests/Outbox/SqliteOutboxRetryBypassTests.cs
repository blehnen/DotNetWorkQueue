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
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Outbox
{
    /// <summary>
    /// Integration test proving that the SQLite outbox caller-transaction Send path does NOT
    /// retry on failure. The <c>IRetrySkippable</c> marker on <c>RelationalSendMessageCommand</c>
    /// tells the <c>RetryCommandHandlerOutputDecorator</c> to short-circuit the Polly retry chain
    /// so the caller's external transaction sees the failure immediately rather than spinning
    /// through 3 attempts. Mirrors <c>PostgreSqlOutboxRetryBypassTests</c> with
    /// <see cref="SQLiteTransaction"/> substituted for <c>NpgsqlTransaction</c>.
    /// </summary>
    [TestClass]
    public class SqliteOutboxRetryBypassTests : SqliteOutboxIntegrationTestBase
    {
        /// <summary>
        /// Verifies that sending with a completed (already-committed) transaction throws on the
        /// first attempt and does not engage the Polly retry chain.
        /// </summary>
        /// <param name="inMemory">
        /// When <c>true</c> the queue lives in an in-memory SQLite database; when <c>false</c>
        /// it lives in a temp file. The retry-bypass path is queue-mode-agnostic; parameterizing
        /// keeps the pattern uniform across Wave-2 files (CONTEXT-3 D2).
        /// </param>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RetryBypass_TransientError_SingleAttempt(bool inMemory)
        {
            using var connInfo = new IntegrationConnectionInfo(inMemory);
            var qc = new QueueConnection(NewQueueName(), connInfo.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);
            using var conn = new SQLiteConnection(connInfo.ConnectionString);
            conn.Open();

            // WHY: Begin a transaction then immediately commit it. The transaction object is now in a
            //      completed state — any subsequent use must throw. We use this as the "transient error"
            //      stand-in to verify SkipRetry semantics, NOT to exercise SQLite-specific commit behavior.
            var completedTransaction = conn.BeginTransaction();
            completedTransaction.Commit();

            Exception thrown = null;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                producer.RelationalProducer.Send(new FakeMessage(), new AdditionalMessageData(), completedTransaction);
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
            stopwatch.Stop();

            // WHY: Polly's default 3-attempt retry chain with exponential back-off adds seconds. A
            //      single-attempt throw (SkipRetry=true because ExternalTransaction!=null) completes
            //      in well under 2000ms. The 2000ms bound is the regression guard against the retry
            //      chain accidentally engaging on the external-transaction Send path. Threshold matches
            //      PG (PostgreSqlOutboxRetryBypassTests lines 74-79) — same Polly defaults apply, both
            //      transports run on local/Docker CI.
            Assert.IsNotNull(thrown, "Expected Send to throw on a completed transaction");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000,
                $"Send took {stopwatch.ElapsedMilliseconds}ms — exceeds 2000ms bound; retry chain may have engaged on the external-transaction path (regression in IRetrySkippable wiring).");
            AssertQueueRowCount(qc, 0);
        }
    }
}
