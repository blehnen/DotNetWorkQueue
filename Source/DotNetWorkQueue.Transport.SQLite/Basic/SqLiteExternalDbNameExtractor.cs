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
using System.Data.SQLite;
using System.IO;
using DotNetWorkQueue.Transport.RelationalDatabase;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// SQLite implementation of <see cref="IExternalDbNameExtractor"/>. Parses the open
    /// connection's <see cref="DbConnection.ConnectionString"/> via
    /// <see cref="SQLiteConnectionStringBuilder"/>, prefers <c>FullUri</c> when set,
    /// otherwise falls back to <c>DataSource</c>, then returns the file stem via
    /// <see cref="Path.GetFileNameWithoutExtension"/>. The companion
    /// <c>SqLiteExternalTransactionValidator</c> applies the identical builder + selection
    /// to the queue's configured connection string, so the
    /// <see cref="System.StringComparison.Ordinal"/> comparison is symmetric on both sides
    /// for every supported connection-string form (<c>Data Source=</c>, <c>FullUri=</c>,
    /// and <c>:memory:</c>).
    /// </summary>
    /// <remarks>
    /// Why parse <see cref="DbConnection.ConnectionString"/> rather than read
    /// <see cref="DbConnection.DataSource"/> directly: System.Data.SQLite reports an opened
    /// FullUri connection's <c>DataSource</c> as the raw URI (e.g.
    /// <c>file:NAME?mode=memory&amp;cache=shared</c>), which never matches the queue side
    /// where the builder returns empty for the same string. Routing both sides through
    /// <see cref="SQLiteConnectionStringBuilder"/> with the same
    /// <c>FullUri ?? DataSource</c> selection guarantees symmetric output.
    /// </remarks>
    public sealed class SqLiteExternalDbNameExtractor : IExternalDbNameExtractor
    {
        /// <summary>
        /// Extracts the canonical SQLite database name from the supplied connection.
        /// </summary>
        /// <param name="connection">An open <see cref="DbConnection"/> from the caller's
        /// transaction. Must not be null.</param>
        /// <returns>The file stem of the connection's <c>FullUri</c> or <c>DataSource</c>,
        /// or an empty string if the connection or its connection string is null/empty.</returns>
        public string Extract(DbConnection connection)
        {
            return Canonicalize(connection?.ConnectionString);
        }

        /// <summary>
        /// Shared canonicalization used by both the extractor and the validator's expected side.
        /// Returns <see cref="Path.GetFileNameWithoutExtension"/> of <c>FullUri</c> (preferred)
        /// or <c>DataSource</c>, or empty when both are empty. Returns empty for null/empty input.
        /// </summary>
        internal static string Canonicalize(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return string.Empty;

            var builder = new SQLiteConnectionStringBuilder(connectionString);
            var raw = !string.IsNullOrEmpty(builder.FullUri) ? builder.FullUri : builder.DataSource;
            return string.IsNullOrEmpty(raw) ? string.Empty : Path.GetFileNameWithoutExtension(raw);
        }
    }
}
