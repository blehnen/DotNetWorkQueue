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
using System;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Purges message history records older than the configured retention period.
    /// </summary>
    public interface IPurgeMessageHistory
    {
        /// <summary>
        /// Deletes all history records older than the specified cutoff date.
        /// </summary>
        /// <param name="olderThan">Records with CompletedUtc (or EnqueuedUtc if never completed) before this date are deleted.</param>
        /// <returns>The number of records deleted.</returns>
        long Purge(DateTime olderThan);
    }
}
