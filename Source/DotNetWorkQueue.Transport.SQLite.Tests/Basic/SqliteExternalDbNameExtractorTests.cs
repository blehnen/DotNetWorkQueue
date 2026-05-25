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
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class SqliteExternalDbNameExtractorTests
    {
        [TestMethod]
        public void Extract_BareDataSource_ReturnsStem()
        {
            var conn = Substitute.For<DbConnection>();
            conn.ConnectionString.Returns("Data Source=myqueue;Version=3;");
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.AreEqual("myqueue", sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_StripsDb3Extension_OnBareFileName()
        {
            var conn = Substitute.For<DbConnection>();
            conn.ConnectionString.Returns("Data Source=myqueue.db3;Version=3;");
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.AreEqual("myqueue", sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_StripsDirectoryAndExtension_FromUnixPath()
        {
            var conn = Substitute.For<DbConnection>();
            conn.ConnectionString.Returns("Data Source=/data/queues/myqueue.db3;Version=3;");
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.AreEqual("myqueue", sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_StripsDb3Extension_FromWindowsPath()
        {
            // On Linux the backslash is literal (not a path separator), so the directory
            // prefix won't be stripped; on Windows it will. The cross-platform invariant
            // tested here is that the .db3 extension is always removed.
            var conn = Substitute.For<DbConnection>();
            conn.ConnectionString.Returns(@"Data Source=C:\data\queues\myqueue.db3;Version=3;");
            var sut = new SqLiteExternalDbNameExtractor();
            var result = sut.Extract(conn);
            Assert.IsFalse(result.EndsWith(".db3"),
                "Extract must strip .db3 regardless of platform path-separator semantics.");
        }

        [TestMethod]
        public void Extract_MemoryMode_ReturnsMemoryToken()
        {
            var conn = Substitute.For<DbConnection>();
            conn.ConnectionString.Returns("Data Source=:memory:;Version=3;");
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.AreEqual(":memory:", sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_FullUriMemory_ReturnsCanonicalFullUriValue()
        {
            // The FullUri=file:NAME?mode=memory&cache=shared form is how shared in-memory
            // databases are addressed. SQLiteConnectionStringBuilder populates FullUri
            // (not DataSource) for this form — the extractor must select FullUri so the
            // result is symmetric with the queue side parsing the same connection string.
            var conn = Substitute.For<DbConnection>();
            conn.ConnectionString.Returns("FullUri=file:Iabc?mode=memory&cache=shared;Version=3;");
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.AreEqual("file:Iabc?mode=memory&cache=shared", sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_NullConnection_ReturnsEmpty()
        {
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.AreEqual(string.Empty, sut.Extract(null));
        }

        [TestMethod]
        public void Extract_EmptyConnectionString_ReturnsEmpty()
        {
            var conn = Substitute.For<DbConnection>();
            conn.ConnectionString.Returns(string.Empty);
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.AreEqual(string.Empty, sut.Extract(conn));
        }

        [TestMethod]
        public void Canonicalize_FullUriForm_MatchesExtractorOutput_Symmetry()
        {
            // Validator/extractor symmetry: parsing the same connection string from both
            // sides (queue + open connection) must yield identical canonical names.
            const string connStr = "FullUri=file:Iabc?mode=memory&cache=shared;Version=3;";

            var conn = Substitute.For<DbConnection>();
            conn.ConnectionString.Returns(connStr);

            var fromConnection = new SqLiteExternalDbNameExtractor().Extract(conn);
            var fromConnectionString = SqLiteExternalDbNameExtractor.Canonicalize(connStr);

            Assert.AreEqual(fromConnectionString, fromConnection);
        }

        [TestMethod]
        public void Canonicalize_DataSourceForm_MatchesExtractorOutput_Symmetry()
        {
            const string connStr = "Data Source=/data/queues/myqueue.db3;Version=3;";

            var conn = Substitute.For<DbConnection>();
            conn.ConnectionString.Returns(connStr);

            var fromConnection = new SqLiteExternalDbNameExtractor().Extract(conn);
            var fromConnectionString = SqLiteExternalDbNameExtractor.Canonicalize(connStr);

            Assert.AreEqual(fromConnectionString, fromConnection);
            Assert.AreEqual("myqueue", fromConnectionString);
        }
    }
}
