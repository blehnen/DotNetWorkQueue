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
using System.IO;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// SQLite-specific subclass of <see cref="ExternalTransactionValidator"/> that canonicalizes
    /// both sides of the database-name check by parsing each connection string through
    /// <c>SQLiteConnectionStringBuilder</c> (taking <c>FullUri</c> when set, otherwise
    /// <c>DataSource</c>) and applying <see cref="Path.GetFileNameWithoutExtension"/> before the
    /// <see cref="StringComparison.Ordinal"/> compare.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SQLite requires this normalization because the underlying connection's reported
    /// <c>DataSource</c> diverges from the queue's <see cref="IConnectionInformation.Container"/>
    /// in two ways: (1) Linux strips <c>.db3</c> extensions after <c>Open()</c> while Windows
    /// preserves them; (2) <c>FullUri=file:NAME?...</c> connection strings cause
    /// <c>SQLiteConnectionStringBuilder.DataSource</c> to be empty while the opened connection
    /// reports the URI as <c>DataSource</c>. The base implementation's raw container compare
    /// would always fail for FullUri form.
    /// </para>
    /// <para>
    /// Both sides are canonicalized by <c>SqLiteExternalDbNameExtractor.Canonicalize</c> so:
    /// <list type="bullet">
    ///   <item><description>Full-path <c>Data Source=</c> values lose their directory prefix and extension.</description></item>
    ///   <item><description><c>FullUri=file:NAME?...</c> values yield the file portion of the URI.</description></item>
    ///   <item><description>The <c>:memory:</c> special value passes through unchanged.</description></item>
    ///   <item><description>The comparison remains <see cref="StringComparison.Ordinal"/>, matching
    ///       the PostgreSQL and SQL Server transports per CONTEXT-1 D1.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class SqLiteExternalTransactionValidator : ExternalTransactionValidator
    {
        private readonly IExternalDbNameExtractor _extractor;
        private readonly IConnectionInformation _connectionInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteExternalTransactionValidator"/> class.
        /// </summary>
        /// <param name="extractor">Per-provider database-name extractor.</param>
        /// <param name="connectionInfo">Queue's configured connection information.</param>
        public SqLiteExternalTransactionValidator(
            IExternalDbNameExtractor extractor,
            IConnectionInformation connectionInfo)
            : base(extractor, connectionInfo)
        {
            _extractor = extractor;
            _connectionInfo = connectionInfo;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Canonicalizes both sides via <c>SqLiteExternalDbNameExtractor.Canonicalize</c>
        /// before the Ordinal comparison. Checks #1 and #2 (null transaction, null/closed
        /// connection) are byte-equivalent to the base implementation.
        /// </remarks>
        public override void Validate(DbTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            var connection = transaction.Connection;
            if (connection == null)
                throw new InvalidOperationException(
                    "Caller-supplied transaction has a null Connection. The transaction " +
                    "has been disposed or its work has already been committed/rolled back.");

            if (connection.State != ConnectionState.Open)
                throw new InvalidOperationException(
                    $"Caller-supplied transaction's connection is not open " +
                    $"(state = {connection.State}). The connection must be open before " +
                    $"the producer can enlist its commands in the caller's transaction.");

            var actual = _extractor.Extract(connection);
            var expected = SqLiteExternalDbNameExtractor.Canonicalize(_connectionInfo.ConnectionString);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Caller-supplied transaction's connection points to database " +
                    $"'{actual}' but the queue is configured for database '{expected}'. " +
                    $"The outbox pattern requires the queue tables and the caller's " +
                    $"business data to live in the same database.");
        }
    }
}
