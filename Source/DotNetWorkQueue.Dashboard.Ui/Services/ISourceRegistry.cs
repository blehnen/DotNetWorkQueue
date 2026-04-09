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

namespace DotNetWorkQueue.Dashboard.Ui.Services
{
    /// <summary>
    /// Registry of configured Dashboard API sources.
    /// Provides lookup by slug or name for routing and display.
    /// </summary>
    public interface ISourceRegistry
    {
        /// <summary>
        /// Returns all configured API sources.
        /// </summary>
        /// <returns>A read-only list of all configured sources.</returns>
        IReadOnlyList<DashboardApiSourceConfig> GetAll();

        /// <summary>
        /// Finds a source by its URL-safe slug.
        /// </summary>
        /// <param name="slug">The slug to search for.</param>
        /// <returns>The matching source, or null if not found.</returns>
        DashboardApiSourceConfig? GetBySlug(string slug);

        /// <summary>
        /// Finds a source by its display name (case-insensitive).
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <returns>The matching source, or null if not found.</returns>
        DashboardApiSourceConfig? GetByName(string name);
    }
}
