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
    /// use by <see cref="Basic.ExternalTransactionValidator"/>. Per-provider implementations
    /// supply the appropriate name comparison semantics:
    /// <list type="bullet">
    ///   <item><description>SqlServer uses case-insensitive comparison (<c>StringComparer.OrdinalIgnoreCase</c>).</description></item>
    ///   <item><description>PostgreSQL uses case-sensitive comparison (<c>StringComparer.Ordinal</c>) to match the database's quoted-identifier semantics.</description></item>
    /// </list>
    /// Implementations live in <c>Transport.SqlServer</c> (Phase 3) and
    /// <c>Transport.PostgreSQL</c> (Phase 4); Phase 2 ships only this contract.
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
