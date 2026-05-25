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
using System.Data;
using System.Data.Common;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class SqLiteExternalTransactionValidatorTests
    {
        // Builds the SUT wired to NSubstitute stubs.
        // extractorResult:  what IExternalDbNameExtractor.Extract returns on the connection side.
        // queueConnectionString: queue's IConnectionInformation.ConnectionString — the validator
        //                        canonicalizes this via SqLiteExternalDbNameExtractor.Canonicalize
        //                        to compute the expected stem.
        private static (SqLiteExternalTransactionValidator sut, DbTransaction transaction, DbConnection conn)
            BuildSut(string extractorResult, string queueConnectionString,
                     ConnectionState connState = ConnectionState.Open,
                     bool nullConnectionOnTx = false)
        {
            var extractor = Substitute.For<IExternalDbNameExtractor>();
            extractor.Extract(Arg.Any<DbConnection>()).Returns(extractorResult);

            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.ConnectionString.Returns(queueConnectionString);

            var conn = Substitute.For<DbConnection>();
            conn.State.Returns(connState);

            var transaction = Substitute.For<DbTransaction>();
            transaction.Connection.Returns(nullConnectionOnTx ? null : conn);

            return (new SqLiteExternalTransactionValidator(extractor, connInfo), transaction, conn);
        }

        // ------------------------------------------------------------------ base-behavior preservation

        [TestMethod]
        public void Validate_NullTransaction_ThrowsArgumentNullException()
        {
            var (sut, _, _) = BuildSut("queue", "Data Source=queue;Version=3;");
            Assert.ThrowsExactly<ArgumentNullException>(() => sut.Validate(null));
        }

        [TestMethod]
        public void Validate_NullConnection_ThrowsInvalidOperationExceptionWithNullConnectionInMessage()
        {
            var (sut, transaction, _) = BuildSut("queue", "Data Source=queue;Version=3;", nullConnectionOnTx: true);
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => sut.Validate(transaction));
            StringAssert.Contains(ex.Message, "null Connection");
        }

        [TestMethod]
        public void Validate_ConnectionNotOpen_ThrowsInvalidOperationExceptionWithStateInMessage()
        {
            var (sut, transaction, _) = BuildSut("queue", "Data Source=queue;Version=3;", connState: ConnectionState.Closed);
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => sut.Validate(transaction));
            StringAssert.Contains(ex.Message, "Closed");
        }

        // ------------------------------------------------------------------ SQLite-specific normalization

        [TestMethod]
        public void Validate_Db3RoundTrip_NormalizesAndMatches_DoesNotThrow()
        {
            // Linux runtime: System.Data.SQLite strips .db3 from the opened connection's
            // DataSource, so the extractor returns "myqueue"; the queue's connection string
            // has the full path, which Canonicalize reduces to the same stem.
            var (sut, transaction, _) = BuildSut(
                extractorResult: "myqueue",
                queueConnectionString: "Data Source=/data/myqueue.db3;Version=3;");
            sut.Validate(transaction); // must not throw
        }

        [TestMethod]
        public void Validate_MemoryMode_NormalizesAndMatches_DoesNotThrow()
        {
            var (sut, transaction, _) = BuildSut(
                extractorResult: ":memory:",
                queueConnectionString: "Data Source=:memory:;Version=3;");
            sut.Validate(transaction);
        }

        [TestMethod]
        public void Validate_FullUriSharedMemory_NormalizesAndMatches_DoesNotThrow()
        {
            // FullUri=file:NAME?mode=memory&cache=shared is the shared-in-memory form used by
            // the SQLite integration test suite (IntegrationConnectionInfo(inMemory:true)).
            // Both sides go through Canonicalize → SQLiteConnectionStringBuilder, which picks
            // FullUri (not DataSource — DataSource is empty for this form).
            const string fullUri = "file:Iabc?mode=memory&cache=shared";
            var (sut, transaction, _) = BuildSut(
                extractorResult: fullUri,
                queueConnectionString: $"FullUri={fullUri};Version=3;");
            sut.Validate(transaction);
        }

        [TestMethod]
        public void Validate_DbNameMismatch_ThrowsInvalidOperationExceptionWithNormalizedValues()
        {
            var (sut, transaction, _) = BuildSut(
                extractorResult: "queueA",
                queueConnectionString: "Data Source=/data/queueB.db3;Version=3;");
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => sut.Validate(transaction));
            StringAssert.Contains(ex.Message, "queueA");
            StringAssert.Contains(ex.Message, "queueB");
            StringAssert.DoesNotMatch(ex.Message,
                new System.Text.RegularExpressions.Regex(@"queueB\.db3"));
        }

        [TestMethod]
        public void Validate_FullUriMismatch_ThrowsAndReportsBothUris()
        {
            // The pre-fix bug: extractor returned the raw URI while the queue side returned
            // empty (SQLiteConnectionStringBuilder("FullUri=...").DataSource is empty), so
            // every FullUri test exploded with `'' != 'file:...'`. After the fix both sides
            // canonicalize via FullUri, so genuine FullUri-name mismatches still throw with
            // both URIs visible in the message.
            var (sut, transaction, _) = BuildSut(
                extractorResult: "file:Iabc?mode=memory&cache=shared",
                queueConnectionString: "FullUri=file:Ixyz?mode=memory&cache=shared;Version=3;");
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => sut.Validate(transaction));
            StringAssert.Contains(ex.Message, "file:Iabc");
            StringAssert.Contains(ex.Message, "file:Ixyz");
        }
    }
}
