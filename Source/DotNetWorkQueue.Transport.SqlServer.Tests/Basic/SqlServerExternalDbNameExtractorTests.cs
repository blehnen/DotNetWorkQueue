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
        public void Extract_ReturnsConnectionDatabaseVerbatim()
        {
            // Pass-through extractor (Phase 6 fix — earlier ToUpperInvariant did not
            // symmetrize with the Container side which carries the verbatim user-typed
            // InitialCatalog value, causing validator-mismatch in integration tests).
            var conn = Substitute.For<DbConnection>();
            conn.Database.Returns("MyDb");
            var sut = new SqlServerExternalDbNameExtractor();
            Assert.AreEqual("MyDb", sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_ReturnsEmptyStringWhenConnectionDatabaseIsNull()
        {
            var conn = Substitute.For<DbConnection>();
            conn.Database.Returns((string?)null);
            var sut = new SqlServerExternalDbNameExtractor();
            Assert.AreEqual(string.Empty, sut.Extract(conn));
        }
    }
}
