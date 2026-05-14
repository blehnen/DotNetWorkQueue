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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class ExternalTransactionValidatorTests
    {
        private const string QueueDb = "MyQueueDb";

        private static (ExternalTransactionValidator sut, DbTransaction tx, DbConnection conn)
            BuildSut(string actualDbFromExtractor = QueueDb,
                     ConnectionState connState = ConnectionState.Open,
                     bool nullConnectionOnTx = false)
        {
            var extractor = Substitute.For<IExternalDbNameExtractor>();
            extractor.Extract(Arg.Any<DbConnection>()).Returns(actualDbFromExtractor);
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.Container.Returns(QueueDb);

            var conn = Substitute.For<DbConnection>();
            conn.State.Returns(connState);

            var tx = Substitute.For<DbTransaction>();
            // DbTransaction.Connection getter — NSubstitute on abstract base
            tx.Connection.Returns(nullConnectionOnTx ? null : conn);

            var sut = new ExternalTransactionValidator(extractor, connInfo);
            return (sut, tx, conn);
        }

        [TestMethod]
        public void Validate_WhenTransactionIsNull_ThrowsArgumentNullException()
        {
            var (sut, _, _) = BuildSut();
            Assert.ThrowsExactly<ArgumentNullException>(() => sut.Validate(null));
        }

        [TestMethod]
        public void Validate_WhenConnectionIsNull_ThrowsInvalidOperationException()
        {
            var (sut, tx, _) = BuildSut(nullConnectionOnTx: true);
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => sut.Validate(tx));
            StringAssert.Contains(ex.Message, "null Connection");
        }

        [TestMethod]
        public void Validate_WhenConnectionNotOpen_ThrowsInvalidOperationException()
        {
            var (sut, tx, _) = BuildSut(connState: ConnectionState.Closed);
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => sut.Validate(tx));
            StringAssert.Contains(ex.Message, "Closed");
        }

        [TestMethod]
        public void Validate_WhenDatabaseNameMismatch_ThrowsInvalidOperationExceptionWithBothNames()
        {
            var (sut, tx, _) = BuildSut(actualDbFromExtractor: "WrongDb");
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => sut.Validate(tx));
            // Diagnostics requirement (PROJECT.md §Non-Functional Diagnostics):
            StringAssert.Contains(ex.Message, "WrongDb");
            StringAssert.Contains(ex.Message, QueueDb);
        }

        [TestMethod]
        public void Validate_WhenAllChecksPass_DoesNotThrow()
        {
            var (sut, tx, _) = BuildSut();
            sut.Validate(tx); // must not throw
        }
    }
}
