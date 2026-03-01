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
namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query
{
    /// <summary>
    /// Dashboard query result: queue status counts by category
    /// </summary>
    public class DashboardStatusCounts
    {
        /// <summary>
        /// Gets or sets the number of messages waiting to be processed.
        /// </summary>
        public long Waiting { get; set; }

        /// <summary>
        /// Gets or sets the number of messages currently being processed.
        /// </summary>
        public long Processing { get; set; }

        /// <summary>
        /// Gets or sets the number of messages in error state.
        /// </summary>
        public long Error { get; set; }

        /// <summary>
        /// Gets or sets the total number of messages in the queue.
        /// </summary>
        public long Total { get; set; }
    }
}
