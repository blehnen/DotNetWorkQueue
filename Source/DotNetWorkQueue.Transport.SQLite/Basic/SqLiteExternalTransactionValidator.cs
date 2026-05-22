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
    /// SQLite-specific subclass of <see cref="ExternalTransactionValidator"/> that applies
    /// <see cref="Path.GetFileNameWithoutExtension"/> symmetrically to both sides of the
    /// database-name check before the <see cref="StringComparison.Ordinal"/> compare.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SQLite requires this normalization because <see cref="IConnectionInformation.Container"/>
    /// returns the full file path including the <c>.db3</c> extension (e.g.
    /// <c>/data/myqueue.db3</c>), while the underlying connection's
    /// <c>DataSource</c> property returns the extension-stripped stem on Linux
    /// (e.g. <c>myqueue</c>). A raw <see cref="StringComparison.Ordinal"/> compare against the
    /// full path would always fail even when the caller's transaction targets the correct file.
    /// </para>
    /// <para>
    /// <see cref="Path.GetFileNameWithoutExtension"/> is applied to both sides so:
    /// <list type="bullet">
    ///   <item><description>Full-path Container values lose their directory prefix and extension.</description></item>
    ///   <item><description>The <c>:memory:</c> special value passes through unchanged (no extension to strip).</description></item>
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
        /// Applies <see cref="Path.GetFileNameWithoutExtension"/> to both the extracted
        /// database name and the configured container before the Ordinal comparison.
        /// Checks #1–#3 are byte-equivalent to the base implementation.
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

            var actualRaw = _extractor.Extract(connection);
            var expectedRaw = _connectionInfo.Container;
            var actual = string.IsNullOrEmpty(actualRaw) ? actualRaw : Path.GetFileNameWithoutExtension(actualRaw);
            var expected = string.IsNullOrEmpty(expectedRaw) ? expectedRaw : Path.GetFileNameWithoutExtension(expectedRaw);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Caller-supplied transaction's connection points to database " +
                    $"'{actual}' but the queue is configured for database '{expected}'. " +
                    $"The outbox pattern requires the queue tables and the caller's " +
                    $"business data to live in the same database.");
        }
    }
}
