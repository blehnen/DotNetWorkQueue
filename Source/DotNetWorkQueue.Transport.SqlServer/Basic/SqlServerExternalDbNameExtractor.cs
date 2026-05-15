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
    /// SqlServer implementation of <see cref="IExternalDbNameExtractor"/>. Returns
    /// <see cref="DbConnection.Database"/> verbatim — pass-through, no case normalization.
    /// The validator's <see cref="System.StringComparison.Ordinal"/> comparison against
    /// <see cref="IConnectionInformation.Container"/> (which equals <c>InitialCatalog</c>
    /// from the parsed connection string) is therefore case-sensitive.
    /// </summary>
    /// <remarks>
    /// Earlier implementations applied <c>ToUpperInvariant</c> on the extractor side but
    /// did not apply symmetric normalization on the <c>Container</c> side, causing a
    /// validator-mismatch when the actual SqlServer catalog case differed from the
    /// uppercased extractor output. Pass-through aligns the SqlServer extractor with
    /// PostgreSQL's pass-through approach (Phase 4 CONTEXT-4 Decision 2) and avoids the
    /// symmetry gap. Users must configure <c>Database=&lt;name&gt;</c> in the connection
    /// string with the exact case as the actual SqlServer catalog (typical and easy to
    /// satisfy; SqlClient's <c>conn.Database</c> after open returns the canonical name
    /// from <c>sys.databases</c>).
    /// </remarks>
    public sealed class SqlServerExternalDbNameExtractor : IExternalDbNameExtractor
    {
        /// <summary>
        /// Returns the database name reported by the connection verbatim.
        /// </summary>
        /// <param name="connection">An open <see cref="DbConnection"/> from the caller's
        /// transaction. Must not be null.</param>
        /// <returns>The database name as reported by <c>connection.Database</c> (or
        /// empty string if the connection has no database).</returns>
        public string Extract(DbConnection connection)
        {
            return connection.Database ?? string.Empty;
        }
    }
}
