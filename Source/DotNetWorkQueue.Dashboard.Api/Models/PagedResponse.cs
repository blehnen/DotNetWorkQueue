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

namespace DotNetWorkQueue.Dashboard.Api.Models
{
    /// <summary>
    /// Generic paged response wrapper.
    /// </summary>
    /// <typeparam name="T">The type of items in the page.</typeparam>
    public class PagedResponse<T>
    {
        /// <summary>Gets or sets the items in this page.</summary>
        public IReadOnlyList<T> Items { get; set; }

        /// <summary>Gets or sets the total count of items across all pages.</summary>
        public long TotalCount { get; set; }

        /// <summary>Gets or sets the zero-based page index.</summary>
        public int PageIndex { get; set; }

        /// <summary>Gets or sets the page size.</summary>
        public int PageSize { get; set; }
    }
}
