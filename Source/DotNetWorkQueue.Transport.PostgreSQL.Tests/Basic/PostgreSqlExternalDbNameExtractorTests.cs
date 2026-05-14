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
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    [TestClass]
    public class PostgreSqlExternalDbNameExtractorTests
    {
        [TestMethod]
        public void Extract_ReturnsConnectionDatabase_Verbatim()
        {
            // CONTEXT-4 Decision 2: pass-through with NO normalization.
            var conn = Substitute.For<DbConnection>();
            conn.Database.Returns("MyDb");
            var sut = new PostgreSqlExternalDbNameExtractor();
            Assert.AreEqual("MyDb", sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_PreservesCase_NoUpperCasing()
        {
            // CONTEXT-4 Decision 2 / Risk #3 closure: PG identifier case is preserved by Npgsql.
            // Verify the extractor returns the raw connection.Database string without folding
            // case. Contrast with SqlServerExternalDbNameExtractor which upper-cases.
            var conn1 = Substitute.For<DbConnection>();
            conn1.Database.Returns("MyDb");
            var conn2 = Substitute.For<DbConnection>();
            conn2.Database.Returns("mydb");
            var sut = new PostgreSqlExternalDbNameExtractor();

            // Two different inputs MUST produce two different outputs (case-sensitive).
            Assert.AreNotEqual(sut.Extract(conn1), sut.Extract(conn2),
                "PG extractor must NOT normalize case — pass-through is required for " +
                "PostgreSQL's case-sensitive quoted-identifier semantics.");
            Assert.AreEqual("MyDb", sut.Extract(conn1));
            Assert.AreEqual("mydb", sut.Extract(conn2));
        }
    }
}
