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
    /// SQLite implementation of <see cref="IExternalDbNameExtractor"/>. Returns the bare
    /// file stem of <see cref="DbConnection.DataSource"/> via
    /// <see cref="Path.GetFileNameWithoutExtension"/>, with NO case normalization.
    /// The companion <c>SqLiteExternalTransactionValidator</c> (PLAN-2.2) applies the
    /// same normalization to <see cref="IConnectionInformation.Container"/> so the
    /// <see cref="System.StringComparison.Ordinal"/> comparison is symmetric on both sides.
    /// </summary>
    /// <remarks>
    /// On Linux, <c>System.Data.SQLite</c> strips the <c>.db3</c> extension from
    /// <c>DataSource</c> after <c>Open()</c>; on Windows it preserves the extension.
    /// Applying <see cref="Path.GetFileNameWithoutExtension"/> on the extracted side and
    /// on the expected side is the safe cross-platform fix (see PR #143 path-normalization bug).
    /// For in-memory databases (<c>:memory:</c>), <c>GetFileNameWithoutExtension</c> returns
    /// <c>:memory:</c> unchanged, so the comparison still works correctly.
    /// </remarks>
    public sealed class SqLiteExternalDbNameExtractor : IExternalDbNameExtractor
    {
        /// <summary>
        /// Extracts the canonical SQLite database name from the supplied connection.
        /// Returns the file stem (no directory, no extension) of <see cref="DbConnection.DataSource"/>
        /// so that the result compares equal to the queue's configured
        /// <see cref="IConnectionInformation.Container"/> after the SQLite-specific validator
        /// applies <see cref="Path.GetFileNameWithoutExtension"/> on the expected side.
        /// </summary>
        /// <param name="connection">An open <see cref="DbConnection"/> from the caller's
        /// transaction. Must not be null.</param>
        /// <returns>The file stem of <see cref="DbConnection.DataSource"/>, or an empty
        /// string if the connection reports null or empty.</returns>
        public string Extract(DbConnection connection)
        {
            var raw = connection?.DataSource ?? string.Empty;
            return string.IsNullOrEmpty(raw) ? raw : Path.GetFileNameWithoutExtension(raw);
        }
    }
}
