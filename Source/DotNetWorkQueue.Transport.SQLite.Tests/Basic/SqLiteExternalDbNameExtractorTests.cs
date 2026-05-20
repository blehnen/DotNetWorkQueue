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
using System.Data.Common;
using System.IO;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// Tests for <see cref="SqLiteExternalDbNameExtractor"/> + the symmetric
    /// <see cref="SqliteNormalizedConnectionInformation"/> wrapper. Confirms spike §3
    /// semantics: <c>:memory:</c> short-circuit + <c>Path.GetFullPath()</c> +
    /// <c>ToUpperInvariant()</c> applied identically on both sides of the
    /// <c>ExternalTransactionValidator</c>'s <c>StringComparison.Ordinal</c> compare.
    /// </summary>
    [TestClass]
    public class SqLiteExternalDbNameExtractorTests
    {
        [TestMethod]
        public void Extract_Returns_MemoryLiteral_For_MemorySource()
        {
            var conn = Substitute.For<DbConnection>();
            conn.DataSource.Returns(":memory:");
            var sut = new SqLiteExternalDbNameExtractor();

            Assert.AreEqual(":memory:", sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_Returns_Canonicalized_UpperCased_Path_For_File_Source()
        {
            var conn = Substitute.For<DbConnection>();
            conn.DataSource.Returns("./test.db");
            var sut = new SqLiteExternalDbNameExtractor();

            var result = sut.Extract(conn);
            // Extension is stripped on both sides of the validator: System.Data.SQLite's
            // SQLiteConnection.DataSource returns the bare name without extension on
            // Linux, so the extractor (and SqliteNormalizedConnectionInformation.Container)
            // strip the extension symmetrically to keep the comparator consistent.
            var full = Path.GetFullPath("./test.db");
            var expected = Path.Combine(Path.GetDirectoryName(full) ?? string.Empty,
                Path.GetFileNameWithoutExtension(full)).ToUpperInvariant();
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Extract_Memory_Literal_Comparison_Is_Case_Sensitive()
        {
            // ":Memory:" (capital M) is NOT the SQLite keyword; the extractor must
            // treat it as a file path (will canonicalize + upper-case it).
            var conn = Substitute.For<DbConnection>();
            conn.DataSource.Returns(":Memory:");
            var sut = new SqLiteExternalDbNameExtractor();

            var result = sut.Extract(conn);
            Assert.AreNotEqual(":memory:", result);
            var full = Path.GetFullPath(":Memory:");
            var expected = Path.Combine(Path.GetDirectoryName(full) ?? string.Empty,
                Path.GetFileNameWithoutExtension(full)).ToUpperInvariant();
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Extract_Returns_Empty_For_Null_DataSource()
        {
            var conn = Substitute.For<DbConnection>();
            conn.DataSource.Returns((string)null);
            var sut = new SqLiteExternalDbNameExtractor();

            // null DataSource short-circuits to empty string before Path.GetFullPath
            var result = sut.Extract(conn);
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void Extract_Throws_ArgumentNullException_For_Null_Connection()
        {
            var sut = new SqLiteExternalDbNameExtractor();
            Assert.ThrowsExactly<ArgumentNullException>(() => sut.Extract(null));
        }
    }
}
