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

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// Extracts the canonical database name from an open <see cref="DbConnection"/> for
    /// use by <see cref="Basic.ExternalTransactionValidator"/>. Both registered transport
    /// implementations (<c>Transport.SqlServer</c> and <c>Transport.PostgreSQL</c>) return
    /// <c>connection.Database</c> verbatim (pass-through, no case normalization). The
    /// validator compares the result against <see cref="IConnectionInformation.Container"/>
    /// using <c>StringComparison.Ordinal</c> — so both transports use case-sensitive,
    /// byte-verbatim comparison. Callers whose connection strings differ in catalog case
    /// from the queue's configured <c>Container</c> will see a cross-database validation
    /// failure even when the underlying database engine would treat the names as identical.
    /// </summary>
    public interface IExternalDbNameExtractor
    {
        /// <summary>
        /// Returns the canonical database name reported by the connection. Typically
        /// implemented via <c>connection.Database</c>.
        /// </summary>
        /// <param name="connection">An open <see cref="DbConnection"/> from the caller's
        /// transaction. Must not be null.</param>
        /// <returns>The database name as reported by the underlying ADO.NET provider.</returns>
        string Extract(DbConnection connection);
    }
}
