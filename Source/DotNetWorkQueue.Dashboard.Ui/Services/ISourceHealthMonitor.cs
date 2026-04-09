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
    /// Provides cached health state for configured Dashboard API sources.
    /// Health state is updated by a background polling service.
    /// </summary>
    public interface ISourceHealthMonitor
    {
        /// <summary>
        /// Returns the cached health state for the specified source slug.
        /// Returns a default <see cref="SourceHealthStatus.Unknown"/> state if the slug has not been polled yet.
        /// </summary>
        /// <param name="slug">The URL-safe slug identifying the API source.</param>
        /// <returns>The current health state for the source.</returns>
        SourceHealthState GetHealth(string slug);

        /// <summary>
        /// Returns the cached health state for all sources that have been polled.
        /// </summary>
        /// <returns>A read-only dictionary mapping source slugs to their health state.</returns>
        IReadOnlyDictionary<string, SourceHealthState> GetAllHealth();
    }
}
