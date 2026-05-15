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
using DotNetWorkQueue.Transport.RelationalDatabase;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// PostgreSQL implementation of <see cref="IExternalDbNameExtractor"/>. Returns
    /// <see cref="DbConnection.Database"/> verbatim with NO case normalization,
    /// matching PostgreSQL's case-sensitive identifier semantics. Quoted-identifier
    /// database names (e.g. <c>"MyDb"</c>) preserve case both server-side and in
    /// Npgsql's reported <c>Database</c> property; the matching
    /// <c>IConnectionInformation.Container</c> value is sourced from
    /// <c>NpgsqlConnectionStringBuilder.Database</c>, so both sides of the
    /// validator's <see cref="System.StringComparison.Ordinal"/> compare are
    /// byte-for-byte consistent.
    /// </summary>
    public sealed class PostgreSqlExternalDbNameExtractor : IExternalDbNameExtractor
    {
        /// <summary>
        /// Returns the database name reported by the connection, verbatim
        /// (no normalization).
        /// </summary>
        /// <param name="connection">An open <see cref="DbConnection"/> from the caller's
        /// transaction. Must not be null.</param>
        /// <returns>The database name from <see cref="DbConnection.Database"/>, or
        /// an empty string if the connection reports null.</returns>
        public string Extract(DbConnection connection)
        {
            return connection.Database ?? string.Empty;
        }
    }
}
