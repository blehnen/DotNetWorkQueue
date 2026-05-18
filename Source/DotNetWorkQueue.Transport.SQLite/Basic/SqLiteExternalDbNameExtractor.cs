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
using System.IO;
using DotNetWorkQueue.Transport.RelationalDatabase;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// SQLite implementation of <see cref="IExternalDbNameExtractor"/>. Returns the
    /// canonicalized file path from <see cref="DbConnection.DataSource"/>, with a
    /// <c>:memory:</c> short-circuit. The result is upper-cased so the validator's
    /// <see cref="System.StringComparison.Ordinal"/> compare against
    /// <see cref="IConnectionInformation.Container"/> (also upper-cased via
    /// <c>SqliteNormalizedConnectionInformation</c>) achieves OrdinalIgnoreCase semantics
    /// per Phase 1 spike §3.
    /// </summary>
    /// <remarks>
    /// Symmetric normalization (per spike §3 + CLAUDE.md "string-comparator drift" lesson):
    /// BOTH the extractor side and the validator-input side (<see cref="SqliteNormalizedConnectionInformation"/>)
    /// apply identical canonicalization. The validator's <see cref="System.StringComparison.Ordinal"/>
    /// compare then succeeds even when the original file paths differed in case.
    /// <para>
    /// The <c>:memory:</c> literal is preserved verbatim because it is a SQLite keyword
    /// (case-sensitive) — passing it through <see cref="Path.GetFullPath(string)"/> would
    /// treat it as a relative file path and produce nonsense.
    /// </para>
    /// </remarks>
    public sealed class SqLiteExternalDbNameExtractor : IExternalDbNameExtractor
    {
        /// <summary>
        /// Returns the canonicalized file path reported by the connection, or
        /// <c>:memory:</c> for in-memory databases. Upper-cased for symmetric-normalization
        /// validator semantics.
        /// </summary>
        /// <param name="connection">An open <see cref="DbConnection"/> from the caller's
        /// transaction. Must not be null.</param>
        /// <returns>The canonicalized path (or <c>:memory:</c>) upper-cased.</returns>
        public string Extract(DbConnection connection)
        {
            var raw = connection.DataSource ?? string.Empty;
            if (raw == ":memory:")
            {
                return ":memory:";
            }
            return Path.GetFullPath(raw).ToUpperInvariant();
        }
    }
}
