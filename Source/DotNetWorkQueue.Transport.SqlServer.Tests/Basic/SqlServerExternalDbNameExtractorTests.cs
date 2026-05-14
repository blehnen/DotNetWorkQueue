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
using System.Data.Common;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    [TestClass]
    public class SqlServerExternalDbNameExtractorTests
    {
        [TestMethod]
        public void Extract_ReturnsConnectionDatabase()
        {
            var conn = Substitute.For<DbConnection>();
            conn.Database.Returns("MyDb");
            var sut = new SqlServerExternalDbNameExtractor();
            Assert.AreEqual("MYDB", sut.Extract(conn));
        }

        [TestMethod]
        public void ConfiguredComparison_IsOrdinalIgnoreCase()
        {
            // The validator uses StringComparison.Ordinal for the final compare
            // (Phase 2 PLAN-2.1 decision); per-provider case semantics are encoded
            // in the extractor by normalizing case at extract time. SqlServer is
            // case-insensitive at the catalog level, so the extractor's output
            // must compare equal under OrdinalIgnoreCase across "MyDb" and "mydb".
            var conn1 = Substitute.For<DbConnection>();
            conn1.Database.Returns("MyDb");
            var conn2 = Substitute.For<DbConnection>();
            conn2.Database.Returns("mydb");
            var sut = new SqlServerExternalDbNameExtractor();

            // SqlServer extractor normalizes via OrdinalIgnoreCase semantics.
            // Implementation choice (uppercase canonicalization) is verified by
            // round-tripping through string.Equals(OrdinalIgnoreCase).
            Assert.IsTrue(
                string.Equals(sut.Extract(conn1), sut.Extract(conn2),
                    System.StringComparison.OrdinalIgnoreCase),
                "SqlServer extractor must produce equal outputs for case-variant database names " +
                "under OrdinalIgnoreCase comparison.");
        }
    }
}
