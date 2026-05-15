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
    /// Configure <c>Database=&lt;name&gt;</c> in the connection string with the exact case as
    /// the actual SqlServer catalog. After the connection opens, SqlClient's
    /// <c>conn.Database</c> returns the canonical name from <c>sys.databases</c>, which
    /// must equal the queue's configured <see cref="IConnectionInformation.Container"/>
    /// byte-for-byte under <see cref="System.StringComparison.Ordinal"/> comparison.
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
