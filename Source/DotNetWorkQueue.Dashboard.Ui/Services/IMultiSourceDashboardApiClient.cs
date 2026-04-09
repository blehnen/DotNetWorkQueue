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

using System.Collections.Generic;

namespace DotNetWorkQueue.Dashboard.Ui.Services
{
    /// <summary>
    /// Provides access to <see cref="IDashboardApiClient"/> instances
    /// for multiple configured API sources, identified by slug.
    /// </summary>
    public interface IMultiSourceDashboardApiClient
    {
        /// <summary>
        /// Returns a cached <see cref="IDashboardApiClient"/> for the specified source slug.
        /// </summary>
        /// <param name="slug">The URL-safe slug identifying the API source.</param>
        /// <returns>An <see cref="IDashboardApiClient"/> configured for the specified source.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="slug"/> is null.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when no source is configured with the specified slug.</exception>
        IDashboardApiClient GetClientForSource(string slug);

        /// <summary>
        /// Returns all configured API sources.
        /// </summary>
        /// <returns>A read-only list of all configured sources.</returns>
        IReadOnlyList<DashboardApiSourceConfig> GetAllSources();
    }
}
