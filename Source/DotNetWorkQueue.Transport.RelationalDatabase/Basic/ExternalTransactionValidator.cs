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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Validates a caller-supplied <see cref="DbTransaction"/> before it is used by
    /// the relational producer to enqueue messages. Runs four checks in order:
    /// <list type="number">
    ///   <item><description>Transaction is non-null (throws <see cref="ArgumentNullException"/>).</description></item>
    ///   <item><description>Transaction's <c>Connection</c> is non-null (throws <see cref="InvalidOperationException"/> — "transaction disposed or completed").</description></item>
    ///   <item><description>Connection state is <see cref="ConnectionState.Open"/> (throws <see cref="InvalidOperationException"/>).</description></item>
    ///   <item><description>Database name reported by the connection (via the injected
    ///       <see cref="IExternalDbNameExtractor"/>) equals the queue's configured
    ///       database (via <see cref="IConnectionInformation.Container"/>). Both registered
    ///       extractors are pass-through; comparison is
    ///       <see cref="StringComparison.Ordinal"/>, so case differences between the
    ///       caller's connection-string catalog and the queue's configured
    ///       <c>Container</c> will fail this check on both transports. Mismatch throws
    ///       <see cref="InvalidOperationException"/> with both database names in the
    ///       message.</description></item>
    /// </list>
    /// </summary>
    public sealed class ExternalTransactionValidator
    {
        private readonly IExternalDbNameExtractor _extractor;
        private readonly IConnectionInformation _connectionInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalTransactionValidator"/>
        /// class.
        /// </summary>
        /// <param name="extractor">Per-provider database-name extractor.</param>
        /// <param name="connectionInfo">Queue's configured connection information. The
        /// <see cref="IConnectionInformation.Container"/> property supplies the expected
        /// database name on both SqlServer and PostgreSQL transports.</param>
        public ExternalTransactionValidator(IExternalDbNameExtractor extractor,
            IConnectionInformation connectionInfo)
        {
            Guard.NotNull(() => extractor, extractor);
            Guard.NotNull(() => connectionInfo, connectionInfo);
            _extractor = extractor;
            _connectionInfo = connectionInfo;
        }

        /// <summary>
        /// Runs the four validation checks against the caller-supplied transaction.
        /// </summary>
        /// <param name="transaction">Caller-supplied transaction.</param>
        /// <exception cref="ArgumentNullException">Transaction is null.</exception>
        /// <exception cref="InvalidOperationException">Connection is null, not open, or
        /// points to a different database than the queue's configured container.</exception>
        public void Validate(DbTransaction transaction)
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
            var expected = _connectionInfo.Container;
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Caller-supplied transaction's connection points to database " +
                    $"'{actual}' but the queue is configured for database '{expected}'. " +
                    $"The outbox pattern requires the queue tables and the caller's " +
                    $"business data to live in the same database.");
        }
    }
}
