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
using System.IO;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SQLite.Basic;

namespace DotNetWorkQueue.Transport.SQLite
{
    /// <summary>
    /// <see cref="SqliteConnectionInformation"/> wrapper that applies the same
    /// <c>Path.GetFullPath()</c> + uppercase canonicalization as
    /// <see cref="SqLiteExternalDbNameExtractor"/> to the <see cref="Container"/>
    /// property, achieving symmetric normalization across both sides of the
    /// <c>ExternalTransactionValidator</c>'s <c>StringComparison.Ordinal</c> compare.
    /// </summary>
    /// <remarks>
    /// Phase 1 spike §3 + CLAUDE.md "string-comparator drift" lesson: both sides of the
    /// DB-name comparator must apply identical normalization. The validator uses
    /// <c>StringComparison.Ordinal</c>; the extractor + this wrapper both apply
    /// <c>Path.GetFullPath() + ToUpperInvariant()</c> with a <c>:memory:</c>
    /// short-circuit, which together achieve OrdinalIgnoreCase semantics under the
    /// strict comparator.
    /// </remarks>
    public sealed class SqliteNormalizedConnectionInformation : SqliteConnectionInformation
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SqliteNormalizedConnectionInformation"/>
        /// by delegating to the base <see cref="SqliteConnectionInformation"/> ctor.
        /// </summary>
        /// <param name="queueConnection">The queue connection info (queue name + connection string).</param>
        /// <param name="dataSource">The <see cref="IDbDataSource"/> used to extract the SQLite file path / <c>:memory:</c> from the connection string.</param>
        public SqliteNormalizedConnectionInformation(QueueConnection queueConnection, IDbDataSource dataSource)
            : base(queueConnection, dataSource)
        {
        }

        /// <inheritdoc />
        /// <remarks>
        /// Overrides the base property to apply the symmetric-normalization canonicalization
        /// (matches <see cref="SqLiteExternalDbNameExtractor.Extract"/>). Returns
        /// <c>:memory:</c> verbatim for in-memory DBs; otherwise <c>Path.GetFullPath()</c> +
        /// <c>ToUpperInvariant()</c>.
        /// </remarks>
        public override string Container
        {
            get
            {
                var raw = Server ?? string.Empty;
                if (raw == ":memory:")
                {
                    return ":memory:";
                }
                if (string.IsNullOrEmpty(raw))
                {
                    return string.Empty;
                }
                // Strip the file extension before canonicalizing: System.Data.SQLite's
                // SQLiteConnection.DataSource property returns the bare file name without
                // its extension after Open() on Linux. The extractor side (which reads from
                // conn.DataSource) therefore canonicalizes to e.g. "/path/Ixxx" while a
                // raw Path.GetFullPath on the connection-string Data Source value
                // canonicalizes to "/path/Ixxx.db3" — the symmetric-normalization comparator
                // would fail despite both sides pointing at the same physical file. Drop
                // the extension on the queue side to match the System.Data.SQLite behavior.
                var full = Path.GetFullPath(raw);
                var dir = Path.GetDirectoryName(full);
                var nameNoExt = Path.GetFileNameWithoutExtension(full);
                var combined = string.IsNullOrEmpty(dir) ? nameNoExt : Path.Combine(dir, nameNoExt);
                return combined.ToUpperInvariant();
            }
        }
    }
}
