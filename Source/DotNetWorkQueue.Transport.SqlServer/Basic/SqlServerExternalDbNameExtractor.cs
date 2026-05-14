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

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// SqlServer implementation of <see cref="IExternalDbNameExtractor"/>. Returns the
    /// uppercase form of <see cref="DbConnection.Database"/> so the validator's
    /// <see cref="System.StringComparison.Ordinal"/> compare against the queue's
    /// configured database name behaves case-insensitively (matching SqlServer's
    /// catalog-name semantics). The matching Phase 3 producer / queue-config side is
    /// expected to upper-case the configured database name when populating
    /// <c>IConnectionInformation.Container</c>; this symmetric normalization is the
    /// per-provider case-handling convention from Phase 2 PLAN-2.1 architect note.
    /// </summary>
    public sealed class SqlServerExternalDbNameExtractor : IExternalDbNameExtractor
    {
        /// <summary>
        /// Returns the canonical (uppercase) database name reported by the connection.
        /// </summary>
        /// <param name="connection">An open <see cref="DbConnection"/> from the caller's
        /// transaction. Must not be null.</param>
        /// <returns>The database name, upper-cased via invariant culture.</returns>
        public string Extract(DbConnection connection)
        {
            return connection.Database?.ToUpperInvariant() ?? string.Empty;
        }
    }
}
