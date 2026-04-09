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
using DotNetWorkQueue.Dashboard.Ui.Services;

namespace DotNetWorkQueue.Dashboard.Ui.Models
{
    /// <summary>
    /// Groups connections from a single API source for display on the multi-source home page.
    /// </summary>
    public class SourceConnectionGroup
    {
        /// <summary>
        /// The source configuration this group represents.
        /// </summary>
        public DashboardApiSourceConfig Source { get; init; } = null!;

        /// <summary>
        /// Cached health state for this source.
        /// </summary>
        public SourceHealthState Health { get; init; } = null!;

        /// <summary>
        /// Loaded connections from this source. Null if not yet loaded.
        /// </summary>
        public List<ConnectionResponse>? Connections { get; set; }

        /// <summary>
        /// Per-source error message from a failed GetConnectionsAsync call.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// True while connections are being fetched for this source.
        /// </summary>
        public bool IsLoading { get; set; }
    }
}
