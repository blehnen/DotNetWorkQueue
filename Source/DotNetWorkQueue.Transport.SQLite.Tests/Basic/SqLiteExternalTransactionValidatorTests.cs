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
        // Builds the SUT wired to NSubstitute stubs for all injected interfaces/abstract types.
        // extractorResult: what IExternalDbNameExtractor.Extract returns (the raw datasource stem).
        // containerValue:  what IConnectionInformation.Container returns (the raw configured path).
        private static (SqLiteExternalTransactionValidator sut, DbTransaction transaction, DbConnection conn)
            BuildSut(string extractorResult, string containerValue,
                     ConnectionState connState = ConnectionState.Open,
                     bool nullConnectionOnTx = false)
        {
            var extractor = Substitute.For<IExternalDbNameExtractor>();
            extractor.Extract(Arg.Any<DbConnection>()).Returns(extractorResult);

            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.Container.Returns(containerValue);

            var conn = Substitute.For<DbConnection>();
            conn.State.Returns(connState);

            var transaction = Substitute.For<DbTransaction>();
            // DbTransaction.Connection getter — NSubstitute on abstract base class (not sealed type).
            transaction.Connection.Returns(nullConnectionOnTx ? null : conn);

            return (new SqLiteExternalTransactionValidator(extractor, connInfo), transaction, conn);
        }

        // ------------------------------------------------------------------ base-behavior preservation

        [TestMethod]
        public void Validate_NullTransaction_ThrowsArgumentNullException()
        {
            // Base check #1 must survive the override.
            var (sut, _, _) = BuildSut("queue", "queue");
            Assert.ThrowsExactly<ArgumentNullException>(() => sut.Validate(null));
        }

        [TestMethod]
        public void Validate_NullConnection_ThrowsInvalidOperationExceptionWithNullConnectionInMessage()
        {
            // Base check #2: disposed/completed transaction has no Connection.
            var (sut, transaction, _) = BuildSut("queue", "queue", nullConnectionOnTx: true);
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => sut.Validate(transaction));
            StringAssert.Contains(ex.Message, "null Connection");
        }

        [TestMethod]
        public void Validate_ConnectionNotOpen_ThrowsInvalidOperationExceptionWithStateInMessage()
        {
            // Base check #3: connection must be Open.
            var (sut, transaction, _) = BuildSut("queue", "queue", connState: ConnectionState.Closed);
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => sut.Validate(transaction));
            StringAssert.Contains(ex.Message, "Closed");
        }

        // ------------------------------------------------------------------ SQLite-specific normalization

        [TestMethod]
        public void Validate_Db3RoundTrip_NormalizesAndMatches_DoesNotThrow()
        {
            // Primary landmine from PR #143: Container is a full path with .db3 extension;
            // the connection DataSource returns only the stem. Path.GetFileNameWithoutExtension
            // on both sides must produce equal stems so no exception is raised.
            var (sut, transaction, _) = BuildSut(extractorResult: "myqueue",
                                                  containerValue: "/data/myqueue.db3");
            sut.Validate(transaction); // must not throw
        }

        [TestMethod]
        public void Validate_MemoryMode_NormalizesAndMatches_DoesNotThrow()
        {
            // :memory: has no extension, so GetFileNameWithoutExtension returns ":memory:" on
            // both sides; Ordinal comparison succeeds.
            var (sut, transaction, _) = BuildSut(extractorResult: ":memory:",
                                                  containerValue: ":memory:");
            sut.Validate(transaction); // must not throw
        }

        [TestMethod]
        public void Validate_DbNameMismatch_ThrowsInvalidOperationExceptionWithNormalizedValues()
        {
            // Mismatch error must contain normalized stems, not the raw Container path.
            // "queueA" vs "/data/queueB.db3" → normalized to "queueA" vs "queueB".
            // Message must contain both stems and must NOT contain the raw ".db3" path.
            var (sut, transaction, _) = BuildSut(extractorResult: "queueA",
                                                  containerValue: "/data/queueB.db3");
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => sut.Validate(transaction));
            StringAssert.Contains(ex.Message, "queueA");
            StringAssert.Contains(ex.Message, "queueB");
            // Prove normalized values are used, not raw paths:
            StringAssert.DoesNotMatch(ex.Message,
                new System.Text.RegularExpressions.Regex(@"queueB\.db3"));
        }
    }
}
