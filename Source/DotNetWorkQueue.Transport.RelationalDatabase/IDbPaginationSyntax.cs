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

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// Provides database-specific SQL pagination syntax.
    /// </summary>
    public interface IDbPaginationSyntax
    {
        /// <summary>
        /// Returns the SQL pagination clause for the given offset and limit parameter names.
        /// </summary>
        /// <param name="offsetParam">The offset parameter name (e.g., "@Offset").</param>
        /// <param name="limitParam">The limit parameter name (e.g., "@PageSize").</param>
        /// <returns>The pagination SQL clause to append after ORDER BY.</returns>
        string BuildPaginationClause(string offsetParam, string limitParam);
    }
}
