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
        public void Extract_BareStem_ReturnsStemUnchanged()
        {
            // Linux runtime shape: System.Data.SQLite strips .db3 from DataSource after Open();
            // the provider hands us the bare stem already.
            var conn = Substitute.For<DbConnection>();
            conn.DataSource.Returns("myqueue");
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.AreEqual("myqueue", sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_StripsDb3Extension_OnBareFileName()
        {
            // Windows runtime shape: provider retains the .db3 extension in DataSource.
            // GetFileNameWithoutExtension must strip it so the result matches the queue's
            // configured Container (which is always the bare stem).
            var conn = Substitute.For<DbConnection>();
            conn.DataSource.Returns("myqueue.db3");
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.AreEqual("myqueue", sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_StripsDirectoryAndExtension_FromUnixPath()
        {
            // Full path on Linux — directory prefix and .db3 extension must both be stripped.
            var conn = Substitute.For<DbConnection>();
            conn.DataSource.Returns("/data/queues/myqueue.db3");
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.AreEqual("myqueue", sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_StripsDb3Extension_FromWindowsPath()
        {
            // On Linux, backslash is not a path separator, so Path.GetFileNameWithoutExtension
            // treats the whole string as a filename and strips only the .db3 extension.
            // On Windows it would also strip the directory. Either way the extension is stripped,
            // which is the invariant this test guards against.
            var conn = Substitute.For<DbConnection>();
            conn.DataSource.Returns(@"C:\data\queues\myqueue.db3");
            var sut = new SqLiteExternalDbNameExtractor();
            // Linux: backslash is literal — result is the full stem without extension
            var result = sut.Extract(conn);
            Assert.IsFalse(result.EndsWith(".db3"),
                "Extract must strip .db3 extension regardless of OS path-separator semantics.");
        }

        [TestMethod]
        public void Extract_MemoryMode_ReturnsMemoryToken()
        {
            // :memory: has no extension; GetFileNameWithoutExtension returns it unchanged.
            // In-memory queues compare their Container against :memory: — must round-trip.
            var conn = Substitute.For<DbConnection>();
            conn.DataSource.Returns(":memory:");
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.AreEqual(":memory:", sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_NullDataSource_ReturnsEmpty()
        {
            // Null DataSource must yield string.Empty, not throw.
            var conn = Substitute.For<DbConnection>();
            conn.DataSource.Returns((string)null);
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.AreEqual(string.Empty, sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_EmptyDataSource_ReturnsEmpty()
        {
            // Empty DataSource must yield string.Empty, consistent with null handling.
            var conn = Substitute.For<DbConnection>();
            conn.DataSource.Returns(string.Empty);
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.AreEqual(string.Empty, sut.Extract(conn));
        }
    }
}
